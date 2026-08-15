using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CharManJur.Services;

public class ItemDataService : IItemDataService
{
    // === MASTER LIST - Contains BOTH foundation AND custom items ===
    private readonly List<Item> _items = new();

    // === INJECTED STORAGE SERVICE ===
    private readonly ICustomItemStorageService _customStorageService;

    // === CONSTRUCTOR ===
    public ItemDataService(ICustomItemStorageService customStorageService)
    {
        _customStorageService = customStorageService;

        // Load foundation items into the master list
        LoadFoundationItems();

        // Load custom items from JSON and add to the master list
        Task.Run(LoadCustomItemsFromStorage).Wait();
    }

    // ============================================================
    // LOAD FOUNDATION ITEMS (Hard-coded)
    // ============================================================
    private void LoadFoundationItems()
    {
        _items.AddRange(new[]
        {
            new Item
            {
                Id = 1,
                Name = "Chip",
                Category = ItemCategory.Currency,
                BaseDescription = "Round, stamped copper.",
                ValueInChips = 1,
                Size = ItemSize.Petty,
                QtyLimit = 15000,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 2,
                Name = "Rupee",
                Category = ItemCategory.Currency,
                BaseDescription = "Triangular, stamped silver, worth five chips.",
                ValueInChips = 5,
                Size = ItemSize.Petty,
                QtyLimit = 3000,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 3,
                Name = "Mark",
                Category = ItemCategory.Currency,
                BaseDescription = "Serialized, Stamped Paper.",
                ValueInChips = 60,
                Size = ItemSize.Petty,
                QtyLimit = 250,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 4,
                Name = "Note",
                Category = ItemCategory.Currency,
                BaseDescription = "Written and notarized blue-backed paper with varying value. Must value at least 10 Marks.",
                ValueInChips = 600,
                Size = ItemSize.Petty,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 100,
                Name = "Thieving Kit",
                Category = ItemCategory.ToolKit,
                BaseDescription = "Lock picks, glass cutters, screwdriver",
                ValueInChips = 50,
                Uses = 3,
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 101,
                Name = "Fishing Kit",
                Category = ItemCategory.ToolKit,
                BaseDescription = "Pole, bait, line",
                ValueInChips = 40,
                Uses = 3,
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 102,
                Name = "Scrivener's Kit",
                Category = ItemCategory.ToolKit,
                BaseDescription = "Pole, bait, line",
                ValueInChips = 40,
                Uses = 3,
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 103,
                Name = "Disguise Kit",
                Category = ItemCategory.ToolKit,
                BaseDescription = "Wigs, fake nose, makeup, fancy and poor set of clothes",
                ValueInChips = 30,
                Uses = 3,
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 104,
                Name = "Healing Supplies",
                Category = ItemCategory.ToolKit,
                BaseDescription = "Bandages, scalpels, alcohol, needle & thread",
                ValueInChips = 30,
                Uses = 1,
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 110,
                Name = "Air Bladder",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Fill it with air, size of a pig stomach",
                ValueInChips = 10,
                Uses = 3,
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 111,
                Name = "Manacles",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Where were you on Jan. 6th?",
                ValueInChips = 10,
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 112,
                Name = "12' Net",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "How else are you gonna catch a partner?",
                ValueInChips = 20,
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 113,
                Name = "Ration",
                Category = ItemCategory.Essential,
                BaseDescription = "Non-perishable food",
                ValueInChips = 10,
                Uses = 2,
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 114,
                Name = "Antitoxin",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Cures most common toxins",
                ValueInChips = 30,
                Uses = 1,
                Size = ItemSize.Petty,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 115,
                Name = "50' Rope",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Weak man’s chain",
                ValueInChips = 15,
                Uses = 1,
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 116,
                Name = "Earplugs",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Hinders hearing, but prevents noise-related ear injuries.",
                Size = ItemSize.Petty,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 117,
                Name = "Shovel",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "For excavation and the occasional smack.",
                ValueInChips = 5,
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 118,
                Name = "Deck of Cards",
                Category = ItemCategory.Miscellaneous,
                BaseDescription = "A complete deck of playing cards.",
                ValueInChips = 5,
                QtyLimit = 3,
                Size = ItemSize.Petty,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 119,
                Name = "10' Pole",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Not only for poking",
                ValueInChips = 5,
                Size = ItemSize.Bulky,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 120,
                Name = "Grappling Hook",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Can be used to climb ledges",
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 121,
                Name = "Holy Book",
                Category = ItemCategory.Miscellaneous,
                BaseDescription = "Book of Holy",
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 122,
                Name = "Incense",
                Category = ItemCategory.Miscellaneous,
                BaseDescription = "Both for and not for pseudoscience",
                Size = ItemSize.Petty,
                Uses = 3,
                ValueInChips = 3,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 123,
                Name = "Bear Trap",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Does anyone actually use this for bears?",
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 124,
                Name = "Tarot Cards",
                Category = ItemCategory.Miscellaneous,
                BaseDescription = "Want to know your future?",
                Size = ItemSize.Petty,
                QtyLimit = 3,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 125,
                Name = "Fake Gems",
                Category = ItemCategory.Miscellaneous,
                BaseDescription = "Gems of an artificial variety, but could be valuable to a fool.",
                Size = ItemSize.Petty,
                QtyLimit = 5,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 126,
                Name = "Cusser",
                Category = ItemCategory.Miscellaneous,
                BaseDescription = "No idea what this is",
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 127,
                Name = "Magnifying Glass",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Boomer glasses",
                ValueInChips = 3,
                Size = ItemSize.Petty,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 128,
                Name = "Vial of Vym",
                Category = ItemCategory.Resource,
                BaseDescription = "Volatile raw magic, whatcha gonna do with it?",
                ValueInChips = 3,
                QtyLimit = 3,
                Size = ItemSize.Petty,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 129,
                Name = "Spyglass",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "How can you be a pirate without one?",
                ValueInChips = 15,
                Size = ItemSize.Petty,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 130,
                Name = "Poison",
                Category = ItemCategory.Resource,
                BaseDescription = "Can be applied to weapons.",
                Size = ItemSize.Petty,
                QtyLimit = 3,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 131,
                Name = "Flash Powder",
                Category = ItemCategory.Resource,
                BaseDescription = "Combustible powder, releases high energy on ingition.",
                Size = ItemSize.Petty,
                QtyLimit = 3,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 132,
                Name = "Witching Salt",
                Category = ItemCategory.Resource,
                BaseDescription = "Magic disrupting salts.",
                Size = ItemSize.Petty,
                QtyLimit = 3,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 133,
                Name = "10' Tarp",
                Category = ItemCategory.Essential,
                BaseDescription = "Waterproof, compact",
                Size = ItemSize.Regular,
                ValueInChips = 20,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 134,
                Name = "Waterskin",
                Category = ItemCategory.Essential,
                BaseDescription = "Low quality bladder",
                Size = ItemSize.Petty,
                Uses = 5,
                ValueInChips = 15,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 135,
                Name = "Hydroflask",
                Category = ItemCategory.Essential,
                BaseDescription = "Simply will not break",
                Size = ItemSize.Petty,
                Uses = 5,
                ValueInChips = 30,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 136,
                Name = "Bedroll",
                Category = ItemCategory.Essential,
                BaseDescription = "Necessary for ‘perilous sleep’ benefits",
                Size = ItemSize.Regular,
                ValueInChips = 20,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 137,
                Name = "Tent",
                Category = ItemCategory.Essential,
                BaseDescription = "Necessary for ‘well-made camp’ benefits",
                Size = ItemSize.Bulky,
                ValueInChips = 50,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 138,
                Name = "Torch",
                Category = ItemCategory.Essential,
                BaseDescription = "Cheap and disposable. range: near",
                Size = ItemSize.Regular,
                Uses = 2,
                ValueInChips = 5,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 139,
                Name = "Lighter",
                Category = ItemCategory.Essential,
                BaseDescription = "Small amount of oil goes a long way",
                Size = ItemSize.Regular,
                Uses = 20,
                ValueInChips = 5,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 140,
                Name = "Lantern",
                Category = ItemCategory.Essential,
                BaseDescription = "Lightsource requiring oil for refills. range: near",
                Size = ItemSize.Regular,
                Uses = 4,
                ValueInChips = 20,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 141,
                Name = "Oil Flask",
                Category = ItemCategory.Essential,
                BaseDescription = "Fuel for a lantern, 1 use fills a lantern",
                Size = ItemSize.Regular,
                Uses = 2,
                ValueInChips = 10,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 142,
                Name = "Brightrocks",
                Category = ItemCategory.Essential,
                BaseDescription = "Invested gemstones, smack to activate. range: near",
                Size = ItemSize.Regular,
                ValueInChips = 90,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 143,
                Name = "Air Bladder",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Fill it with air, size of a pig stomach",
                Size = ItemSize.Regular,
                Uses = 3,
                ValueInChips = 10,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 144,
                Name = "Jug",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Fill it with... whatever...",
                Size = ItemSize.Regular,
                Uses = 3,
                ValueInChips = 3,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 145,
                Name = "Glue",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Sticky.",
                Size = ItemSize.Regular,
                Uses = 1,
                ValueInChips = 1,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 146,
                Name = "Bottle",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Stores a small amount of liquid",
                Size = ItemSize.Petty,
                QtyLimit = 3,
                ValueInChips = 10,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 147,
                Name = "Crampons",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Set of two stabilizing footwear for snowy conditions",
                Size = ItemSize.Regular,
                Uses = 3,
                ValueInChips = 20,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 148,
                Name = "Metal File",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Probably shouldn’t use this for your nails",
                Size = ItemSize.Petty,
                Uses = 3,
                ValueInChips = 20,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 149,
                Name = "Ball Bearings",
                Category = ItemCategory.Resource,
                BaseDescription = "Set of 50… go crazy",
                Uses = 50,
                Size = ItemSize.Petty,
                ValueInChips = 20,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 150,
                Name = "Holy Water",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Blessings depend on the merchant (price may vary)",
                Uses = 2,
                Size = ItemSize.Regular,
                ValueInChips = 10,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 151,
                Name = "10' Ladder",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Go up or down",
                Size = ItemSize.Bulky,
                ValueInChips = 15,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 152,
                Name = "10' Ladder, Collapsable",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Go up or down, but collapsable",
                Size = ItemSize.Regular,
                ValueInChips = 75,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 153,
                Name = "10' Pole, Collapsable",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Not only for poking, but collapsable",
                Size = ItemSize.Regular,
                ValueInChips = 40,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 154,
                Name = "Padlock and Key",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Not only for chastity belts",
                Size = ItemSize.Petty,
                ValueInChips = 5,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 155,
                Name = "Grease",
                Category = ItemCategory.Resource,
                BaseDescription = "Think like a wizard",
                Size = ItemSize.Regular,
                Uses = 1,
                ValueInChips = 3,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 156,
                Name = "Chalk",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "For making plans or tracing bodies, other things too, probably",
                Size = ItemSize.Petty,
                Uses = 5,
                ValueInChips = 2,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 157,
                Name = "20' Chain",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "Hope you fucking lift",
                Size = ItemSize.Bulky,
                ValueInChips = 20,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 158,
                Name = "Whistle",
                Category = ItemCategory.AdventuringGear,
                BaseDescription = "The kind you blow",
                Size = ItemSize.Petty,
                ValueInChips = 2,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 500,
                Name = "Flute",
                Category = ItemCategory.Instrument,
                BaseDescription = "Musical tube",
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 501,
                Name = "Lute",
                Category = ItemCategory.Instrument,
                BaseDescription = "A classic",
                Size = ItemSize.Regular,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1000,
                Name = "Concealed Dagger",
                Category = ItemCategory.Weapon,
                BaseDescription = "Small dagger, can be hidden in boots or other...orifices...(at your own peril).",
                Rarity = 1,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Piercing,
                WeaponDamage = WeaponDamageDie.D6,
                WeaponEffects = null,
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1001,
                Name = "Pitchfork",
                Category = ItemCategory.Weapon,
                BaseDescription = "Tool of a workin' man, yuuurt.",
                Rarity = 1,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Piercing,
                WeaponDamage = WeaponDamageDie.D6,
                WeaponEffects = null,
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1002,
                Name = "Dagger",
                Category = ItemCategory.Weapon,
                BaseDescription = "Traditional little stabbing device.",
                Rarity = 1,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Piercing,
                WeaponDamage = WeaponDamageDie.D6,
                WeaponEffects = null,
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1003,
                Name = "Ceremonial Dagger",
                Category = ItemCategory.Weapon,
                BaseDescription = "Fancy little stabbing device.",
                Rarity = 2,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Piercing,
                WeaponDamage = WeaponDamageDie.D6,
                WeaponEffects = [WeaponEffectType.BleedV],
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1004,
                Name = "Parrying Dagger",
                Category = ItemCategory.Weapon,
                BaseDescription = "Does it work on nukes?",
                Rarity = 2,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Piercing,
                WeaponDamage = WeaponDamageDie.D6,
                WeaponEffects = [WeaponEffectType.CounterI],
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1005,
                Name = "Rapier",
                Category = ItemCategory.Weapon,
                BaseDescription = "En guarde!",
                Rarity = 2,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Piercing,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.CounterII],
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1006,
                Name = "Spear",
                Category = ItemCategory.Weapon,
                BaseDescription = "Long pole with a pokey bit at the end of it.",
                Rarity = 1,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Piercing,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.PushVI,WeaponEffectType.Reach],
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1007,
                Name = "War Pick",
                Category = ItemCategory.Weapon,
                BaseDescription = "A pickaxe of war.",
                Rarity = 2,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Piercing,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.BrutalVI],
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1008,
                Name = "Boar Hunting Spear",
                Category = ItemCategory.Weapon,
                BaseDescription = "A spear of boar hunting.",
                Rarity = 3,
                Size = ItemSize.Bulky,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Piercing,
                WeaponDamage = WeaponDamageDie.D10,
                WeaponEffects = [WeaponEffectType.PushVII,WeaponEffectType.Reach],
                WeaponSpeed = WeaponSpeedType.Slow,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1009,
                Name = "Cudgel",
                Category = ItemCategory.Weapon,
                BaseDescription = "A crude smashy smasher.",
                Rarity = 1,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Bludgeoning,
                WeaponDamage = WeaponDamageDie.D6,
                WeaponEffects = null,
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1010,
                Name = "Simple Staff",
                Category = ItemCategory.Weapon,
                BaseDescription = "Not of the magical variety.",
                Rarity = 1,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Bludgeoning,
                WeaponDamage = WeaponDamageDie.D6,
                WeaponEffects = [WeaponEffectType.Reach],
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1011,
                Name = "Sage's Staff",
                Category = ItemCategory.Weapon,
                BaseDescription = "A wise, long, shaft.",
                Rarity = 2,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Bludgeoning,
                WeaponDamage = WeaponDamageDie.D6,
                WeaponEffects = [WeaponEffectType.Reach,WeaponEffectType.ShockVI],
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1012,
                Name = "Flail",
                Category = ItemCategory.Weapon,
                BaseDescription = "For thwackin'.",
                Rarity = 3,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Bludgeoning,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.ShockV],
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1013,
                Name = "Mace",
                Category = ItemCategory.Weapon,
                BaseDescription = "For smashin'.",
                Rarity = 2,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Bludgeoning,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.ShockVIII],
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1014,
                Name = "Spiked Mace",
                Category = ItemCategory.Weapon,
                BaseDescription = "For smashin', but spikey..",
                Rarity = 3,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Bludgeoning,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.ShockVI],
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1015,
                Name = "Maul",
                Category = ItemCategory.Weapon,
                BaseDescription = "For big smashin'.",
                Rarity = 3,
                Size = ItemSize.Bulky,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Bludgeoning,
                WeaponDamage = WeaponDamageDie.D10,
                WeaponEffects = [WeaponEffectType.ShockVI],
                WeaponSpeed = WeaponSpeedType.Slow,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1016,
                Name = "Warhammer",
                Category = ItemCategory.Weapon,
                BaseDescription = "Hammerin' for war.",
                Rarity = 2,
                Size = ItemSize.Bulky,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Bludgeoning,
                WeaponDamage = WeaponDamageDie.D10,
                WeaponEffects = [WeaponEffectType.ShockVII],
                WeaponSpeed = WeaponSpeedType.Slow,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1017,
                Name = "Shortsword",
                Category = ItemCategory.Weapon,
                BaseDescription = "A simple sword, and its short.",
                Rarity = 1,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Slashing,
                WeaponDamage = WeaponDamageDie.D6,
                WeaponEffects = null,
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1017,
                Name = "Axe",
                Category = ItemCategory.Weapon,
                BaseDescription = "For choppin' wood... or limbs.",
                Rarity = 1,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Slashing,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.HeavyI],
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1018,
                Name = "Longsword",
                Category = ItemCategory.Weapon,
                BaseDescription = "A simple sword, and its long.",
                Rarity = 2,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Slashing,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.SweepVII],
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1019,
                Name = "Scimitar",
                Category = ItemCategory.Weapon,
                BaseDescription = "A long curved blade, for slicing, and maybe some dicing.",
                Rarity = 2,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Slashing,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.SweepVI],
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1020,
                Name = "Executioner's Axe",
                Category = ItemCategory.Weapon,
                BaseDescription = "An axe for executing.",
                Rarity = 3,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Slashing,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.BrutalV],
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1021,
                Name = "Flamberge",
                Category = ItemCategory.Weapon,
                BaseDescription = "A big squiggly sword.",
                Rarity = 3,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Slashing,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.BleedVI],
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1022,
                Name = "Halberd",
                Category = ItemCategory.Weapon,
                BaseDescription = "A long ass axe.",
                Rarity = 2,
                Size = ItemSize.Bulky,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Slashing,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.SweepVIII],
                WeaponSpeed = WeaponSpeedType.Balanced,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1023,
                Name = "Greataxe",
                Category = ItemCategory.Weapon,
                BaseDescription = "A great ass axe.",
                Rarity = 2,
                Size = ItemSize.Bulky,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Slashing,
                WeaponDamage = WeaponDamageDie.D10,
                WeaponEffects = [WeaponEffectType.HeavyII],
                WeaponSpeed = WeaponSpeedType.Slow,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1024,
                Name = "Greatsword",
                Category = ItemCategory.Weapon,
                BaseDescription = "A great ass sword.",
                Rarity = 2,
                Size = ItemSize.Bulky,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Slashing,
                WeaponDamage = WeaponDamageDie.D10,
                WeaponEffects = [WeaponEffectType.SweepVI],
                WeaponSpeed = WeaponSpeedType.Slow,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1100,
                Name = "Smoker",
                Category = ItemCategory.Weapon,
                BaseDescription = "Light explosive releasing a vision obscuring cloud in a 3x3 range.",
                Rarity = 1,
                Size = ItemSize.Petty,
                QtyLimit = 3,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Thrown,
                WeaponDamage = null,
                WeaponEffects = null,
                WeaponSpeed = null,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 1101,
                Name = "Sharper",
                Category = ItemCategory.Weapon,
                BaseDescription = "Moderate explosive releasing shrapnel in a wide range.",
                Rarity = 1,
                Size = ItemSize.Petty,
                QtyLimit = 3,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Thrown,
                WeaponDamage = null,
                WeaponEffects = null,
                WeaponSpeed = null,
                IsPlayerCreated = false,
                IsStackable = true
            },
            new Item
            {
                Id = 1200,
                Name = "Bow",
                Category = ItemCategory.Weapon,
                BaseDescription = "A simple bow.",
                Rarity = 1,
                Size = ItemSize.Bulky,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Ranged,
                WeaponDamage = WeaponDamageDie.D6,
                WeaponEffects = null,
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1201,
                Name = "Breech Loading Pistol",
                Category = ItemCategory.Weapon,
                BaseDescription = "A simple firearm.",
                Rarity = 2,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Ranged,
                WeaponDamage = WeaponDamageDie.D6,
                WeaponEffects = [WeaponEffectType.HeavyI,WeaponEffectType.Reload,WeaponEffectType.RangeNear],
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1202,
                Name = "Crossbow",
                Category = ItemCategory.Weapon,
                BaseDescription = "Not Karil's.",
                Rarity = 2,
                Size = ItemSize.Bulky,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Ranged,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = null,
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1203,
                Name = "Modular Crossbow",
                Category = ItemCategory.Weapon,
                BaseDescription = "A convenient crossbow.",
                Rarity = 3,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Ranged,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.Modular],
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1204,
                Name = "Blunderbus",
                Category = ItemCategory.Weapon,
                BaseDescription = "For bussin' blunderously.",
                Rarity = 3,
                Size = ItemSize.Regular,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Ranged,
                WeaponDamage = WeaponDamageDie.D8,
                WeaponEffects = [WeaponEffectType.HeavyII,WeaponEffectType.Reload,WeaponEffectType.RangeNear,WeaponEffectType.Blast],
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1205,
                Name = "Greatbow",
                Category = ItemCategory.Weapon,
                BaseDescription = "A pretty great bow.",
                Rarity = 3,
                Size = ItemSize.Bulky,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Ranged,
                WeaponDamage = WeaponDamageDie.D10,
                WeaponEffects = null,
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 1206,
                Name = "Breech Loading Rifle",
                Category = ItemCategory.Weapon,
                BaseDescription = "Not a Win(ton)chester.",
                Rarity = 3,
                Size = ItemSize.Bulky,
                QtyLimit = 1,
                Uses = null,
                WeaponCategory = WeaponCategoryType.Ranged,
                WeaponDamage = WeaponDamageDie.D10,
                WeaponEffects = [WeaponEffectType.HeavyII,WeaponEffectType.Reload],
                WeaponSpeed = WeaponSpeedType.Fast,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 2100,
                Name = "Old Shield",
                Category = ItemCategory.Armor,
                BaseDescription = "Worn shield, simple but effective.",
                Rarity = 1,
                Size = ItemSize.Regular,
                Uses = null,
                ArmorType = ArmorType.Shield,
                ArmorValue = 1,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 2101,
                Name = "Light Armor",
                Category = ItemCategory.Armor,
                BaseDescription = "Generic leather armor.",
                Rarity = 1,
                Size = ItemSize.Regular,
                Uses = null,
                ArmorType = ArmorType.Light,
                ArmorValue = 1,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 2102,
                Name = "Medium Armor",
                Category = ItemCategory.Armor,
                BaseDescription = "Generic chainmail.",
                Rarity = 1,
                Size = ItemSize.Regular,
                Uses = null,
                ArmorType = ArmorType.Medium,
                ArmorValue = 2,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 2103,
                Name = "Heavy Armor",
                Category = ItemCategory.Armor,
                BaseDescription = "Generic plate armor.",
                Rarity = 1,
                Size = ItemSize.Bulky,
                Uses = null,
                ArmorType = ArmorType.Heavy,
                ArmorValue = 3,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 5000,
                Name = "Unknown Gem",
                Category = ItemCategory.Story,
                BaseDescription = "A gem of unknown kind.",
                Size = ItemSize.Petty,
                Uses = null,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 5001,
                Name = "Letter From Collector",
                Category = ItemCategory.Story,
                BaseDescription = "A gem of unknown kind.",
                Size = ItemSize.Petty,
                Uses = null,
                IsPlayerCreated = false,
                IsStackable = false
            },
            new Item
            {
                Id = 5002,
                Name = "A list with names",
                Category = ItemCategory.Story,
                BaseDescription = "I wonder who's on it?",
                Size = ItemSize.Petty,
                Uses = null,
                IsPlayerCreated = false,
                IsStackable = false
            }
        });
    }

    // ============================================================
    // LOAD CUSTOM ITEMS FROM JSON
    // ============================================================
    private async Task LoadCustomItemsFromStorage()
    {
        try
        {
            var customItems = await _customStorageService.LoadCustomItemsAsync();
            _items.AddRange(customItems);
            System.Diagnostics.Debug.WriteLine($"Loaded {customItems.Count} custom items from JSON.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading custom items: {ex.Message}");
        }
    }

    // ============================================================
    // INTERFACE IMPLEMENTATIONS
    // ============================================================

    public Task<List<Item>> GetAllItemsAsync()
    {
        return Task.FromResult(_items);
    }

    public Task<Item?> GetItemByIdAsync(int id)
    {
        var result = _items.FirstOrDefault(i => i.Id == id);
        return Task.FromResult(result);
    }

    public Task<List<Item>> GetItemsByCategoryAsync(ItemCategory? category)
    {
        if (category == null)
            return Task.FromResult(_items);

        var result = _items.Where(i => i.Category == category).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Item>> GetFoundationItemsAsync()
    {
        var result = _items.Where(i => !i.IsPlayerCreated).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Item>> GetPlayerCreatedItemsAsync()
    {
        var result = _items.Where(i => i.IsPlayerCreated).ToList();
        return Task.FromResult(result);
    }

    public Task<List<Item>> QueryItemsAsync(ItemQueryCriteria criteria)
    {
        var query = _items.AsQueryable();

        // === CATEGORY FILTER ===
        if (criteria.Category != null)
            query = query.Where(i => i.Category == criteria.Category);

        if (criteria.AllowedCategories != null && criteria.AllowedCategories.Any())
        {
            // Filter by any of the allowed categories
            query = query.Where(i => i.Category.HasValue && criteria.AllowedCategories.Contains(i.Category.Value));
        }
        // Also handle single category
        else if (criteria.Category != null)
        {
            query = query.Where(i => i.Category == criteria.Category);
        }

        // === EXACT ITEMS ===
        if (criteria.SpecificItemIds != null && criteria.SpecificItemIds.Any())
            query = query.Where(i => criteria.SpecificItemIds.Contains(i.Id));

        // === SEARCH TERM ===
        if (!string.IsNullOrEmpty(criteria.SearchTerm))
            query = query.Where(i => i.Name.Contains(criteria.SearchTerm, StringComparison.OrdinalIgnoreCase));

        // === VALUE FILTERS ===
        if (criteria.MinValue.HasValue)
            query = query.Where(i => i.ValueInChips >= criteria.MinValue.Value);

        if (criteria.MaxValue.HasValue)
            query = query.Where(i => i.ValueInChips <= criteria.MaxValue.Value);

        // === RARITY FILTER ===
        if (criteria.MaxRarity.HasValue)
            query = query.Where(i => i.Rarity <= criteria.MaxRarity.Value);

        // === SIZE FILTER ===
        if (criteria.Size != null)
            query = query.Where(i => i.Size == criteria.Size);

        if (criteria.AllowedSizes != null && criteria.AllowedSizes.Any())
            query = query.Where(i => criteria.AllowedSizes.Contains(i.Size));

        // === SOURCE FILTERS ===
        if (!criteria.IncludePlayerCreated)
            query = query.Where(i => !i.IsPlayerCreated);

        if (!criteria.IncludeFoundation)
            query = query.Where(i => i.IsPlayerCreated);

        // === EXCLUDE CONSUMABLES ===
        if (criteria.ExcludeItemsWithUses)
            query = query.Where(i => i.Uses == null || i.Uses == 0);

        // === LIMIT ===
        if (criteria.MaxResults.HasValue)
            query = query.Take(criteria.MaxResults.Value);

        return Task.FromResult(query.ToList());
    }

    public Task<Item> CreateCustomItemAsync(CreateCustomItemRequest request)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Item name is required.");
        }

        // Get the next available ID (start from 9900001 for custom items)
        int newId = 9900001;

        // Find the highest existing custom item ID
        var maxCustomId = _items
            .Where(i => i.IsPlayerCreated)
            .Select(i => i.Id)
            .DefaultIfEmpty(9900000)
            .Max();

        // If there are custom items, use the next ID after the highest
        if (maxCustomId >= 9900001)
        {
            newId = maxCustomId + 1;
        }

        var newItem = new Item
        {
            Id = newId,
            Name = request.Name,
            Category = request.Category,
            BaseDescription = request.BaseDescription,
            Size = request.Size,
            ValueInChips = request.ValueInChips,
            Rarity = request.Rarity,
            Uses = request.Uses,
            QtyLimit = request.QtyLimit,
            WeaponCategory = request.WeaponCategory,
            WeaponDamage = request.WeaponDamage,
            WeaponEffects = request.WeaponEffects ?? new List<WeaponEffectType>(),
            WeaponSpeed = request.WeaponSpeed,
            ArmorValue = request.ArmorValue,
            ArmorType = request.ArmorType,
            IsPlayerCreated = true,
            IsStackable = request.IsStackable
        };

        _items.Add(newItem);

        // === SAVE TO JSON ===
        try
        {
            Task.Run(() => _customStorageService.SaveCustomItemAsync(newItem));
            System.Diagnostics.Debug.WriteLine($"Custom item saved to JSON: {newItem.Name} (ID: {newItem.Id})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving custom item to JSON: {ex.Message}");
        }

        return Task.FromResult(newItem);
    }

    public Task<bool> UpdateItemAsync(Item item)
    {
        var existing = _items.FirstOrDefault(i => i.Id == item.Id);
        if (existing == null) return Task.FromResult(false);

        var index = _items.IndexOf(existing);
        _items[index] = item;

        // If it's a custom item, update in JSON too
        if (item.IsPlayerCreated)
        {
            try
            {
                Task.Run(() => _customStorageService.SaveCustomItemAsync(item));
                System.Diagnostics.Debug.WriteLine($"Custom item updated in JSON: {item.Name} (ID: {item.Id})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating custom item in JSON: {ex.Message}");
            }
        }

        return Task.FromResult(true);
    }

    public Task<bool> DeleteItemAsync(int id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item == null) return Task.FromResult(false);

        _items.Remove(item);

        // If it's a custom item, delete from JSON too
        if (item.IsPlayerCreated)
        {
            try
            {
                Task.Run(() => _customStorageService.DeleteCustomItemAsync(id));
                System.Diagnostics.Debug.WriteLine($"Custom item deleted from JSON: {item.Name} (ID: {item.Id})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting custom item from JSON: {ex.Message}");
            }
        }

        return Task.FromResult(true);
    }

    public Task<List<Item>> GetCustomItemsAsync()
    {
        var result = _items.Where(i => i.IsPlayerCreated).ToList();
        return Task.FromResult(result);
    }
}