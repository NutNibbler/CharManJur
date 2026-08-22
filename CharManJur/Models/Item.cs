using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Runtime.CompilerServices;

namespace CharManJur.Models;

public class Item : INotifyPropertyChanged
{
    // === CUSTOM ITEM ID CONSTANTS ===
    public const int CUSTOM_ITEM_BASE = 9900000;  // Base for custom item IDs
    public const int CUSTOM_ITEM_START = 9900001; // First custom item ID

    // === CUSTOM ITEM ATTRIBUTES ===
    public Guid Guid { get; set; } = Guid.Empty;  // permanent identity — never changes, this is what actually gets referenced

    private bool _isLoaded = true;
    public bool IsLoaded
    {
        get => _isLoaded;
        set
        {
            if (_isLoaded != value)
            {
                _isLoaded = value;
                OnPropertyChanged();
            }
        }
    }

    public string SourcePackId { get; set; } = "Local";
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // === GENERAL ITEM ATTRIBUTES ===
    public int Id { get; set; }
    public int? Uses { get; set; } = null;
    public int? ValueInChips { get; set; }
    public int? Rarity { get; set; }
    public int? QtyLimit { get; set; }

    public ItemCategory? Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public ItemSize Size { get; set; } = ItemSize.Regular;
    public string? BaseDescription { get; set; } = string.Empty;
    public string? PlayerNote { get; set; } = string.Empty;
    public WeaponCategoryType? WeaponCategory { get; set; }
    public WeaponDamageDie? WeaponDamage { get; set; }
    public List<WeaponEffectType>? WeaponEffects { get; set; }
    public WeaponSpeedType? WeaponSpeed { get; set; }

    public int? ArmorValue { get; set; }
    public ArmorType? ArmorType { get; set; }

    public bool IsPlayerCreated { get; set; } = false;
    public bool? IsStackable { get; set; } = false;
    public bool IsDiscovered { get; set; } = true;

    // ===== HELPER PROPERTIES =====
    public bool IsJewelry => Category == ItemCategory.Jewelry;
    public bool IsArmor => Category == ItemCategory.Armor;
    public bool IsShield => Category == ItemCategory.Armor && ArmorType == CharManJur.Models.ArmorType.Shield;
    public bool IsWeapon => Category == ItemCategory.Weapon;
    public bool IsShieldOrWeapon => IsShield || IsWeapon;
    public bool IsStackableItem => IsStackable.HasValue && IsStackable.Value;
    public bool HasUses => Uses.HasValue && Uses.Value > 0;
    public bool IsUnlimited => !Uses.HasValue || Uses.Value == -1;

    public int SlotsRequired => Size == ItemSize.Bulky ? 2 : 1;

    // ===== NEW: Per-Instance Metadata =====
    public int CurrentUses { get; set; } = 0;
    public bool IsEmpty { get; set; } = false;
    public string? CustomName { get; set; }
    public Guid InstanceId { get; set; } = Guid.NewGuid();
    public int? EquippedSlot { get; set; }
    public bool IsEquipped { get; set; } = false;

    // ===== SLOT CALCULATION METHODS =====
    public int CalculateSlotsUsed(int quantity)
    {
        if (quantity <= 0) return 0;

        switch (Size)
        {
            case ItemSize.Petty:
                // Petty items: 0 slots unless quantity >= QtyLimit
                if (QtyLimit.HasValue && quantity >= QtyLimit.Value)
                    return 1; // Move up to Regular size grade
                return 0;

            case ItemSize.Regular:
                // Regular items: 1 slot per QtyLimit threshold
                if (QtyLimit.HasValue && QtyLimit.Value > 0)
                    return (int)Math.Ceiling((double)quantity / QtyLimit.Value);
                return quantity; // If no QtyLimit, each item takes 1 slot

            case ItemSize.Bulky:
                // Bulky items: 2 slots natively
                return 2;

            default:
                return quantity;
        }
    }

    public bool IsEncumbering(int quantity)
    {
        if (quantity <= 0) return false;

        // Bulky items encumber if they surpass QtyLimit
        if (Size == ItemSize.Bulky && QtyLimit.HasValue)
            return quantity > QtyLimit.Value;

        return false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum ItemCategory
{
    Weapon,
    Armor,
    Food,
    Essential,
    ToolKit,
    AdventuringGear,
    Resource,
    Currency,
    Instrument,
    Miscellaneous,
    Jewelry,
    Unknown,
    Story
}

public enum ItemSize
{
    Petty,      // 0 slots unless QtyLimit reached
    Regular,    // 1 slot per QtyLimit threshold
    Bulky       // 2 slots natively, encumbered if QtyLimit surpassed
}

public enum WeaponCategoryType
{
    [Display(Name = "Ranged", Description = "Weapons that are designed to harm from afar, they launch projectiles.")]
    Ranged,
    [Display(Name = "Piercing", Description = "Rolling the maximum damage on your damage roll will ignore armor. The attack must be the original weapon die for the weapon.")]
    Piercing,
    [Display(Name = "Bludgeoning", Description = "Always ignores 1 armor. ")]
    Bludgeoning,
    [Display(Name = "Slashing", Description = "Deals +1 damage against opponents with 0 armor. ")]
    Slashing,
    [Display(Name = "Thrown", Description = "Weapons that can be thrown to deal damage or set an effect.")]
    Thrown,
}

public enum WeaponDamageDie
{
    [Display(Name = "D4")]
    D4,
    [Display(Name = "D6")]
    D6,
    [Display(Name = "D8")]
    D8,
    [Display(Name = "D10")]
    D10,
    [Display(Name = "D12")]
    D12,
    [Display(Name = "D20")]
    D20,
}

public enum WeaponSpeedType
{
    [Display(Name = "Slow", Description = "Slow weapons pack a punch but aren't quick enough for dodging. They share the D10 damage die. ")]
    Slow,

    [Display(Name = "Balanced", Description = "Balanced weapons are both light enough for dodging and heavy enough for blocking. They share a D8 damage die.")]
    Balanced,

    [Display(Name = "Fast", Description = "Fast weapons strike quickly for lower damage using the D6 damage die. These weapons cannot be used to block.")]
    Fast
}

public enum WeaponEffectType
{
    [Display(Name = "Range-Near", Description = "Can only effectively hit targets within NEAR range.")]
    RangeNear,
    [Display(Name = "Blast", Description = "PLACE-HOLDER DESC")]
    Blast,
    [Display(Name = "Bleed V", Description = "Damage rolls of X or higher trigger an additional 1d4 VIG damage at the start of their next turn. This does not trigger a critical damage save. ")]
    BleedV,
    [Display(Name = "Bleed VI", Description = "Damage rolls of X or higher trigger an additional 1d4 VIG damage at the start of their next turn. This does not trigger a critical damage save. ")]
    BleedVI,
    [Display(Name = "Brutal V", Description = "Critical Damage resulting from a damage roll of X or more results in an instant kill and forces witnesses to make a morale save. (morale saves are made by NPCs and Opponents during combat)")]
    BrutalV,
    [Display(Name = "Brutal VI", Description = "Critical Damage resulting from a damage roll of X or more results in an instant kill and forces witnesses to make a morale save. (morale saves are made by NPCs and Opponents during combat)")]
    BrutalVI,
    [Display(Name = "Brutal VII", Description = "Critical Damage resulting from a damage roll of X or more results in an instant kill and forces witnesses to make a morale save. (morale saves are made by NPCs and Opponents during combat)")]
    BrutalVII,
    [Display(Name = "Counter I", Description = "Damage rolls of X or less against you are bounced back against your attacker. ")]
    CounterI,
    [Display(Name = "Counter II", Description = "Damage rolls of X or less against you are bounced back against your attacker. ")]
    CounterII,
    [Display(Name = "Counter III", Description = "Damage rolls of X or less against you are bounced back against your attacker. ")]
    CounterIII,
    [Display(Name = "Heavy I", Description = "Ignore X points of armor but damage rolls of X or lower don't apply.")]
    HeavyI,
    [Display(Name = "Heavy II", Description = "Ignore X points of armor but damage rolls of X or lower don't apply.")]
    HeavyII,
    [Display(Name = "Heavy III", Description = "Ignore X points of armor but damage rolls of X or lower don't apply.")]
    HeavyIII,
    [Display(Name = "Modular", Description = "Weapons can quickly be deconstructed for concealabilty. ")]
    Modular,
    [Display(Name = "Push VI", Description = "Damage rolls of X or higher push the target back from close to near, preventing approach and allowing longer weapons to keep their advantage. ")]
    PushVI,
    [Display(Name = "Push VII", Description = "Damage rolls of X or higher push the target back from close to near, preventing approach and allowing longer weapons to keep their advantage. ")]
    PushVII,
    [Display(Name = "Push VIII", Description = "Damage rolls of X or higher push the target back from close to near, preventing approach and allowing longer weapons to keep their advantage. ")]
    PushVIII,
    [Display(Name = "Reload", Description = "Takes 1 full turn to reload.")]
    Reload,
    [Display(Name = "Shock V", Description = "Damage rolls of X or higher trigger a VIG Save. Target loses their next turn if they fail. ")]
    ShockV,
    [Display(Name = "Shock VI", Description = "Damage rolls of X or higher trigger a VIG Save. Target loses their next turn if they fail. ")]
    ShockVI,
    [Display(Name = "Shock VII", Description = "Damage rolls of X or higher trigger a VIG Save. Target loses their next turn if they fail. ")]
    ShockVII,
    [Display(Name = "Shock VIII", Description = "Damage rolls of X or higher trigger a VIG Save. Target loses their next turn if they fail. ")]
    ShockVIII,
    [Display(Name = "Sweep I", Description = "Damage rolls of X or higher grant 1 bonus attack on another target within reach. This effect doesn't chain with other abilities. ")]
    SweepI,
    [Display(Name = "Sweep II", Description = "Damage rolls of X or higher grant 1 bonus attack on another target within reach. This effect doesn't chain with other abilities. ")]
    SweepII,
    [Display(Name = "Sweep III", Description = "Damage rolls of X or higher grant 1 bonus attack on another target within reach. This effect doesn't chain with other abilities. ")]
    SweepIII,
    [Display(Name = "Sweep IV", Description = "Damage rolls of X or higher grant 1 bonus attack on another target within reach. This effect doesn't chain with other abilities. ")]
    SweepIV,
    [Display(Name = "Sweep V", Description = "Damage rolls of X or higher grant 1 bonus attack on another target within reach. This effect doesn't chain with other abilities. ")]
    SweepV,
    [Display(Name = "Sweep VI", Description = "Damage rolls of X or higher grant 1 bonus attack on another target within reach. This effect doesn't chain with other abilities. ")]
    SweepVI,
    [Display(Name = "Sweep VII", Description = "Damage rolls of X or higher grant 1 bonus attack on another target within reach. This effect doesn't chain with other abilities. ")]
    SweepVII,
    [Display(Name = "Sweep VIII", Description = "Damage rolls of X or higher grant 1 bonus attack on another target within reach. This effect doesn't chain with other abilities. ")]
    SweepVIII,
    [Display(Name = "Reach", Description = "Can hit near targets.")]
    Reach
}

public enum ArmorType
{
    [Display(Name = "Light", Description = "Lightweight and nimble armor, typicaly a gambeson or made of leather. Doesn't hinder movement at all.")]
    Light,
    [Display(Name = "Medium", Description = "Typically made of chainmail, this armor can hinder your ability to swim.")]
    Medium,
    [Display(Name = "Heavy", Description = "Metal Plate armor, very cumbersome and will make all agile movement more difficult, including stealth.")]
    Heavy,
    [Display(Name = "Shield", Description = "Provides more armor, can be quickly released unlike worn armor(Armor cap is still 3).")]
    Shield
}