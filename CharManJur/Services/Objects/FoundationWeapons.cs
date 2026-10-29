using System.Collections.Generic;
using CharManJur.Models;

namespace CharManJur.Services;

// The foundation weapons (Ids 1000-1206), kept apart from ItemDataService so each one is a single line.
public static class FoundationWeapons
{
    public static List<Item> All() => new()
    {
        // ----- Piercing -----
        Melee(1000, "Concealed Dagger", "Small dagger, can be hidden in boots or other...orifices...(at your own peril).", 1, ItemSize.Regular, WeaponDamageType.Piercing, WeaponDamageDie.D6, WeaponSpeedType.Fast),
        Melee(1001, "Pitchfork", "Tool of a workin' man, yuuurt.", 1, ItemSize.Regular, WeaponDamageType.Piercing, WeaponDamageDie.D6, WeaponSpeedType.Balanced),
        Melee(1002, "Dagger", "Traditional little stabbing device.", 1, ItemSize.Regular, WeaponDamageType.Piercing, WeaponDamageDie.D6, WeaponSpeedType.Fast),
        Melee(1003, "Ceremonial Dagger", "Fancy little stabbing device.", 2, ItemSize.Regular, WeaponDamageType.Piercing, WeaponDamageDie.D6, WeaponSpeedType.Fast, WeaponEffectType.BleedV),
        Melee(1004, "Parrying Dagger", "Does it work on nukes?", 2, ItemSize.Regular, WeaponDamageType.Piercing, WeaponDamageDie.D6, WeaponSpeedType.Fast, WeaponEffectType.CounterI),
        Melee(1005, "Rapier", "En guarde!", 2, ItemSize.Regular, WeaponDamageType.Piercing, WeaponDamageDie.D8, WeaponSpeedType.Balanced, WeaponEffectType.CounterII),
        Melee(1006, "Spear", "Long pole with a pokey bit at the end of it.", 1, ItemSize.Regular, WeaponDamageType.Piercing, WeaponDamageDie.D8, WeaponSpeedType.Balanced, WeaponEffectType.PushVI, WeaponEffectType.Reach),
        Melee(1007, "War Pick", "A pickaxe of war.", 2, ItemSize.Regular, WeaponDamageType.Piercing, WeaponDamageDie.D8, WeaponSpeedType.Balanced, WeaponEffectType.BrutalVI),
        Melee(1008, "Boar Hunting Spear", "A spear of boar hunting.", 3, ItemSize.Bulky, WeaponDamageType.Piercing, WeaponDamageDie.D10, WeaponSpeedType.Slow, WeaponEffectType.PushVII, WeaponEffectType.Reach),

        // ----- Bludgeoning -----
        Melee(1009, "Cudgel", "A crude smashy smasher.", 1, ItemSize.Regular, WeaponDamageType.Bludgeoning, WeaponDamageDie.D6, WeaponSpeedType.Fast),
        Melee(1010, "Simple Staff", "Not of the magical variety.", 1, ItemSize.Regular, WeaponDamageType.Bludgeoning, WeaponDamageDie.D6, WeaponSpeedType.Fast, WeaponEffectType.Reach),
        Melee(1011, "Sage's Staff", "A wise, long, shaft.", 2, ItemSize.Regular, WeaponDamageType.Bludgeoning, WeaponDamageDie.D6, WeaponSpeedType.Fast, WeaponEffectType.Reach, WeaponEffectType.ShockVI),
        Melee(1012, "Flail", "For thwackin'.", 3, ItemSize.Regular, WeaponDamageType.Bludgeoning, WeaponDamageDie.D8, WeaponSpeedType.Balanced, WeaponEffectType.ShockV),
        Melee(1013, "Mace", "For smashin'.", 2, ItemSize.Regular, WeaponDamageType.Bludgeoning, WeaponDamageDie.D8, WeaponSpeedType.Balanced, WeaponEffectType.ShockVIII),
        Melee(1014, "Spiked Mace", "For smashin', but spikey..", 3, ItemSize.Regular, WeaponDamageType.Bludgeoning, WeaponDamageDie.D8, WeaponSpeedType.Balanced, WeaponEffectType.ShockVI),
        Melee(1015, "Maul", "For big smashin'.", 3, ItemSize.Bulky, WeaponDamageType.Bludgeoning, WeaponDamageDie.D10, WeaponSpeedType.Slow, WeaponEffectType.ShockVI),
        Melee(1016, "Warhammer", "Hammerin' for war.", 2, ItemSize.Bulky, WeaponDamageType.Bludgeoning, WeaponDamageDie.D10, WeaponSpeedType.Slow, WeaponEffectType.ShockVII),

        // ----- Slashing -----
        Melee(1017, "Shortsword", "A simple sword, and its short.", 1, ItemSize.Regular, WeaponDamageType.Slashing, WeaponDamageDie.D6, WeaponSpeedType.Fast),
        Melee(1018, "Axe", "For choppin' wood... or limbs.", 1, ItemSize.Regular, WeaponDamageType.Slashing, WeaponDamageDie.D8, WeaponSpeedType.Balanced, WeaponEffectType.HeavyI),
        Melee(1019, "Longsword", "A simple sword, and its long.", 2, ItemSize.Regular, WeaponDamageType.Slashing, WeaponDamageDie.D8, WeaponSpeedType.Balanced, WeaponEffectType.SweepVII),
        Melee(1020, "Scimitar", "A long curved blade, for slicing, and maybe some dicing.", 2, ItemSize.Regular, WeaponDamageType.Slashing, WeaponDamageDie.D8, WeaponSpeedType.Balanced, WeaponEffectType.SweepVI),
        Melee(1021, "Executioner's Axe", "An axe for executing.", 3, ItemSize.Regular, WeaponDamageType.Slashing, WeaponDamageDie.D8, WeaponSpeedType.Balanced, WeaponEffectType.BrutalV),
        Melee(1022, "Flamberge", "A big squiggly sword.", 3, ItemSize.Regular, WeaponDamageType.Slashing, WeaponDamageDie.D8, WeaponSpeedType.Balanced, WeaponEffectType.BleedVI),
        Melee(1023, "Halberd", "A long ass axe.", 2, ItemSize.Bulky, WeaponDamageType.Slashing, WeaponDamageDie.D8, WeaponSpeedType.Balanced, WeaponEffectType.SweepVIII),
        Melee(1024, "Greataxe", "A great ass axe.", 2, ItemSize.Bulky, WeaponDamageType.Slashing, WeaponDamageDie.D10, WeaponSpeedType.Slow, WeaponEffectType.HeavyII),
        Melee(1025, "Greatsword", "A great ass sword.", 2, ItemSize.Bulky, WeaponDamageType.Slashing, WeaponDamageDie.D10, WeaponSpeedType.Slow, WeaponEffectType.SweepVI),

        // ----- Thrown -----
        Thrown(1100, "Smoker", "Light explosive releasing a vision obscuring cloud in a 3x3 range."),
        Thrown(1101, "Sharper", "Moderate explosive releasing shrapnel in a wide range."),

        // ----- Ranged (always Piercing) -----
        Ranged(1200, "Bow", "A simple bow.", 1, ItemSize.Bulky, WeaponDamageDie.D6),
        Ranged(1201, "Breech Loading Pistol", "A simple firearm.", 2, ItemSize.Regular, WeaponDamageDie.D6, WeaponEffectType.HeavyI, WeaponEffectType.Reload, WeaponEffectType.RangeNear),
        Ranged(1202, "Crossbow", "Not Karil's.", 2, ItemSize.Bulky, WeaponDamageDie.D8),
        Ranged(1203, "Modular Crossbow", "A convenient crossbow.", 3, ItemSize.Regular, WeaponDamageDie.D8, WeaponEffectType.Modular),
        Ranged(1204, "Blunderbus", "For bussin' blunderously.", 3, ItemSize.Regular, WeaponDamageDie.D8, WeaponEffectType.HeavyII, WeaponEffectType.Reload, WeaponEffectType.RangeNear, WeaponEffectType.Blast),
        Ranged(1205, "Greatbow", "A pretty great bow.", 3, ItemSize.Bulky, WeaponDamageDie.D10),
        Ranged(1206, "Breech Loading Rifle", "Not a Win(ton)chester.", 3, ItemSize.Bulky, WeaponDamageDie.D10, WeaponEffectType.HeavyII, WeaponEffectType.Reload),
    };

    private static Item Melee(int id, string name, string description, int rarity, ItemSize size,
        WeaponDamageType damageType, WeaponDamageDie die, WeaponSpeedType speed, params WeaponEffectType[] effects)
        => Make(id, name, description, rarity, size, WeaponType.Melee, damageType, die, speed, effects);

    private static Item Ranged(int id, string name, string description, int rarity, ItemSize size,
        WeaponDamageDie die, params WeaponEffectType[] effects)
        => Make(id, name, description, rarity, size, WeaponType.Ranged, WeaponDamageType.Piercing, die, WeaponSpeedType.Fast, effects);

    private static Item Make(int id, string name, string description, int rarity, ItemSize size,
        WeaponType kind, WeaponDamageType damageType, WeaponDamageDie die, WeaponSpeedType speed, WeaponEffectType[] effects)
        => new Item
        {
            Id = id,
            Name = name,
            Category = ItemCategory.Weapon,
            BaseDescription = description,
            Rarity = rarity,
            Size = size,
            QtyLimit = 1,
            Uses = null,
            WeaponKind = kind,
            DamageType = damageType,
            WeaponDamage = die,
            WeaponSpeed = speed,
            WeaponEffects = effects.Length == 0 ? null : new List<WeaponEffectType>(effects),
            IsPlayerCreated = false,
            IsStackable = false
        };

    private static Item Thrown(int id, string name, string description)
        => new Item
        {
            Id = id,
            Name = name,
            Category = ItemCategory.Weapon,
            BaseDescription = description,
            Rarity = 1,
            Size = ItemSize.Petty,
            QtyLimit = 3,
            Uses = null,
            WeaponKind = WeaponType.Thrown,
            DamageType = WeaponDamageType.Piercing,
            WeaponDamage = null,
            WeaponEffects = null,
            WeaponSpeed = null,
            IsPlayerCreated = false,
            IsStackable = true
        };
}