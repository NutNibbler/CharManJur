using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

// One weapon in the Warrior's weapon picker.
public class WeaponChoice
{
    public Item Item { get; init; } = null!;
    public string Name => Item.Name;
    public string Summary { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string SkillsText { get; init; } = string.Empty;
    public bool HasSkills { get; init; }
    public string SearchText { get; init; } = string.Empty;
}

public static class WeaponChoices
{
    // Every loaded weapon that hasn't been picked already (no duplicate picks). Weapons with skill effects come
    // first, since the pick only matters for them.
    public static async Task<List<WeaponChoice>> LoadAsync(IItemDataService items, IReadOnlyList<WeaponKillRecord> alreadyPicked)
    {
        var all = await items.GetAllItemsAsync();

        return all
            .Where(i => i.IsWeapon && !alreadyPicked.Any(r => r.IsFor(i)))
            .Select(Build)
            .OrderByDescending(c => c.HasSkills)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static WeaponChoice Build(Item item)
    {
        var effects = item.WeaponEffects ?? new List<WeaponEffectType>();
        var skills = effects.Where(WeaponProficiencyTracker.NeedsWeaponSkill).Select(e => EnumDisplayHelper.GetName(e)).ToList();
        var others = effects.Where(e => !WeaponProficiencyTracker.NeedsWeaponSkill(e)).Select(e => EnumDisplayHelper.GetName(e)).ToList();

        var parts = new List<string>();
        if (item.WeaponKind.HasValue) parts.Add(EnumDisplayHelper.GetName(item.WeaponKind.Value));
        if (item.DamageType.HasValue) parts.Add(EnumDisplayHelper.GetName(item.DamageType.Value));
        if (item.WeaponDamage.HasValue) parts.Add(EnumDisplayHelper.GetName(item.WeaponDamage.Value));
        if (item.WeaponSpeed.HasValue) parts.Add(EnumDisplayHelper.GetName(item.WeaponSpeed.Value));
        parts.AddRange(others);

        string summary = string.Join(" · ", parts);
        string skillsText = skills.Count > 0 ? "⭐ Weapon skills: " + string.Join(", ", skills) : "No weapon skills to unlock";

        return new WeaponChoice
        {
            Item = item,
            Summary = summary,
            Description = item.BaseDescription ?? string.Empty,
            SkillsText = skillsText,
            HasSkills = skills.Count > 0,
            SearchText = $"{item.Name} {summary} {skillsText}"
        };
    }
}