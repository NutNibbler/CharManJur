using System.Text.Json.Serialization;

namespace CharManJur.Models;

// What a field holds. Fixed per field: the GM never picks a type.
public enum NpcFieldKind
{
    SingleLineText,
    MultiLineText,
    KinChoice,        // one kin from the loaded kins (foundation and custom)
    FactionChoice,    // any of the campaign's factions
    LanguageChoice,   // any of the loaded languages
    Image,
    StatBlock         // HP, Vigor, Agility, Mind, Spirit
}

public sealed record NpcFieldSpec(string Key, string DefaultLabel, NpcFieldKind Kind, bool CanRename, string Hint);

// One NPC profile field as saved with a campaign. Only the label is saved; what the field is, and whether
// the GM may rename it, is decided by its key.
public class NpcFieldDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    [JsonIgnore] public NpcFieldKind Kind => NpcFieldDefaults.SpecFor(Key)?.Kind ?? NpcFieldKind.SingleLineText;
    [JsonIgnore] public bool CanRename => NpcFieldDefaults.SpecFor(Key)?.CanRename ?? false;
    [JsonIgnore] public string DefaultLabel => NpcFieldDefaults.SpecFor(Key)?.DefaultLabel ?? Label;
    [JsonIgnore] public string Hint => NpcFieldDefaults.SpecFor(Key)?.Hint ?? string.Empty;
}

public static class NpcFieldDefaults
{
    public const string NameKey = "name";
    public const string KinKey = "kin";
    public const string FactionKey = "faction";
    public const string DescriptionKey = "description";
    public const string LanguagesKey = "languages";
    public const string LocationKey = "location";
    public const string ProfessionKey = "profession";
    public const string AccentKey = "accent";
    public const string ImageKey = "image";
    public const string StatBlockKey = "statblock";

    public static readonly IReadOnlyList<NpcFieldSpec> Specs = new[]
    {
        new NpcFieldSpec(NameKey, "NPC Name", NpcFieldKind.SingleLineText, false, "Single-line text"),
        new NpcFieldSpec(KinKey, "Kin", NpcFieldKind.KinChoice, true, "Pick one from the loaded kins. The name can change, the choices always come from kins."),
        new NpcFieldSpec(FactionKey, "Faction", NpcFieldKind.FactionChoice, false, "Pick any of this campaign's factions"),
        new NpcFieldSpec(DescriptionKey, "Generic Description", NpcFieldKind.MultiLineText, false, "Multi-line text, for a backstory or appearance"),
        new NpcFieldSpec(LanguagesKey, "Language(s)", NpcFieldKind.LanguageChoice, false, "Pick any of the loaded languages"),
        new NpcFieldSpec(LocationKey, "Location", NpcFieldKind.SingleLineText, false, "Single-line text"),
        new NpcFieldSpec(ProfessionKey, "Profession", NpcFieldKind.SingleLineText, true, "Single-line text"),
        new NpcFieldSpec(AccentKey, "Accent", NpcFieldKind.SingleLineText, true, "Single-line text"),
        new NpcFieldSpec(ImageKey, "Image", NpcFieldKind.Image, false, "An image file that represents the NPC"),
        new NpcFieldSpec(StatBlockKey, "Stat Block", NpcFieldKind.StatBlock, false, "HP, Vigor, Agility, Mind and Spirit"),
    };

    public static NpcFieldSpec? SpecFor(string? key) => Specs.FirstOrDefault(s => s.Key == key);

    public static List<NpcFieldDefinition> Create()
        => Specs.Select(s => new NpcFieldDefinition { Key = s.Key, Label = s.DefaultLabel }).ToList();

    // Rebuilds the field list from the rules above. A saved label is kept only for a field the GM may rename;
    // anything else (older campaigns had customizable fields) goes back to its fixed name.
    public static List<NpcFieldDefinition> Normalize(IEnumerable<NpcFieldDefinition>? saved)
    {
        var byKey = (saved ?? Enumerable.Empty<NpcFieldDefinition>())
            .Where(f => f != null)
            .GroupBy(f => f.Key ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new List<NpcFieldDefinition>();
        foreach (var spec in Specs)
        {
            string label = spec.DefaultLabel;
            if (spec.CanRename && byKey.TryGetValue(spec.Key, out var existing) && !string.IsNullOrWhiteSpace(existing.Label))
                label = existing.Label.Trim();

            result.Add(new NpcFieldDefinition { Key = spec.Key, Label = label });
        }
        return result;
    }

    public static string? Validate(IReadOnlyList<NpcFieldDefinition> fields)
    {
        if (Specs.Any(s => fields.All(f => f.Key != s.Key))) return "The NPC field list is incomplete.";
        if (fields.Any(f => string.IsNullOrWhiteSpace(f.Label))) return "Every NPC field needs a label.";
        if (fields.Select(f => f.Label.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != fields.Count)
            return "Two NPC fields share a label.";
        return null;
    }
}