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
    public int? ContainerSlots { get; set; }
    // Usage die (ammo-style)
    public bool HasUsageDie { get; set; }
    public bool HybridUsage { get; set; }
    public int? UsageDieSides { get; set; }

    // === WEAPON-SPECIFIC ===
    public WeaponType? WeaponKind { get; set; }
    public WeaponDamageType? DamageType { get; set; }
    public WeaponDamageDie? WeaponDamage { get; set; }
    public WeaponSpeedType? WeaponSpeed { get; set; }
    public List<WeaponEffectType> WeaponEffects { get; set; } = new();

    // === ARMOR-SPECIFIC ===
    public ArmorType? ArmorType { get; set; }
    public int? ArmorValue { get; set; }

    // === SOURCE ===
    public bool IsPlayerCreated { get; set; } = true;
}