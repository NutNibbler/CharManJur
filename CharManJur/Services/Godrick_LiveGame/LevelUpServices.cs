using CharManJur.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CharManJur.Services;

// The Artificer's construct. Type, intelligence and size are fixed (Construct, Inanimate, Small).
public sealed record ConstructSpec(string Name, string Description, int BaseCapacity);

// Everything chosen for one level-up.
public sealed class LevelUpRequest
{
    public Dictionary<string, int> AbilityPoints { get; init; } = new();   // "Vigor" -> points (2 in total)
    public CharacterClass Class { get; init; } = null!;
    public ClassFeature? Feature { get; init; }                // null for a class with no features (Shifter)
    public Item? WarriorWeapon { get; init; }                  // Warrior: the additional weapon
    public SubFeatureEntry? Quip { get; init; }                // Arcanist: the free quip
    public SubFeatureEntry? FeatureSubFeature { get; init; }   // Spell Inscription / Technique / Blueprint: what was picked
    public ConstructSpec? Construct { get; init; }             // Artificer: the new construct
}

public sealed class LevelUpResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int NewLevel { get; init; }
    public List<string> Notes { get; init; } = new();   // things that went wrong after the level was applied

    public static LevelUpResult Fail(string message) => new() { Success = false, Message = message };
}

public static class LevelUpRules
{
    public const int AbilityPointsPerLevel = 2;
    public const int UsualMaxLevel = 6;           // past this is rule skating, never blocked
    public const int UsualMaxPicksPerClass = 4;   // same
    public const int ConstructDescriptionWords = 5;

    public static readonly string[] Stats = { "Vigor", "Agility", "Mind", "Spirit" };

    // "1 Vigor, 1 Any" -> Vigor 1. "Any" can go on any score, so it adds no requirement.
    public static Dictionary<string, int> RequiredPoints(CharacterClass cls)
    {
        var required = new Dictionary<string, int>();
        foreach (var part in (cls.LevelingSkillBonus ?? string.Empty).Split(','))
        {
            var bits = part.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (bits.Length != 2 || !int.TryParse(bits[0], out int count)) continue;

            var stat = Stats.FirstOrDefault(s => s.Equals(bits[1], StringComparison.OrdinalIgnoreCase));
            if (stat == null) continue;
            required[stat] = required.GetValueOrDefault(stat) + count;
        }
        return required;
    }

    // A class can be picked when the points include what it asks for.
    public static bool IsAllowed(CharacterClass cls, IReadOnlyDictionary<string, int> allocation)
        => RequiredPoints(cls).All(r => allocation.GetValueOrDefault(r.Key) >= r.Value);

    public static int WordCount(string? text)
        => string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    public static string Ordinal(int n)
        => n % 100 is 11 or 12 or 13 ? $"{n}th" : (n % 10) switch { 1 => $"{n}st", 2 => $"{n}nd", 3 => $"{n}rd", _ => $"{n}th" };
}

// Taking a level: ability points, a class pick with its recurring benefits and feature, and the choices that come with them.
// Everything is validated first, then applied, so a refused level changes nothing.
public sealed class LevelUpService
{
    private readonly ICharAttribDataService _svc;
    private readonly IClassDataService _classes;
    private readonly LiveSubFeatures _subFeatures;
    private readonly IPlayerActionLogService _log;
    private readonly WeaponProficiencyTracker _proficiency;

    public LevelUpService(ICharAttribDataService svc, IClassDataService classes, LiveSubFeatures subFeatures, IPlayerActionLogService log)
    {
        _svc = svc;
        _classes = classes;
        _subFeatures = subFeatures;
        _log = log;
        _proficiency = new WeaponProficiencyTracker(svc, log);
    }

    public static LevelUpService Create(ICharAttribDataService svc, IServiceProvider services)
    {
        var log = services.GetRequiredService<IPlayerActionLogService>();
        var subFeatures = new LiveSubFeatures(
            svc,
            services.GetRequiredService<IBlueprintDataService>(),
            services.GetRequiredService<IQuipDataService>(),
            services.GetRequiredService<ISpellDataService>(),
            services.GetRequiredService<ITechniqueDataService>(),
            log);
        return new LevelUpService(svc, services.GetRequiredService<IClassDataService>(), subFeatures, log);
    }

    public LiveSubFeatures SubFeatures => _subFeatures;
    public int CurrentLevel => _svc.CharacterLevel;
    public int PendingLevels => _svc.PendingLevels;

    // Single-player: always. Hosted: only while the GM has granted a level.
    public bool CanLevelUp => !SessionMode.GmHosted || _svc.PendingLevels > 0;

    // ===== WHAT THE PAGE OFFERS =====
    public int ClassPicks(int classId) => _svc.ClassSelections.GetValueOrDefault(classId);

    public async Task<List<CharacterClass>> AllowedClassesAsync(IReadOnlyDictionary<string, int> allocation)
    {
        var all = await _classes.GetClassesAsync();
        return all.Where(c => LevelUpRules.IsAllowed(c, allocation)).ToList();
    }

    // A feature can be taken once, except the ones that add spells, techniques or blueprints.
    public List<ClassFeature> AvailableFeatures(CharacterClass cls)
        => cls.Features.Where(f => f.UnlockableTypes.Count > 0 || !_svc.HasFeature(f.Id)).ToList();

    public async Task<List<SubFeatureEntry>> AvailableSubFeaturesAsync(SubFeatureType type)
    {
        var catalog = await _subFeatures.CatalogAsync(type);
        var owned = _subFeatures.Owned(type);
        return catalog.Where(e => !owned.Any(o => o.IsSameAs(e)))
                      .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                      .ToList();
    }

    // Going past the usual limits is allowed, and flagged as rule skating.
    public List<string> SoftLimitWarnings(CharacterClass cls)
    {
        var warnings = new List<string>();

        int newLevel = _svc.CharacterLevel + 1;
        if (newLevel > LevelUpRules.UsualMaxLevel)
            warnings.Add($"This would be level {newLevel}; the usual maximum is {LevelUpRules.UsualMaxLevel}.");

        int picks = ClassPicks(cls.Id) + 1;
        if (picks > LevelUpRules.UsualMaxPicksPerClass)
            warnings.Add($"This would be your {LevelUpRules.Ordinal(picks)} {cls.ClassName} pick; the usual maximum is {LevelUpRules.UsualMaxPicksPerClass} per class.");

        return warnings;
    }

    // ===== VALIDATION =====
    public string? Validate(LevelUpRequest r)
    {
        if (!CanLevelUp) return "A level-up has to be granted by your GM first.";
        if (r.Class == null) return "Choose a class.";

        if (r.AbilityPoints.Any(p => p.Value < 0 || !LevelUpRules.Stats.Contains(p.Key))) return "Unknown ability score.";
        if (r.AbilityPoints.Values.Sum() != LevelUpRules.AbilityPointsPerLevel)
            return $"Distribute exactly {LevelUpRules.AbilityPointsPerLevel} ability points.";
        if (!LevelUpRules.IsAllowed(r.Class, r.AbilityPoints))
            return $"{r.Class.ClassName} needs these ability points: {r.Class.LevelingSkillBonus}.";

        // ----- the feature -----
        if (r.Class.Features.Count > 0)
        {
            if (r.Feature == null || !r.Class.Features.Any(f => f.Id == r.Feature.Id)) return "Choose one of the class's features.";

            if (r.Feature.UnlockableTypes.Count == 0)
            {
                if (_svc.HasFeature(r.Feature.Id)) return $"You already have {r.Feature.Name}.";
            }
            else
            {
                var type = r.Feature.UnlockableTypes[0];
                if (r.FeatureSubFeature == null || r.FeatureSubFeature.Type != type)
                    return $"Choose the {LiveSubFeatures.TypeName(type).ToLowerInvariant()} for {r.Feature.Name}.";
                if (_subFeatures.IsOwned(r.FeatureSubFeature)) return $"You already know {r.FeatureSubFeature.Name}.";
            }
        }
        else if (r.Feature != null)
        {
            return $"{r.Class.ClassName} has no features to choose.";
        }

        // ----- the class's own choice -----
        switch (r.Class.RecurringChoice)
        {
            case ClassChoice.Weapon:
                if (r.WarriorWeapon is not { IsWeapon: true }) return "Choose your additional weapon.";
                if (_svc.Proficiency.GrantedSkills.Any(g => g.IsFor(r.WarriorWeapon)))
                    return $"You already have a weapon pick for {r.WarriorWeapon.Name}.";
                break;

            case ClassChoice.Quip:
                if (r.Quip == null || r.Quip.Type != SubFeatureType.Quip) return "Choose your free quip.";
                if (_subFeatures.IsOwned(r.Quip)) return $"You already know {r.Quip.Name}.";
                break;

            case ClassChoice.Construct:
                if (r.Construct == null || string.IsNullOrWhiteSpace(r.Construct.Name)) return "Name your construct.";
                if (LevelUpRules.WordCount(r.Construct.Description) > LevelUpRules.ConstructDescriptionWords)
                    return $"A construct's description is {LevelUpRules.ConstructDescriptionWords} words at most.";
                if (r.Construct.BaseCapacity is < 0 or > 99) return "Base capacity must be from 0 to 99.";
                break;
        }

        return null;
    }

    // ===== TAKING THE LEVEL =====
    public async Task<LevelUpResult> ApplyAsync(LevelUpRequest r)
    {
        string? error = Validate(r);
        if (error != null) return LevelUpResult.Fail(error);

        var cls = r.Class;
        bool hosted = SessionMode.GmHosted;
        int hpBefore = _svc.BaseHitpoints;
        int fatigueBefore = _svc.FatigueBarMax;

        // 1. Ability points: permanent. Pack size, Hit Protection and the Fatigue Bar follow from the new scores.
        foreach (var (stat, points) in r.AbilityPoints.Where(p => p.Value > 0)) AddToScore(stat, points);

        // 2. The class pick. Level is the total of picks.
        int picks = ClassPicks(cls.Id) + 1;
        _svc.ClassSelections[cls.Id] = picks;
        int newLevel = _svc.CharacterLevel;

        var notes = new List<string>();
        var benefits = new List<string>();

        // 3. Recurring benefits that are plain numbers. Hit Protection, the Fatigue Bar and the dice pools are computed from the picks.
        if (cls.GrantsDicePool is { } pool)
            benefits.Add($"+{cls.DicePerSelection} {DicePools.Name(pool)}");

        if (cls.FavourPerSelection > 0)
        {
            _svc.Favour += cls.FavourPerSelection;
            benefits.Add($"+{cls.FavourPerSelection} Favour");
        }

        if (cls.TrainingPointsPerSelection > 0)
        {
            _svc.UnspentTrainingPoints += cls.TrainingPointsPerSelection;
            benefits.Add($"+{cls.TrainingPointsPerSelection} training points");
        }

        // 4. The feature.
        string featureText = string.Empty;
        if (r.Feature != null)
        {
            _svc.AddFeature(r.Feature.Id);
            featureText = r.Feature.Name;

            if (r.FeatureSubFeature != null)
            {
                var result = await _subFeatures.BestowAsync(r.FeatureSubFeature);
                if (result.Success) featureText += $" ({r.FeatureSubFeature.Name})";
                else notes.Add($"{r.FeatureSubFeature.Name} couldn't be added: {result.Message}");
            }
        }

        // 5. The class's own choice.
        switch (cls.RecurringChoice)
        {
            case ClassChoice.Weapon when r.WarriorWeapon != null:
                await _proficiency.GrantWarriorWeaponAsync(r.WarriorWeapon);   // proficiency only: nothing goes in the pack
                benefits.Add($"{r.WarriorWeapon.Name} weapon skill proficiency");
                break;

            case ClassChoice.Quip when r.Quip != null:
                var quip = await _subFeatures.BestowAsync(r.Quip);
                if (quip.Success) benefits.Add($"quip {r.Quip.Name}");
                else notes.Add($"{r.Quip.Name} couldn't be added: {quip.Message}");
                break;

            case ClassChoice.Construct when r.Construct != null:
                _svc.AcquiredFamiliars.Add(BuildConstruct(r.Construct));
                benefits.Add($"construct {r.Construct.Name.Trim()}");
                break;
        }

        _svc.UpdateEncumbranceState();

        int hpAfter = _svc.BaseHitpoints;
        int fatigueAfter = _svc.FatigueBarMax;
        if (hpAfter != hpBefore) benefits.Insert(0, $"Hit Protection {hpBefore} → {hpAfter}");
        if (fatigueAfter != fatigueBefore) benefits.Insert(hpAfter != hpBefore ? 1 : 0, $"Fatigue Bar {fatigueBefore} → {fatigueAfter}");

        // 6. A hosted level uses up one the GM granted.
        if (hosted) _svc.PendingLevels = Math.Max(0, _svc.PendingLevels - 1);

        // 7. The log.
        string abilities = string.Join(", ", r.AbilityPoints.Where(p => p.Value > 0).Select(p => $"+{p.Value} {p.Key}"));
        string summary = $"Level {newLevel}: {cls.ClassName} ({LevelUpRules.Ordinal(picks)} pick)" +
                         (featureText.Length > 0 ? $", feature {featureText}" : "") +
                         $". Ability points: {abilities}" +
                         (benefits.Count > 0 ? ". " + string.Join(", ", benefits) : "");
        await LogAsync(PlayerActionType.LevelGained, summary, hosted ? ActionSource.GM : ActionSource.Player);

        if (!hosted)
            await LogAsync(PlayerActionType.RuleBypassed, $"Took level {newLevel} without a GM award");
        if (newLevel > LevelUpRules.UsualMaxLevel)
            await LogAsync(PlayerActionType.RuleBypassed, $"Reached level {newLevel}, past the usual maximum of {LevelUpRules.UsualMaxLevel}");
        if (picks > LevelUpRules.UsualMaxPicksPerClass)
            await LogAsync(PlayerActionType.RuleBypassed,
                $"Took {cls.ClassName} for the {LevelUpRules.Ordinal(picks)} time, past the usual maximum of {LevelUpRules.UsualMaxPicksPerClass} per class");

        await _svc.SaveNowAsync();

        // 8. The notice.
        var toast = new List<string> { $"{cls.ClassName} · {LevelUpRules.Ordinal(picks)} pick" };
        if (featureText.Length > 0) toast.Add(featureText);
        toast.AddRange(benefits);
        GameNotices.Post(new GameNotice($"⬆️ Level {newLevel}", string.Join(" · ", toast),
            hosted ? NoticeSource.Gm : NoticeSource.Player, 8));

        return new LevelUpResult { Success = true, NewLevel = newLevel, Message = summary, Notes = notes };
    }

    // ===== THE GM'S AWARD (multiplayer) =====
    // Players can stack several. Single-player has no use for it: the button is always available there.
    public async Task GrantLevelsAsync(int count)
    {
        if (count < 1) return;

        _svc.PendingLevels += count;
        await LogAsync(PlayerActionType.LevelGained,
            $"The GM granted {count} level-up(s); {_svc.PendingLevels} waiting", ActionSource.GM);
        await _svc.SaveNowAsync();

        GameNotices.Post(new GameNotice("⬆️ Level up available",
            _svc.PendingLevels == 1 ? "Your GM granted you a level." : $"Your GM granted you levels: {_svc.PendingLevels} waiting.",
            NoticeSource.Gm, 8));
    }

    // ===== PLUMBING =====
    private void AddToScore(string stat, int points)
    {
        switch (stat)
        {
            case "Vigor": _svc.StatVigor = (_svc.StatVigor ?? 10) + points; break;
            case "Agility": _svc.StatAgility = (_svc.StatAgility ?? 10) + points; break;
            case "Mind": _svc.StatMind = (_svc.StatMind ?? 10) + points; break;
            case "Spirit": _svc.StatSpirit = (_svc.StatSpirit ?? 10) + points; break;
        }
    }

    // The stats exist only so shared familiar code works; the panels hide them for Inanimate familiars.
    private static Familiar BuildConstruct(ConstructSpec spec) => new()
    {
        Guid = Guid.NewGuid(),
        Id = 0,
        IsLoaded = true,
        SourcePackId = "Level Up",
        LastModified = DateTime.UtcNow,
        FmlrName = spec.Name.Trim(),
        FmlrDescription = string.IsNullOrWhiteSpace(spec.Description) ? null : spec.Description.Trim(),
        FmlrClass = FmlrClasses.Construct,
        FmlrSize = FmlrSizes.Small,
        FmlrIntelligence = FmlrIntelligences.Inanimate,
        HP = 1,
        StatVigor = 10,
        StatAgility = 10,
        StatMind = 10,
        StatSpirit = 10,
        IsPlayerCreated = true,
        IsFoundation = false,
        BaseInventoryCapacity = spec.BaseCapacity,
        InstanceId = Guid.NewGuid()
    };

    private Task LogAsync(PlayerActionType type, string summary, ActionSource source = ActionSource.Player)
        => _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = type,
            Source = source,
            PlayerName = _svc.PlayerName,
            PlayerId = _svc.PlayerId,
            CharacterName = _svc.CharacterName,
            CharacterGuid = _svc.CharacterGuid,
            Summary = summary
        });
}