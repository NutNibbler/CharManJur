using CharManJur.Models;

namespace CharManJur.Services;

public sealed record KillResult(bool Recorded, bool Unlocked, string Message);

// Weapon proficiency: 5 kills unlock a damage type's rule and, separately, a weapon's skills.
// A kill ticks both tallies at once. Advisory only: it tells the player what they can use, it never blocks.
public sealed class WeaponProficiencyTracker
{
    public const int KillsRequired = 5;

    private static readonly string[] SkillEffectPrefixes = { "Heavy", "Sweep", "Push", "Counter", "Brutal", "Shock", "Bleed" };

    private readonly ICharAttribDataService _svc;
    private readonly IPlayerActionLogService _log;

    public WeaponProficiencyTracker(ICharAttribDataService svc, IPlayerActionLogService log)
    {
        _svc = svc;
        _log = log;
    }

    // Read fresh every time: a different character may have been loaded since.
    public WeaponProficiencyState State => _svc.Proficiency;

    public bool NeedsStartingChoice => !State.Initialized;

    // Heavy, Sweep, Push, Counter, Brutal, Shock and Bleed need weapon proficiency. Reload, Modular, Reach, Blast and Range don't.
    public static bool NeedsWeaponSkill(WeaponEffectType effect)
    {
        string name = effect.ToString();
        return SkillEffectPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal));
    }

    // ===== LOOKUPS =====
    public DamageTypeProgress TypeProgress(WeaponDamageType type)
    {
        var row = State.DamageTypes.FirstOrDefault(r => r.Type == type);
        if (row == null)
        {
            row = new DamageTypeProgress { Type = type };
            State.DamageTypes.Add(row);
        }
        return row;
    }

    public int TypeKills(WeaponDamageType type) => State.DamageTypes.FirstOrDefault(r => r.Type == type)?.Kills ?? 0;

    public bool IsTypeProficient(WeaponDamageType type)
    {
        var row = State.DamageTypes.FirstOrDefault(r => r.Type == type);
        return row != null && (row.Granted || row.Kills >= KillsRequired);   // the starting choice, or 5 kills
    }

    private static bool Matches(WeaponKillRecord record, Item template)
        => template.IsPlayerCreated
            ? record.TemplateGuid == template.Guid
            : !record.TemplateGuid.HasValue && record.TemplateId == template.Id;

    public WeaponKillRecord? FindWeapon(Item template) => State.Weapons.FirstOrDefault(r => Matches(r, template));

    public int WeaponKills(Item template) => FindWeapon(template)?.Kills ?? 0;

    public bool IsWeaponProficient(Item template)
        => WeaponKills(template) >= KillsRequired || State.GrantedSkills.Any(r => Matches(r, template));

    public bool IsEffectUsable(Item template, WeaponEffectType effect)
        => !NeedsWeaponSkill(effect) || IsWeaponProficient(template);

    // True when a kill-tracker entry is for a weapon the Warrior benefit already unlocked.
    public bool IsGranted(WeaponKillRecord record)
        => State.GrantedSkills.Any(g => record.TemplateGuid.HasValue
            ? g.TemplateGuid == record.TemplateGuid
            : !g.TemplateGuid.HasValue && g.TemplateId == record.TemplateId);

    private static WeaponKillRecord NewRecord(Item template, WeaponDamageType type) => new()
    {
        TemplateId = template.Id,
        TemplateGuid = template.IsPlayerCreated ? template.Guid : null,
        Name = template.Name,
        DamageType = type
    };

    // ===== RECORDING =====
    public async Task<KillResult> RecordKillAsync(Item template)
    {
        if (!template.IsWeapon || template.DamageType is not WeaponDamageType type)
            return new KillResult(false, false, "This weapon has no damage type set, so its kills can't be tracked.");

        var typeRow = TypeProgress(type);
        var weapon = FindWeapon(template);

        if (typeRow.Kills >= KillsRequired && (weapon?.Kills ?? 0) >= KillsRequired)
            return new KillResult(false, false, $"Already at {KillsRequired}/{KillsRequired} for {template.Name} and {Display(type)} weapons.");

        bool typeWas = IsTypeProficient(type);
        bool weaponWas = IsWeaponProficient(template);

        if (weapon == null)
        {
            weapon = NewRecord(template, type);
            State.Weapons.Add(weapon);
        }
        if (typeRow.Kills < KillsRequired) typeRow.Kills++;
        if (weapon.Kills < KillsRequired) weapon.Kills++;

        bool typeNow = IsTypeProficient(type);
        bool weaponNow = IsWeaponProficient(template);

        await LogAsync(PlayerActionType.KillRecorded,
            $"Killing blow with {template.Name}: weapon {weapon.Kills}/{KillsRequired}, {Display(type)} {typeRow.Kills}/{KillsRequired}", template.Name);

        var unlocked = new List<string>();
        if (typeNow && !typeWas)
        {
            unlocked.Add($"{Display(type)} proficiency");
            await LogAsync(PlayerActionType.ProficiencyGained, $"Gained {Display(type)} proficiency ({typeRow.Kills} kills)");
        }
        if (weaponNow && !weaponWas)
        {
            var skills = (template.WeaponEffects ?? new List<WeaponEffectType>())
                .Where(NeedsWeaponSkill).Select(e => EnumDisplayHelper.GetName(e)).ToList();
            unlocked.Add($"{template.Name} weapon skills" + (skills.Count > 0 ? $" ({string.Join(", ", skills)})" : ""));
            await LogAsync(PlayerActionType.ProficiencyGained, $"Gained weapon skill proficiency: {template.Name} ({weapon.Kills} kills)", template.Name);
        }

        await _svc.SaveNowAsync();

        string message = $"{template.Name} {weapon.Kills}/{KillsRequired} · {Display(type)} {typeRow.Kills}/{KillsRequired}.";
        if (unlocked.Count > 0) message += " Unlocked: " + string.Join(" and ", unlocked) + ".";
        return new KillResult(true, unlocked.Count > 0, message);
    }

    // ===== EDITING (the tracker page) =====
    public async Task SetTypeKillsAsync(WeaponDamageType type, int kills)
    {
        kills = Math.Clamp(kills, 0, KillsRequired);
        var row = TypeProgress(type);
        if (row.Kills == kills) return;

        int before = row.Kills;
        row.Kills = kills;
        await LogAsync(PlayerActionType.KillsEdited, $"Edited {Display(type)} kills: {before} → {kills}");
        await _svc.SaveNowAsync();
    }

    // Setting a weapon's kills to 0 removes its entry. +Kill on the equipped weapon starts it again.
    public async Task SetWeaponKillsAsync(WeaponKillRecord record, int kills)
    {
        kills = Math.Clamp(kills, 0, KillsRequired);
        if (record.Kills == kills) return;

        int before = record.Kills;
        record.Kills = kills;
        if (kills == 0) State.Weapons.Remove(record);

        await LogAsync(PlayerActionType.KillsEdited, $"Edited {record.Name} kills: {before} → {kills}", record.Name);
        await _svc.SaveNowAsync();
    }

    // ===== STARTING CHOICE / WARRIOR =====
    public async Task ChooseStartingTypeAsync(WeaponDamageType type)
    {
        TypeProgress(type).Granted = true;
        State.Initialized = true;

        await LogAsync(PlayerActionType.ProficiencyGained, $"Chose {Display(type)} as the starting damage type proficiency");
        await _svc.SaveNowAsync();
    }

    // The Warrior's extra weapon: its skills work without kills (the damage type rule still needs the type).
    public async Task<bool> GrantWarriorWeaponAsync(Item template)
    {
        if (!template.IsWeapon || State.GrantedSkills.Any(r => Matches(r, template))) return false;

        State.GrantedSkills.Add(NewRecord(template, template.DamageType ?? WeaponDamageType.Piercing));
        await LogAsync(PlayerActionType.ProficiencyGained,
            $"Warrior benefit: gained {template.Name} weapon skill proficiency", template.Name);
        await _svc.SaveNowAsync();
        return true;
    }

    // ===== PLUMBING =====
    public static string Display(WeaponDamageType type) => EnumDisplayHelper.GetName(type);

    private Task LogAsync(PlayerActionType type, string summary, string? itemName = null)
        => _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = type,
            PlayerName = _svc.PlayerName,
            PlayerId = _svc.PlayerId,
            CharacterName = _svc.CharacterName,
            CharacterGuid = _svc.CharacterGuid,
            ItemName = itemName,
            Summary = summary
        });
}