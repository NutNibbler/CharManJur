using CharManJur.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CharManJur.Services;

public class BackgroundDataService : IBackgroundDataService
{
    private readonly List<CharacterBackground> _backgrounds = new()
    {
        new CharacterBackground
        {
            Id = 1,
            Name = "Assassin",
            Description = "You may be a Briar from Rru or just an assassin’s pledge at the guild in Grono. Whatever you are, you know how to kill, and do it quietly",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 6 }, //Rupee
                new StartingItem { ItemId = 3, Quantity = 1 }, //Mark
                new StartingItem { ItemId = 1100, Quantity = 1 }, //Smoker
                new StartingItem { ItemId = 130, Quantity = 1 }, //Poison
                new StartingItem { ItemId = 1000, Quantity = 1 } //Concealed Dagger
            }
        },
        new CharacterBackground
        {
            Id = 2,
            Name = "Cutpurse",
            Description = "You can lift a coin from a pocket like your life depends on it… it probably did at some point. You may have thieved for a crew or you owe money to a Collector. Hope to all 6 Gods it's not the latter.",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 3, Quantity = 2 },   // Mark
                new StartingItem { ItemId = 100, Quantity = 1 },   // Thieving Kit
                new StartingItem { ItemId = 110, Quantity = 1 },   // Air Bladder
                new StartingItem { ItemId = 5000, Quantity = 1, PlayerNote = "No fence has been willig to buy this." }   // Unknown Gem
            }
        },
        new CharacterBackground
        {
            Id = 3,
            Name = "Bounty Hunter",
            Description = "There’s always work for a bounty hunter in Vermil. The Way Road is rife with peril, brigands, thieves, cultists, monsters, you name it. Local tradesmen pay high to keep the road safe. ",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 111, Quantity = 1 },   // Manacles
                new StartingItem { ItemId = 112, Quantity = 1 },   // 12' Net
                new StartingItem { ItemId = 5001, Quantity = 1 },   // Letter From Collector
                new StartingItem { ItemId = 2, Quantity = 8 },   // Rupee
                new StartingItem { ItemId = 3, Quantity = 1 }   // Mark
            }
        },
        new CharacterBackground
        {
            Id = 4,
            Name = "Farmer",
            Description = "The red soil of Vermil has stained your hands, your cheeks. You know hard work… and you’re tired of it. Or your farm is getting turned over by Collectors. Or you ran away. Or…",
            VigorModifier = 1,
            AgilityModifier = 0,
            MindModifier = 0,
            SpiritModifier = 0,
            SkillBonuses = new List<BGSkillBonuses>
            {
                // No skill bonuses for Farmer
            },
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 9 },   // Rupee
                new StartingItem { ItemId = 1001, Quantity = 1 },   // Pitchfork
                new StartingItem { ItemId = 113, Quantity = 1 }   // Ration
            }
        },
        new CharacterBackground
        {
            Id = 5,
            Name = "Healer",
            Description = "You may have apprenticed a doctor in Grono or some other town or village in Vermil. You might be a dropout from Ivory College. Regardless, you know your way around a wound.",
            SkillBonuses = new List<BGSkillBonuses>
            {
                new BGSkillBonuses { SkillName = "Heal", Bonus = 1}
            },
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 9 },   // Rupee
                new StartingItem { ItemId = 114, Quantity = 1 },   // Antitoxin
                new StartingItem { ItemId = 104, Quantity = 1 }   // Healeing Supplies
            }
        },
        new CharacterBackground
        {
            Id = 6,
            Name = "Inquisitor",
            Description = "You’ve shown a proclivity for rooting out heresy on the shores of Vermil, or possibly elsewhere on or outside of Ysol. Consult the religion section. Either way, you are here to burn the heretics.",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 4 },   // Rupee
                new StartingItem { ItemId = 3, Quantity = 1 },   // Mark
                new StartingItem { ItemId = 131, Quantity = 1 },   // Flash Powder
                new StartingItem { ItemId = 111, Quantity = 1 },   // Manacles
                new StartingItem { ItemId = 5002, Quantity = 1 }   // A list with names
            }
        },
        new CharacterBackground
        {
            Id = 7,
            Name = "Magekiller",
            Description = "You may belong to an order of magekillers like The Sceptre. Perhaps you’re self-taught and plying your trade to the highest bidder. Whatever the mess, mages are dangerous and you’re really good at killing them. ",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 4 },   // Rupee
                new StartingItem { ItemId = 3, Quantity = 1 },   // Mark
                new StartingItem { ItemId = 116, Quantity = 1 },   // Earplugs
                new StartingItem { ItemId = 132, Quantity = 1 },   // Witching Salt
                new StartingItem { ItemId = 115, Quantity = 1 }   // 50' Rope
            }
        },
        new CharacterBackground
        {
            Id = 8,
            Name = "Minstrel",
            Description = "You play a good song. You lay a good lay. Entertain the masses and so forth. Your ability to captivate has opened as many doors as it's closed, and most of those doors that closed were behind you with a grin on your teeth. You have an endearing fan.",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 6 },   // Rupee
                new StartingItem { ItemId = 3, Quantity = 1 }   // Mark
            },
            ItemChoices = new List<ItemChoice>
            {
                new ItemChoice
                    {
                        Id = 1,
                        Prompt = "Choose your starting instruments:",
                        Description = "You may choose up to 2 instruments.",
                        RecommendedMin = 2,
                        RecommendedMax = 2,
                        AllowDuplicates = false,
                        MaxDuplicatesPerItem = 1,
                        AllowCustomItems = true,
                        CustomItemCategory = "Instrument",
                        // ===== IMPORTANT: Set QueryCriteria =====
                        QueryCriteria = new ItemQueryCriteria
                        {
                            Category = ItemCategory.Instrument,
                            IncludeFoundation = true,
                            IncludePlayerCreated = true,
                            MaxResults = 100
                        }
                    }
            }
        },
        new CharacterBackground
        {
            Id = 9,
            Name = "Smuggler",
            Description = "You’ve a penchant for getting valuable things to places and kin where they shouldn’t be. It pays nicely. You may have been a pirate sailing the Kapaluan or a backstabbing Tradesman, but you run a good racket.",
            SkillBonuses = new List<BGSkillBonuses>
            {
                new BGSkillBonuses { SkillName = "Diplomacy", Bonus = 1}
            },
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 8 },   // Rupee
                new StartingItem { ItemId = 3, Quantity = 1 },   // Mark
                new StartingItem { ItemId = 133, Quantity = 1 }   // 10' Tarp
            }
        },
        new CharacterBackground
        {
            Id = 10,
            Name = "Solider",
            Description = "Whether you’re still in or you’re out, you’re always a soldier. You may have been a Rruen Navy officer or you’re a veteran of the Balkham Wars. 5 Gods forbid you’re a deserter…",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 7 },   // Rupee
                new StartingItem { ItemId = 3, Quantity = 1 },   // Mark
                new StartingItem { ItemId = 117, Quantity = 1 },   // Shovel
                new StartingItem { ItemId = 2100, Quantity = 1 },   // Old Shield
                new StartingItem { ItemId = 118, Quantity = 1 }   // Deck of Cards
            }
        },
        new CharacterBackground
        {
            Id = 11,
            Name = "Spelunker",
            Description = "The claustrophobic bowels of a cave have never deterred you from getting what’s yours. Well… by finder’s keepers standards. You may find lost items for the Constelliary or another independent, anthropological community. ",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 3, Quantity = 1 },   // Mark
                new StartingItem { ItemId = 120, Quantity = 1 },   // Grappling Hook
                new StartingItem { ItemId = 119, Quantity = 1 }   // 10' Pole
            }
        },
        new CharacterBackground
        {
            Id = 12,
            Name = "Cleric",
            Description = "You may answer the call for any of the 6 Gods… or 5, depending on what you believe, or you may speak for a spirit. Either way, your relationship to your faith is… more than natural, you have ONE Miracle(Ask your DM)",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 8 },   // Rupee
                new StartingItem { ItemId = 121, Quantity = 1 },   // Holy Book
                new StartingItem { ItemId = 122, Quantity = 1 }   // Incense
            }
        },
        new CharacterBackground
        {
            Id = 13,
            Name = "Ranger",
            Description = "You’ve an excellent understanding of the wilderness and what it can provide and a proclivity for tracking. Rangers are often hired by Surveying groups to navigate the deadly wilds or even the Constelliary to locate some peculiar, natural phenomenon. \r\n You may choose one TINY, SEMI DOMESTIC familiar.",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 6 },   // Rupee
                new StartingItem { ItemId = 123, Quantity = 1 }   // Bear Trap
            },
            FamiliarChoices = new List<FamiliarChoice>
            {
                new FamiliarChoice
                {
                    Id = 1,
                    Prompt = "Choose a familiar(Must be TINY and SEMI DOMESTIC):",
                    RecommendedMin = 1,
                    RecommendedMax = 1,
                    AllowCustomFamiliar = true,
                    QueryCriteria = new FamiliarQueryCriteria
                    {
                        // ===== SIZE FILTER =====
                        Size = "Tiny",
                        // ===== INTELLIGENCE FILTER =====
                        Intelligence = "SemiDomestic",  // Rangers can only have wild familiars
                        // ===== CLASS FILTER =====
                        //AllowedSpecies = new List<string> { "Aves", "Mammalia", "Reptilia" },
                        IncludeFoundation = true,
                        IncludePlayerCreated = true
                    }
                }
            }
        },
        new CharacterBackground
        {
            Id = 14,
            Name = "Seer",
            Description = "You’ve a knack for interpreting the stars, seeing the future, analyzing the supernatural… or you’re a liar. Whether you believe you can see the future or you run a good con, kin are easier for you to read… and so too are they easier to lie to.",
            SkillBonuses = new List<BGSkillBonuses>
            {
                new BGSkillBonuses { SkillName = "Deception", Bonus = 1}
            },
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 3 },   // Rupee
                new StartingItem { ItemId = 3, Quantity = 1 },   // Marks
                new StartingItem { ItemId = 124, Quantity = 1 },   // Tarot Cards
                new StartingItem { ItemId = 125, Quantity = 1 }   // Fake Gems
            }
        },
        new CharacterBackground
        {
            Id = 15,
            Name = "Sapper",
            Description = "You have a hard time getting along with the riff raff. It’s probably because kin have a hard time trusting you. You’re always sweating, your eyes are always dashing. If only they knew that it was because you’re holding volatile explosives. Then they’d understand.",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 4 },   // Rupee
                new StartingItem { ItemId = 110, Quantity = 1 },   // Air Bladder
                new StartingItem { ItemId = 1101, Quantity = 1 },   // Sharper
                new StartingItem { ItemId = 126, Quantity = 1 }   // Cusser
            }
        },
        new CharacterBackground
        {
            Id = 16,
            Name = "Aurifex",
            Description = "The wilds of Ysol hold ancient, arcane knowledge whether it be in the Machine Ruins or nature itself… and you want it. Better yet, you know how to eke it out. Rare is a magical conundrum that befuddles you for too long. Otherwise you’d be dead. ",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 2, Quantity = 3 },   // Rupee
                new StartingItem { ItemId = 127, Quantity = 1 },   // Magnifying Glass
                new StartingItem { ItemId = 128, Quantity = 1 },   // Vial of Vym
                new StartingItem { ItemId = 5003, Quantity = 1 }   // Anicent Map
            }
        },
        new CharacterBackground
        {
            Id = 17,
            Name = "Noble",
            Description = "Fallier boasts a deep, aristocratic hierarchy from which you have roots. Why did you fly the coop? All that money… All that promise. Did you cheat a duel? Fall in love with a lee-kin? Murder your parents in a fit of psychotic rage? Maybe you left on good terms and keep a steady flow of Marks into your loving arms. Who knows but you…",
            StartingItems = new ObservableCollection<StartingItem>
            {
                new StartingItem { ItemId = 3, Quantity = 5 },   // Mark
                new StartingItem { ItemId = 129, Quantity = 1 }   // Spyglass
            }
        }

    };

    public Task<List<CharacterBackground>> GetBackgroundsAsync()
    {
        return Task.FromResult(_backgrounds);
    }

    public Task<CharacterBackground?> GetBackgroundByIdAsync(int id)
    {
        var result = _backgrounds.FirstOrDefault(b => b.Id == id);
        return Task.FromResult(result);
    }
}