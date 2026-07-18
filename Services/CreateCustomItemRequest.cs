using CharManJur.Models;
using System.Collections.Generic;

namespace CharManJur.Services;

public class CreateCustomItemRequest
{
    // === REQUIRED ===
    public string Name { get; set; } = string.Empty;

    // === OPTIONAL ===
    public ItemCategory? Category { get; set; }
    public string? BaseDescription { get; set; }
    public ItemSize Size { get; set; } = ItemSize.Regular;
    public int? ValueInChips { get; set; }
    public int? Rarity { get; set; }
    public int? Uses { get; set; }
    public int? QtyLimit { get; set; }
    public bool IsStackable { get; set; } = true;

    // === WEAPON-SPECIFIC ===
    public WeaponCategoryType? WeaponCategory { get; set; }
    public WeaponDamageDie? WeaponDamage { get; set; }
    public WeaponSpeedType? WeaponSpeed { get; set; }
    public List<WeaponEffectType> WeaponEffects { get; set; } = new();

    // === ARMOR-SPECIFIC ===
    public ArmorType? ArmorType { get; set; }
    public int? ArmorValue { get; set; }

    // === SOURCE ===
    public bool IsPlayerCreated { get; set; } = true;
}