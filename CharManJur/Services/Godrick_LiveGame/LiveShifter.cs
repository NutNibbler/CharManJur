using System.ComponentModel.DataAnnotations;
using System.Reflection;
using CharManJur.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CharManJur.Services;

// The form the chosen traits add up to (with the guideline warnings they trip).
public sealed class FormPreview
{
    public int Vigor { get; init; } = 10;
    public int Agility { get; init; } = 10;
    public bool VigorClash { get; init; }
    public bool AgilityClash { get; init; }
    public int Armor { get; init; }
    public string? ArmorName { get; init; }        // the armoring trait's name
    public int DurationMinutes { get; init; } = 10;
    public List<string> Warnings { get; init; } = new();
}

public sealed class TransformRequest
{
    public IReadOnlyList<int> Rolled { get; init; } = Array.Empty<int>();
    public IReadOnlyList<ClassTrait> Traits { get; init; } = Array.Empty<ClassTrait>();
    public int Vigor { get; init; } = 10;
    public int Agility { get; init; } = 10;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool SaveForm { get; init; }
    // The element picked for each trait that lets the player choose one (trait name -> element).
    public IReadOnlyDictionary<string, DamageElement> Elements { get; init; } = new Dictionary<string, DamageElement>();
}

public sealed class DamageOutcome
{
    public bool NeedsSave { get; init; }
    public bool Reverted { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class SaveOutcome
{
    public bool Passed { get; init; }
    public bool Reverted { get; init; }
    public string Message { get; init; } = string.Empty;
}

// A Shifter's transformation: rolling Polymorph Dice for Stability, picking traits, taking damage as Stability
// damage, Stability saves, and reverting. The trait rules are guidelines: they warn, never block.
//
// While transformed, the form's VIG and AGI replace the character's for Hit Protection, the Fatigue Bar,
// skill checks and saves (the data service does this through TotalStatVigor / TotalStatAgility). Pack capacity
// keeps following the character's normal Vigor. Reverting restores the Vigor and Agility the character had
// before transforming.
public class LiveShifter
{
    private const int DefaultAbility = 10;
    private const int DefaultDurationMinutes = 10;

    private readonly ICharAttribDataService _svc;
    private readonly IClassDataService _classes;
    private readonly IPlayerActionLogService _log;
    private readonly ITraitDataService _traits;

    public LiveShifter(ICharAttribDataService svc, IClassDataService classes, IPlayerActionLogService log, ITraitDataService traits)
    {
        _svc = svc;
        _classes = classes;
        _log = log;
        _traits = traits;
    }

    public static LiveShifter Create(ICharAttribDataService svc)
    {
        var services = IPlatformApplication.Current!.Services;
        return new LiveShifter(
            svc,
            services.GetRequiredService<IClassDataService>(),
            services.GetRequiredService<IPlayerActionLogService>(),
            services.GetRequiredService<ITraitDataService>());
    }

    public TransformState? State => _svc.Transform is { IsTransformed: true } t ? t : null;
    public bool IsTransformed => State != null;
    public int PoolSize => _svc.GetDicePoolSize(DicePool.Polymorph);

    // The class the character picked that has traits (the Shifter), if any.
    public async Task<CharacterClass?> ShifterClassAsync()
    {
        var all = await _classes.GetClassesAsync();
        return all.FirstOrDefault(c => c.Traits.Count > 0 && _svc.ClassSelections.ContainsKey(c.Id));
    }

    // Custom traits that are loaded (they join the foundation traits in the picker).
    public async Task<List<ClassTrait>> CustomTraitsAsync()
        => (await _traits.GetLoadedTraitsAsync()).Cast<ClassTrait>().ToList();

    // ===== TRAITS AND THE FORM =====
    // 1 die = 2 traits ... 4 dice = 5 traits (more dice than that don't add traits).
    public static int TraitsAllowed(int dice) => Math.Min(Math.Max(dice, 1), 4) + 1;

    // The traits reachable with this many dice (PD 1 for one die, PD 1-2 for two, and so on).
    // The PD rule applies to custom traits too: a trait only appears once its PD is invested.
    public static List<ClassTrait> TraitsFor(CharacterClass cls, int dice, IEnumerable<ClassTrait>? custom = null)
        => cls.Traits.Concat(custom ?? Enumerable.Empty<ClassTrait>())
                     .Where(t => t.PD <= Math.Max(dice, 1))
                     .OrderBy(t => t.PD)
                     .ThenBy(t => t.TraitName, StringComparer.OrdinalIgnoreCase)
                     .ToList();

    // Used when the player leaves the name blank.
    public static string GeneratedName(IEnumerable<ClassTrait> picked)
    {
        var names = picked.Select(t => t.TraitName).ToList();
        if (names.Count == 0) return "Custom form";
        return string.Join(" + ", names.Take(3)) + (names.Count > 3 ? " …" : "") + " form";
    }

    public static FormPreview Preview(IReadOnlyList<ClassTrait> picked, int dice)
    {
        dice = Math.Max(1, dice);
        var warnings = new List<string>();

        int allowed = TraitsAllowed(dice);
        if (picked.Count != allowed)
            warnings.Add($"Your Polymorph Dice allow {allowed} traits and you picked {picked.Count}.");

        foreach (var t in picked.Where(t => t.PD > dice))
            warnings.Add($"{t.TraitName} needs {t.PD} Polymorph Dice and you invested {dice}.");

        bool hasSmall = picked.Any(t => t.IsSmall);
        bool hasLarge = picked.Any(t => t.IsLarge);
        foreach (var t in picked.Where(t => t.RequiresSmall && !hasSmall))
            warnings.Add($"{t.TraitName} requires the Small trait.");
        foreach (var t in picked.Where(t => t.ForbidsLarge && hasLarge))
            warnings.Add($"{t.TraitName} doesn't work for Large forms.");
        if (hasSmall && hasLarge)
            warnings.Add("A form can't be both Small and Large.");

        var (vigor, vigorClash) = Resolve(picked.Where(t => t.Vigor.HasValue).Select(t => t.Vigor!.Value));
        var (agility, agilityClash) = Resolve(picked.Where(t => t.Agility.HasValue).Select(t => t.Agility!.Value));

        // Armor takes the highest value; the trait that gives it names the armor on the dashboard.
        var armoring = picked.Where(t => t.Armor > 0).OrderByDescending(t => t.Armor).FirstOrDefault();

        return new FormPreview
        {
            Vigor = vigor,
            Agility = agility,
            VigorClash = vigorClash,
            AgilityClash = agilityClash,
            Armor = armoring?.Armor ?? 0,
            ArmorName = armoring?.TraitName,
            DurationMinutes = picked.Where(t => t.DurationMinutes.HasValue)
                                    .Select(t => t.DurationMinutes!.Value)
                                    .DefaultIfEmpty(DefaultDurationMinutes).Max(),
            Warnings = warnings
        };
    }

    // Values on the same side of 10 agree: the most extreme wins (Large 16 + Strong 18 = 18).
    // Values on opposite sides clash (Small's VIG 2 + Strong's 18): the middle value is proposed.
    private static (int Value, bool Clash) Resolve(IEnumerable<int> values)
    {
        var list = values.ToList();
        if (list.Count == 0) return (DefaultAbility, false);

        int min = list.Min(), max = list.Max();
        if (min == max) return (min, false);

        bool above = list.Any(v => v > DefaultAbility);
        bool below = list.Any(v => v < DefaultAbility);
        if (above && below) return ((min + max) / 2, true);
        return (above ? max : min, false);
    }

    // Every weapon trait becomes a natural weapon of the form, named after the trait.
    public static List<FormWeapon> BuildWeapons(IEnumerable<ClassTrait> picked, IReadOnlyDictionary<string, DamageElement>? elements)
    {
        var list = new List<FormWeapon>();
        foreach (var t in picked.Where(t => t.WeaponDie.HasValue))
        {
            var element = t.Element;
            if (t.ChooseElement)
                element = elements != null && elements.TryGetValue(t.TraitName, out var chosen) ? chosen : DamageElement.None;

            list.Add(new FormWeapon
            {
                Name = t.TraitName,
                Die = t.WeaponDie!.Value,
                Type = t.WeaponType,
                Effects = t.Blast ? new List<WeaponEffectType> { WeaponEffectType.Blast } : new List<WeaponEffectType>(),
                Element = element
            });
        }
        return list;
    }

    // "1d8 · Slashing · Fire"
    public static string WeaponSummary(FormWeapon w)
    {
        var parts = new List<string> { $"1d{w.Die}" };
        if (w.Type.HasValue) parts.Add(DisplayName(w.Type.Value));
        if (w.Element != DamageElement.None) parts.Add(w.Element.ToString());
        return string.Join(" · ", parts);
    }

    // "Blast"
    public static string WeaponEffectsText(FormWeapon w)
        => string.Join(", ", w.Effects.Select(e => DisplayName(e)));

    private static string DisplayName<T>(T value) where T : struct, Enum
    {
        string name = value.ToString();
        var attribute = typeof(T).GetField(name)?.GetCustomAttribute<DisplayAttribute>();
        return attribute?.Name ?? name;
    }

    public static string DurationText(int minutes) => minutes switch
    {
        >= 1440 when minutes % 1440 == 0 => minutes == 1440 ? "1 day" : $"{minutes / 1440} days",
        >= 60 when minutes % 60 == 0 => minutes == 60 ? "1 hour" : $"{minutes / 60} hours",
        _ => $"{minutes} minutes"
    };

    public static string FormSummary(TransformState s)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(s.Name)) lines.Add(s.Name);

        var parts = new List<string>
        {
            $"Stability {s.StabilityCurrent}/{s.StabilityMax}",
            $"VIG {s.FormVigor}",
            $"AGI {s.FormAgility}"
        };
        if (s.Armor > 0) parts.Add(string.IsNullOrEmpty(s.ArmorName) ? $"Armor {s.Armor}" : $"{s.ArmorName}: Armor {s.Armor}");
        parts.Add($"Lasts {DurationText(s.DurationMinutes)}");
        lines.Add(string.Join(" · ", parts));

        foreach (var w in s.Weapons)
        {
            string line = $"{w.Name}: {WeaponSummary(w)}";
            string effects = WeaponEffectsText(w);
            if (effects.Length > 0) line += $" ({effects})";
            lines.Add(line);
        }

        if (s.Traits.Count > 0) lines.Add("Traits: " + string.Join(", ", s.Traits));
        if (!string.IsNullOrWhiteSpace(s.Description)) lines.Add(s.Description);
        return string.Join("\n\n", lines);
    }

    // ===== SAVED FORMS =====
    public IReadOnlyList<SavedForm> SavedForms => _svc.SavedForms;

    public async Task DeleteSavedFormAsync(SavedForm form)
    {
        _svc.SavedForms.RemoveAll(f => string.Equals(f.Name, form.Name, StringComparison.OrdinalIgnoreCase));
        await _svc.SaveNowAsync();
    }

    // A form with the same name is replaced.
    private void UpsertSavedForm(string name, string description, int dice, IEnumerable<string> traits,
        int vigor, int agility, Dictionary<string, DamageElement> elements)
    {
        _svc.SavedForms.RemoveAll(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
        _svc.SavedForms.Add(new SavedForm
        {
            Name = name,
            Description = description,
            PolymorphDice = dice,
            Traits = traits.ToList(),
            Vigor = vigor,
            Agility = agility,
            Elements = elements,
            SavedAt = DateTime.Now
        });
    }

    // ===== TRANSFORM =====
    public async Task<FeatureUseResult> TransformAsync(TransformRequest req)
    {
        if (IsTransformed) return Fail("You're already transformed. Revert first.");

        var cls = await ShifterClassAsync();
        if (cls == null) return Fail("Only a Shifter can transform.");

        int dice = req.Rolled.Count;
        if (dice < 1) return Fail("Roll the Polymorph Dice first.");
        if (dice > PoolSize) return Fail($"You only have {PoolSize} Polymorph {(PoolSize == 1 ? "Die" : "Dice")}.");
        if (req.Rolled.Any(d => d < 1 || d > 6)) return Fail("Every die must be a number from 1 to 6.");
        if (req.Vigor < 1 || req.Vigor > 30 || req.Agility < 1 || req.Agility > 30)
            return Fail("VIG and AGI must be whole numbers from 1 to 30.");

        // A trait that lets the player choose an element needs one.
        foreach (var t in req.Traits.Where(t => t.ChooseElement))
        {
            if (!req.Elements.TryGetValue(t.TraitName, out var element) || element == DamageElement.None)
                return Fail($"Choose an element for {t.TraitName}.");
        }

        var preview = Preview(req.Traits, dice);
        string name = string.IsNullOrWhiteSpace(req.Name) ? GeneratedName(req.Traits) : req.Name.Trim();
        var weapons = BuildWeapons(req.Traits, req.Elements);
        int stability = req.Rolled.Sum();
        int fatigue = req.Rolled.Count(d => d >= 4);   // every die showing 4-6; no series damage when transforming
        if (fatigue > 0) _svc.AddFatigue(fatigue);

        var state = new TransformState
        {
            IsTransformed = true,
            Name = name,
            Description = req.Description?.Trim() ?? string.Empty,
            PolymorphDice = dice,
            StabilityMax = stability,
            StabilityCurrent = stability,
            FormVigor = req.Vigor,
            FormAgility = req.Agility,
            Armor = preview.Armor,
            ArmorName = preview.ArmorName,
            Weapons = weapons,
            DurationMinutes = preview.DurationMinutes,
            Traits = req.Traits.Select(t => t.TraitName).ToList(),
            StartedAt = DateTime.Now,
            // The Vigor and Agility the character had before: reverting puts them back.
            PreVigorAdjustment = _svc.CurrentVigorAdjustment,
            PreAgilityAdjustment = _svc.CurrentAgilityAdjustment
        };

        // The form's VIG and AGI replace the character's. Ability loss starts fresh on the form.
        _svc.CurrentVigorAdjustment = 0;
        _svc.CurrentAgilityAdjustment = 0;
        _svc.Transform = state;

        // A carrying trait (Beast of Burden and the like) gives the form its own pack.
        if (req.Traits.Any(t => t.GrantsPack)) EnsureFormPack(name);

        if (req.SaveForm)
        {
            var picked = req.Traits.Where(t => t.ChooseElement)
                .ToDictionary(t => t.TraitName, t => req.Elements[t.TraitName]);
            UpsertSavedForm(name, state.Description, dice, state.Traits, req.Vigor, req.Agility, picked);
        }

        // The chip stays on the dashboard until the form ends. Tapping it opens the form's actions.
        _svc.ActiveEffects.RemoveAll(e => e.HardcodedType == HardcodedEffectType.Transformed);
        _svc.ActiveEffects.Add(new CharacterEffect
        {
            Name = ChipName(state),
            Note = FormSummary(state),
            Duration = EffectDurationType.Persistent,
            HardcodedType = HardcodedEffectType.Transformed
        });

        var notes = new List<string>
        {
            $"rolled {dice} Polymorph {(dice == 1 ? "Die" : "Dice")} ({string.Join(", ", req.Rolled)})",
            $"Stability {stability}"
        };
        if (fatigue > 0) notes.Add($"fatigue +{fatigue}");
        notes.Add($"VIG {req.Vigor}, AGI {req.Agility}");
        if (req.Vigor != preview.Vigor || req.Agility != preview.Agility)
            notes.Add($"form scores changed from the proposed VIG {preview.Vigor}/AGI {preview.Agility} (GM approval needed)");
        if (preview.Armor > 0) notes.Add($"{preview.ArmorName ?? "armor"} (armor {preview.Armor})");
        foreach (var w in weapons) notes.Add($"{w.Name}: {WeaponSummary(w)}");
        notes.Add($"lasts {DurationText(preview.DurationMinutes)}");
        if (state.Traits.Count > 0) notes.Add("traits: " + string.Join(", ", state.Traits));
        if (state.Description.Length > 0) notes.Add($"description: {state.Description}");

        foreach (var w in preview.Warnings)
            await LogAsync(PlayerActionType.RuleBypassed, $"Transformed outside the trait guidelines: {w}");
        await LogAsync(PlayerActionType.Transformed, $"Transformed into {name} — " + string.Join("; ", notes));

        await _svc.SaveNowAsync();
        return new FeatureUseResult
        {
            Success = true,
            Message = $"You transform into {name}. Stability {stability}" + (fatigue > 0 ? $", fatigue +{fatigue}." : ".") +
                      (preview.Warnings.Count > 0 ? " Noted as rule skating." : string.Empty)
        };
    }

    // ===== DAMAGE AND SAVES =====
    // Hit Protection damage hits Stability while transformed. Armor absorbs first: worn armor plus the
    // form's armoring trait, which the data service already caps at 3.
    public async Task<DamageOutcome> TakeDamageAsync(int amount)
    {
        var s = State;
        if (s == null || amount <= 0) return new DamageOutcome { Message = "No damage applied." };

        int armor = _svc.ArmorRating;
        int absorbed = Math.Min(armor, amount);
        int lost = amount - absorbed;
        int before = s.StabilityCurrent;
        s.StabilityCurrent = Math.Max(0, before - lost);

        string armorText = absorbed > 0 ? $"{absorbed} absorbed by armor. " : string.Empty;

        if (lost == 0)
        {
            await LogAsync(PlayerActionType.StabilityDamaged, $"Took {amount} damage while transformed, fully absorbed by armor");
            await _svc.SaveNowAsync();
            return new DamageOutcome { Message = $"{amount} damage, fully absorbed by armor. Your Stability is unchanged." };
        }

        await LogAsync(PlayerActionType.StabilityDamaged,
            $"Took {amount} damage while transformed: {armorText}{lost} to Stability ({before} → {s.StabilityCurrent})");

        if (s.StabilityCurrent <= 0)
        {
            await RevertAsync("Stability reached 0");
            return new DamageOutcome
            {
                Reverted = true,
                Message = $"{armorText}{lost} damage to Stability ({before} → 0). Your Stability is gone: you revert to your normal form."
            };
        }

        RefreshChip(s);
        await _svc.SaveNowAsync();
        return new DamageOutcome
        {
            NeedsSave = true,
            Message = $"{armorText}{lost} damage to Stability ({before} → {s.StabilityCurrent}). Make a Stability save."
        };
    }

    // d20: equal to or under the current Stability passes. A 1 always passes and a 20 always fails.
    public async Task<SaveOutcome> ResolveSaveAsync(int roll)
    {
        var s = State;
        if (s == null) return new SaveOutcome { Message = "You aren't transformed." };

        bool passed = roll == 1 || (roll != 20 && roll <= s.StabilityCurrent);
        await LogAsync(PlayerActionType.StabilitySaveRolled,
            $"Stability save: rolled {roll} against {s.StabilityCurrent} — {(passed ? "passed" : "failed")}");

        if (!passed)
        {
            await RevertAsync("failed a Stability save");
            return new SaveOutcome { Reverted = true, Message = $"Failed: rolled {roll} against {s.StabilityCurrent}. Your form ends and you revert." };
        }

        return new SaveOutcome { Passed = true, Message = $"Passed: rolled {roll} against {s.StabilityCurrent}. You hold your form." };
    }

    // Combat ended and you're still transformed: Stability returns to the rolled value.
    public async Task RestoreStabilityAsync()
    {
        var s = State;
        if (s == null) return;

        int before = s.StabilityCurrent;
        s.StabilityCurrent = s.StabilityMax;
        RefreshChip(s);
        await LogAsync(PlayerActionType.StabilityDamaged, $"Combat ended: Stability restored ({before} → {s.StabilityMax})");
        await _svc.SaveNowAsync();
    }

    public async Task RevertAsync(string reason)
    {
        var t = _svc.Transform;
        if (t == null) return;

        string name = t.Name;

        // The Vigor and Agility from before the transformation come back, whatever happened to the form's.
        _svc.CurrentVigorAdjustment = t.PreVigorAdjustment;
        _svc.CurrentAgilityAdjustment = t.PreAgilityAdjustment;

        _svc.Transform = null;
        _svc.ActiveEffects.RemoveAll(e => e.HardcodedType == HardcodedEffectType.Transformed);
        await ResolveFormPackAsync();
        await LogAsync(PlayerActionType.Reverted,
            $"Reverted to normal form{(string.IsNullOrWhiteSpace(name) ? "" : $" (was {name})")} ({reason})");
        await _svc.SaveNowAsync();
    }

    // ===== FORM PACK =====
    // The form's pack is a hidden-name construct in the familiar list: capacity comes only from the containers
    // equipped in it, with no limit on how many. It exists only while the form does.
    private Familiar EnsureFormPack(string formName)
    {
        var existing = _svc.AcquiredFamiliars.FirstOrDefault(f => f.IsFormPack);
        if (existing != null) return existing;

        var pack = new Familiar
        {
            Id = -1,
            IsFormPack = true,
            IsPlayerCreated = false,
            FmlrName = string.IsNullOrWhiteSpace(formName) ? "Form pack" : $"{formName} pack",
            FmlrDescription = "Your form's pack. Equip containers to give it room; it goes away when the form ends.",
            FmlrSize = FmlrSizes.Medium,
            FmlrIntelligence = FmlrIntelligences.Inanimate
        };
        pack.EnsureInstanceId();
        _svc.AcquiredFamiliars.Add(pack);
        return pack;
    }

    // Safety net: a form pack left in the list when there is no form (an old save, an interrupted revert)
    // is settled the same way a revert settles it, and disappears from the list.
    public async Task RemoveStrayFormPackAsync()
    {
        if (IsTransformed) return;
        if (_svc.AcquiredFamiliars.Any(f => f.IsFormPack)) await ResolveFormPackAsync();
    }

    // On revert: everything in the form pack comes back to the character's pack, or is left behind.
    private async Task ResolveFormPackAsync()
    {
        var pack = _svc.AcquiredFamiliars.FirstOrDefault(f => f.IsFormPack);
        if (pack == null) return;

        var items = pack.Inventory.Where(i => !i.IsDropped && !i.IsEmpty).ToList();
        if (items.Count == 0)
        {
            _svc.RemoveFamiliar(pack.InstanceId);
            return;
        }

        int incoming = pack.InventorySlotsUsed;
        int room = Math.Max(0, _svc.InventorySlotsAvailable - _svc.InventorySlotsUsed);
        bool take = true;

        if (_svc.IsPackDropped)
        {
            take = false;   // your own pack is on the ground, there's nowhere to put them
            await ShowAlertAsync("Pack Dropped",
                "Your own pack is dropped, so the items from your form's pack are left behind. You can recover them like any dropped item.");
        }
        else if (incoming > room)
        {
            take = await ConfirmAsync("Your pack can't hold everything",
                $"Your form's pack holds {incoming} slot(s) of items and you have room for {room}. " +
                "Take everything and be encumbered, or drop the pack and leave the items behind (they can be recovered like dropped items)?",
                "Take everything", "Drop the pack");
        }

        if (take)
        {
            // Containers stop being equipped once they're in your pack.
            foreach (var item in items.Where(i => i.IsEquipped))
                _svc.SetFamiliarItemEquipped(pack.InstanceId, item.Id, false);
            foreach (var item in items)
                _svc.TakeItemFromFamiliar(pack.InstanceId, item.Id, item.Quantity);
            await LogAsync(PlayerActionType.ItemTakenFromFamiliar,
                $"Took everything from the form pack ({items.Count} item record(s))");
        }
        else
        {
            // Left-behind items go to the recovery table, just like dropped items.
            _svc.DropFormPackItems(pack.InstanceId);
        }

        _svc.RemoveFamiliar(pack.InstanceId);
    }

    private static Task ShowAlertAsync(string title, string message)
        => Application.Current?.MainPage?.DisplayAlertAsync(title, message, "OK") ?? Task.CompletedTask;

    private static async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        var page = Application.Current?.MainPage;
        return page == null || await page.DisplayAlertAsync(title, message, accept, cancel);
    }

    // ===== PLUMBING =====
    private static string ChipName(TransformState s)
    {
        string name = string.IsNullOrWhiteSpace(s.Name) ? "Transformed" : s.Name;
        if (name.Length > 18) name = name[..17] + "…";
        return $"🐺 {name} {s.StabilityCurrent}/{s.StabilityMax}";
    }

    private void RefreshChip(TransformState s)
    {
        var chip = _svc.ActiveEffects.FirstOrDefault(e => e.HardcodedType == HardcodedEffectType.Transformed);
        if (chip == null) return;
        chip.Name = ChipName(s);
        chip.Note = FormSummary(s);
    }

    private Task LogAsync(PlayerActionType type, string summary)
        => _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = type,
            PlayerName = _svc.PlayerName,
            PlayerId = _svc.PlayerId,
            CharacterName = _svc.CharacterName,
            CharacterGuid = _svc.CharacterGuid,
            Summary = summary
        });

    private static FeatureUseResult Fail(string message) => new() { Success = false, Message = message };
}