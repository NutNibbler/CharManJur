using System.Text.Json;
using CharManJur.Models;

namespace CharManJur.Helpers;

public static class ItemEditHelpers
{
    // Independent copy of an item definition (same Id and Guid), used to preview an edit
    public static Item Clone(Item source) =>
        JsonSerializer.Deserialize<Item>(JsonSerializer.Serialize(source))!;

    // Names of the fields that differ, for the event log and the GM's approval card
    public static List<string> Describe(Item before, Item after)
    {
        var changes = new List<string>();

        void Check<T>(string label, T a, T b)
        {
            if (!EqualityComparer<T>.Default.Equals(a, b)) changes.Add(label);
        }

        Check("Name", before.Name, after.Name);
        Check("Description", before.BaseDescription, after.BaseDescription);
        Check("Category", before.Category, after.Category);
        Check("Size", before.Size, after.Size);
        Check("Value", before.ValueInChips, after.ValueInChips);
        Check("Rarity", before.Rarity, after.Rarity);
        Check("Qty Limit", before.QtyLimit, after.QtyLimit);
        Check("Stackable", before.IsStackable, after.IsStackable);
        Check("Uses", before.Uses, after.Uses);
        Check("Weapon Type", before.WeaponKind, after.WeaponKind);
        Check("Damage Type", before.DamageType, after.DamageType);
        Check("Damage Die", before.WeaponDamage, after.WeaponDamage);
        Check("Weapon Speed", before.WeaponSpeed, after.WeaponSpeed);
        Check("Armor Type", before.ArmorType, after.ArmorType);
        Check("Armor Value", before.ArmorValue, after.ArmorValue);
        Check("Container Slots", before.ContainerSlots, after.ContainerSlots);
        Check("Usage Die", before.HasUsageDie, after.HasUsageDie);
        Check("Starting Die", before.DefaultUsageDieSides, after.DefaultUsageDieSides);
        Check("Hybrid Usage", before.HybridUsage, after.HybridUsage);

        var a = (before.WeaponEffects ?? new()).OrderBy(e => e).ToList();
        var b = (after.WeaponEffects ?? new()).OrderBy(e => e).ToList();
        if (!a.SequenceEqual(b)) changes.Add("Weapon Effects");

        return changes;
    }
}