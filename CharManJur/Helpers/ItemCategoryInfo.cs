using CharManJur.Models;

namespace CharManJur.Helpers;

// One plain-language description per item category. Used by the custom item creator now,
// and meant for the GM's shop setup screen later so they know what they are switching on.
public static class ItemCategoryInfo
{
    public static string Describe(ItemCategory category) => category switch
    {
        ItemCategory.Weapon => "Used to attack. Melee, Thrown or Ranged, with a damage die, a speed and optional skills. Equips to the hands.",
        ItemCategory.Armor => "Worn on the body for an armor value. The armor type sets how heavy it is. Shields also qualify here.",
        ItemCategory.Food => "Eaten or drunk to satisfy daily needs. Often has a limited number of uses.",
        ItemCategory.Essential => "Supplies a character is expected to carry to get by day to day.",
        ItemCategory.ToolKit => "Tools and kits for a trade, repair or craft.",
        ItemCategory.AdventuringGear => "General equipment for travel and exploring: rope, torches, bedrolls and the like.",
        ItemCategory.Resource => "Raw materials and components used for crafting.",
        ItemCategory.Currency => "Money: Marks, Rupees and written Notes. Counted in the currency display.",
        ItemCategory.Instrument => "Musical instruments.",
        ItemCategory.Miscellaneous => "Anything that doesn't fit another category.",
        ItemCategory.Jewelry => "Worn items such as rings and amulets. Worn with no slot and no limit. May have magical effects or enchantments.",
        ItemCategory.Trinket => "Charms and curios carried or worn. Behaves like Jewelry: no slot and no limit. May have magical effects or enchantments.",
        ItemCategory.Story => "Quest and plot items handed out by the GM or during character creation, these items are not seen in shops.",
        ItemCategory.Container => "Provides extra inventory slots to a familiar while equipped.",
        ItemCategory.Ammo => "Ammunition for weapons requiring it. Tends to have a Usage die.",
        ItemCategory.Unknown => "Unidentified items.",
        _ => string.Empty
    };

    public static string Describe(ItemCategory? category) =>
        category.HasValue ? Describe(category.Value) : string.Empty;
}