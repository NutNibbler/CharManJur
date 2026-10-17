using CharManJur.Models;

namespace CharManJur.Services;

// A custom entry is identified by its Guid; a foundation entry (empty Guid) by its Id.
// The numeric Id of a custom entry is only a runtime slot, so it is never compared when a Guid exists.
public static class SubFeatureIdentity
{
    public static bool Same(Guid guidA, int idA, Guid guidB, int idB)
        => guidA != Guid.Empty || guidB != Guid.Empty ? guidA == guidB : idA == idB;
}

public sealed class SubFeatureResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

// One Spell, Quip, Blueprint or Technique as the live-game screens see it.
public sealed class SubFeatureEntry
{
    public SubFeatureType Type { get; init; }
    public object Source { get; init; } = null!;   // the Spell / Quip / Blueprint / Technique itself
    public Guid Guid { get; init; }
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DetailLine { get; init; } = string.Empty;

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasDetailLine => !string.IsNullOrWhiteSpace(DetailLine);

    public bool IsSameAs(SubFeatureEntry other)
        => Type == other.Type && SubFeatureIdentity.Same(Guid, Id, other.Guid, other.Id);

    public static SubFeatureEntry From(object source) => source switch
    {
        Spell s => new SubFeatureEntry
        {
            Type = SubFeatureType.Spell,
            Source = s,
            Guid = s.Guid,
            Id = s.Id,
            Name = s.SpellName,
            Description = s.SpellDescription ?? string.Empty,
            DetailLine = SpellDetail(s)
        },
        Quip q => new SubFeatureEntry
        {
            Type = SubFeatureType.Quip,
            Source = q,
            Guid = q.Guid,
            Id = q.Id,
            Name = q.QuipName,
            Description = q.QuipDescription ?? string.Empty
        },
        Blueprint b => new SubFeatureEntry
        {
            Type = SubFeatureType.Blueprint,
            Source = b,
            Guid = b.Guid,
            Id = b.Id,
            Name = b.BlueprintName,
            Description = b.BlueprintDescription,
            DetailLine = $"Material cost: {b.BlueprintCost}"
        },
        Technique t => new SubFeatureEntry
        {
            Type = SubFeatureType.Technique,
            Source = t,
            Guid = t.Guid,
            Id = t.Id,
            Name = t.TechniqueName,
            Description = t.TechniqueDescription
        },
        _ => throw new ArgumentException("Not a sub-feature.", nameof(source))
    };

    private static string SpellDetail(Spell s)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(s.SpellDice)) parts.Add($"Dice: {s.SpellDice}");
        if (!string.IsNullOrWhiteSpace(s.SpellRange)) parts.Add($"Range: {s.SpellRange}");
        return string.Join("   ", parts);
    }
}

// Live-game handling of the character's sub-features: list, bestow, forget, use. Every change is logged for the GM.
// The creation-time AddSpell/AddQuip/etc. neither log nor autosave, so this class edits the lists itself.
public class LiveSubFeatures
{
    private readonly ICharAttribDataService _svc;
    private readonly IBlueprintDataService _blueprints;
    private readonly IQuipDataService _quips;
    private readonly ISpellDataService _spells;
    private readonly ITechniqueDataService _techniques;
    private readonly IPlayerActionLogService _log;

    public LiveSubFeatures(
        ICharAttribDataService svc,
        IBlueprintDataService blueprints,
        IQuipDataService quips,
        ISpellDataService spells,
        ITechniqueDataService techniques,
        IPlayerActionLogService log)
    {
        _svc = svc;
        _blueprints = blueprints;
        _quips = quips;
        _spells = spells;
        _techniques = techniques;
        _log = log;
    }

    // ===== NAMES =====
    public static string TypeName(SubFeatureType type) => type switch
    {
        SubFeatureType.Spell => "Spell",
        SubFeatureType.Quip => "Quip",
        SubFeatureType.Blueprint => "Blueprint",
        _ => "Technique"
    };

    public static string PluralName(SubFeatureType type) => TypeName(type) + "s";

    public static string Icon(SubFeatureType type) => type switch
    {
        SubFeatureType.Spell => "📜",
        SubFeatureType.Quip => "⚡",
        SubFeatureType.Blueprint => "📐",
        _ => "🌀"
    };

    // ===== WHAT THE CHARACTER KNOWS / WHAT EXISTS =====
    public List<SubFeatureEntry> Owned(SubFeatureType type)
    {
        IEnumerable<object> source = type switch
        {
            SubFeatureType.Spell => _svc.AcquiredSpells.Cast<object>(),
            SubFeatureType.Quip => _svc.AcquiredQuips.Cast<object>(),
            SubFeatureType.Blueprint => _svc.AcquiredBlueprints.Cast<object>(),
            _ => _svc.AcquiredTechniques.Cast<object>()
        };
        return source.Select(SubFeatureEntry.From)
                     .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                     .ToList();
    }

    public bool IsOwned(SubFeatureEntry entry) => Owned(entry.Type).Any(o => o.IsSameAs(entry));

    // Everything that exists (loaded) for a type: the bestow page's list. The plain lists may hold only the
    // foundation entries, so loaded custom entries are added from the custom trackers; duplicates are dropped.
    public async Task<List<SubFeatureEntry>> CatalogAsync(SubFeatureType type)
    {
        IEnumerable<object> all = type switch
        {
            SubFeatureType.Spell => (await _spells.GetSpellsAsync()).Cast<object>()
                .Concat((await _spells.GetCustomSpellsAsync()).Where(s => s.IsLoaded).Cast<object>()),
            SubFeatureType.Quip => (await _quips.GetQuipsAsync()).Cast<object>()
                .Concat((await _quips.GetCustomQuipsAsync()).Where(q => q.IsLoaded).Cast<object>()),
            SubFeatureType.Blueprint => (await _blueprints.GetBlueprintsAsync()).Cast<object>()
                .Concat((await _blueprints.GetCustomBlueprintsAsync()).Where(b => b.IsLoaded).Cast<object>()),
            _ => (await _techniques.GetTechniquesAsync()).Cast<object>()
                .Concat((await _techniques.GetCustomTechniquesAsync()).Where(t => t.IsLoaded).Cast<object>())
        };

        var result = new List<SubFeatureEntry>();
        foreach (var entry in all.Select(SubFeatureEntry.From))
            if (!result.Any(r => r.IsSameAs(entry))) result.Add(entry);
        return result;
    }

    // ===== BESTOW / FORGET =====
    public async Task<SubFeatureResult> BestowAsync(SubFeatureEntry entry, bool createdJustNow = false)
    {
        string kind = TypeName(entry.Type).ToLowerInvariant();
        if (IsOwned(entry)) return Fail($"You already know {entry.Name}.");

        if (createdJustNow)
            await LogAsync(PlayerActionType.SubFeatureCreated, $"Created custom {kind}: {entry.Name}");

        AddToCharacter(entry);
        await LogAsync(PlayerActionType.SubFeatureBestowed, $"Bestowed {kind}: {entry.Name}");
        await _svc.SaveNowAsync();
        return Ok();
    }

    // Permanent: it only comes back if it is bestowed again.
    public async Task<SubFeatureResult> ForgetAsync(SubFeatureEntry entry)
    {
        if (!RemoveFromCharacter(entry)) return Fail($"You don't know {entry.Name}.");

        await LogAsync(PlayerActionType.SubFeatureForgotten,
            $"Forgot {TypeName(entry.Type).ToLowerInvariant()}: {entry.Name}");
        await _svc.SaveNowAsync();
        return Ok();
    }

    // ===== USE =====
    // Quips need no dice and no cost: using one is just logged. The other types get their own flows.
    public async Task<SubFeatureResult> UseAsync(SubFeatureEntry entry)
    {
        if (entry.Type != SubFeatureType.Quip) return Fail("That can't be used from here yet.");
        await LogAsync(PlayerActionType.SubFeatureUsed, $"Used quip: {entry.Name}");
        return Ok();
    }

    public Task LogAsync(PlayerActionType type, string summary)
        => _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = type,
            PlayerName = _svc.PlayerName,
            PlayerId = _svc.PlayerId,
            CharacterName = _svc.CharacterName,
            CharacterGuid = _svc.CharacterGuid,
            Summary = summary
        });

    // ===== LIST EDITS (the numeric id lists mirror what AddSpell/RemoveSpell and the others keep) =====
    private void AddToCharacter(SubFeatureEntry entry)
    {
        switch (entry.Source)
        {
            case Spell s:
                _svc.AcquiredSpells.Add(s);
                if (!_svc.AcquiredSpellIds.Contains(s.Id)) _svc.AcquiredSpellIds.Add(s.Id);
                break;
            case Quip q:
                _svc.AcquiredQuips.Add(q);
                if (!_svc.AcquiredQuipIds.Contains(q.Id)) _svc.AcquiredQuipIds.Add(q.Id);
                break;
            case Blueprint b:
                _svc.AcquiredBlueprints.Add(b);
                if (!_svc.AcquiredBlueprintIds.Contains(b.Id)) _svc.AcquiredBlueprintIds.Add(b.Id);
                break;
            case Technique t:
                _svc.AcquiredTechniques.Add(t);
                if (!_svc.AcquiredTechniqueIds.Contains(t.Id)) _svc.AcquiredTechniqueIds.Add(t.Id);
                break;
        }
    }

    private bool RemoveFromCharacter(SubFeatureEntry entry)
    {
        switch (entry.Source)
        {
            case Spell s:
                {
                    int n = _svc.AcquiredSpells.RemoveAll(x => SubFeatureIdentity.Same(x.Guid, x.Id, s.Guid, s.Id));
                    if (n > 0 && !_svc.AcquiredSpells.Any(x => x.Id == s.Id)) _svc.AcquiredSpellIds.Remove(s.Id);
                    return n > 0;
                }
            case Quip q:
                {
                    int n = _svc.AcquiredQuips.RemoveAll(x => SubFeatureIdentity.Same(x.Guid, x.Id, q.Guid, q.Id));
                    if (n > 0 && !_svc.AcquiredQuips.Any(x => x.Id == q.Id)) _svc.AcquiredQuipIds.Remove(q.Id);
                    return n > 0;
                }
            case Blueprint b:
                {
                    int n = _svc.AcquiredBlueprints.RemoveAll(x => SubFeatureIdentity.Same(x.Guid, x.Id, b.Guid, b.Id));
                    if (n > 0 && !_svc.AcquiredBlueprints.Any(x => x.Id == b.Id)) _svc.AcquiredBlueprintIds.Remove(b.Id);
                    return n > 0;
                }
            case Technique t:
                {
                    int n = _svc.AcquiredTechniques.RemoveAll(x => SubFeatureIdentity.Same(x.Guid, x.Id, t.Guid, t.Id));
                    if (n > 0 && !_svc.AcquiredTechniques.Any(x => x.Id == t.Id)) _svc.AcquiredTechniqueIds.Remove(t.Id);
                    return n > 0;
                }
            default:
                return false;
        }
    }

    private static SubFeatureResult Ok() => new() { Success = true };
    private static SubFeatureResult Fail(string message) => new() { Success = false, Message = message };
}