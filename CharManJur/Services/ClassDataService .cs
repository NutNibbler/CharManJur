using CharManJur.Models;

namespace CharManJur.Services;

public class ClassDataService : IClassDataService
{
    //WARNING CLASS FEATURE IDS ARE SHARED ACROSS ALL CLASSES!!! DO NOT USE THE SAME FEATURE ID BETWEEN TWO CLASSES!!!
    private readonly List<CharacterClass> _classes = new()
    {
        new CharacterClass
        {
            Id = 1,
            ClassName = "Warrior",
            RecurringBenefit = "Roll TWICE for HP and select the BETTER result.\r\nChoose 1 ADDITIONAL weapon. Gain that weapon’s SKILL PROFICIENCY. \r\n",
            CompatibleCampaigns = {"Godrick", "Sandbox"},
            LevelingSkillBonus = "1 Vigor, 1 Any",
            Features = new List<ClassFeature>
            {
                new ClassFeature
                {
                    Id = 1,
                    Name = "Ironclad",
                    Description = "Full armor does not impose its disadvantages on you."
                },
                new ClassFeature
                {
                    Id = 2,
                    Name = "Deafening Roar",
                    Description = "Once per day, you may unlease a deafening war cry triggering a Morale Check amongst all enemies near you. Enemies that fail make impaired damage rolls against creatures other than you. "
                },
                new ClassFeature
                {
                    Id = 3,
                    Name = "Rage",
                    Description = "Once per short rest, while not wearing armor, you may rage. You gain 1 armor and add a bonus weapon die. Rage lasts until you stop attacking on 1 of your turns."
                },
                new ClassFeature
                {
                    Id = 4,
                    Name = "Defender",
                    Description = "Twice per short rest, while holding a shield, you may choose to make the block reaction for a close ally. "
                },
                new ClassFeature
                {
                    Id = 5,
                    Name = "Berserker",
                    Description = "Upon taking critcal damage, you may assume 3 fatigue to ignore the consequences and raise your weapon die. Damage is taken to your VIG as normal and you still perish at 0 VIG. When combat ends, roll on the wounds table."
                },
                new ClassFeature
                {
                    Id = 6,
                    Name = "Sentinel",
                    Description = "Twice per short rest, you may assume 1 fatigue and stop any close creature of similar size attempting to leave your vicinity. This synergizes with the tank feature. "
                },
                new ClassFeature
                {
                    Id = 7,
                    Name = "Tank",
                    Description = "You may grapple creatures 1 size larger than you. Scales with any feature specifying the size of a creature as a condition."
                },
                new ClassFeature
                {
                    Id = 8,
                    Name = "Brawler",
                    Description = "Your fists are now legally categorized as fast weapons and have a damage die of 1D4 bludgeoning with shock 4. If damage die becomes raised, shock increases to within the highest 2 digits of the die."
                },
                new ClassFeature
                {
                    Id = 9,
                    Name = "Pack Mule",
                    Description = "Increase your pack capacity by 2 slots."
                }
            }
        },
        new CharacterClass
        {
            Id = 2,
            ClassName = "Rogue",
            RecurringBenefit = "When you attack from SURPRISE or from STEALTH, raise your weapon damage die.\r\nAquire 2 TRAINING POINTS.\r\n",
            CompatibleCampaigns = {"Godrick", "Sandbox"},
            LevelingSkillBonus = "1 Agility, 1 Any",
            Features = new List<ClassFeature>
            {
                new ClassFeature
                {
                    Id = 10,
                    Name = "Long Shot",
                    Description = "Make ranged attacks with viable weapons at distant ranges with an Aim Check. The Target Number varies depending on how distant the target is."
                },
                new ClassFeature
                {
                    Id = 11,
                    Name = "Stunning Strike",
                    Description = "Assume 1 fatigue to reduce an enemy creature’s action economy by 1 AP on their next turn."
                },
                new ClassFeature
                {
                    Id = 12,
                    Name = "Quick Shot",
                    Description = "You can reload a firearm for 1 AP if you have not moved this turn and have a free hand. "
                },
                new ClassFeature
                {
                    Id = 13,
                    Name = "Shadow Step",
                    Description = "While wearing 2 armor or less, under cover of darkness, assume a +2 bonus to stealth checks."
                },
                new ClassFeature
                {
                    Id = 14,
                    Name = "Deadeye",
                    Description = "After a ranged attack, gain a bonus weapon die with this weapon on the same target until you attack another target or the combat ends."
                },
                new ClassFeature
                {
                    Id = 15,
                    Name = "Quickened Step",
                    Description = "You may assume 1 additional fatigue when making the dodge reaction to move up to near in any direction."
                },
                new ClassFeature
                {
                    Id = 16,
                    Name = "Swift Strike",
                    Description = "While attacking, you may hit a second target within range, rolling your weapon damage dice without any bonus dice."
                },
                new ClassFeature
                {
                    Id = 17,
                    Name = "Duelist",
                    Description = "While wielding only 1 fast melee weapon, add a bonus weapon die to your attacks."
                },
                new ClassFeature
                {
                    Id = 18,
                    Name = "Evasion",
                    Description = "You may assume 3 fatigue to fully negate the damage of a blast effect no greater than close range."
                }
            }
        },
        new CharacterClass
        {
            Id = 3,
            ClassName = "Arcanist",
            Description = "You can read and write in RUNIC, the language of arcana, and own a GRIMOIRE within which you may inscribe your SPELLS. Your grimoire begins with 2 SPELLS from the ARCANIST SPELL POOL. You can also SCULPT CANTRIPS. \r\n\r\nTo CAST a SPELL or CANTRIP:\r\nSelect an amount of MAGIC DICE you wish to INVEST in your spell. \r\nRolling a 4-6 on the MAGIC DIE will always result in 1 FATIGUE\r\nIf you get a SERIES, 2-4 dice that match, something has gone very wrong. Consider the SUM of each matching die as damage to your HP. If there’s excess damage, it’s applied to your MIND rather than your VIG. If this damage is equal to or larger than 4, it has the blast property.\r\n\r\nTo CAST a CANTRIP:\r\nInvesting 1 MAGIC DIE into a bolt of raw energy would equate to 1d6 + 1 to a NEAR target. \r\nInvesting more MAGIC DICE nets you more damage. \r\n\r\nThe Arcane Formula for casting cantrips looks like this: [sum of dice] + [number of dice rolled]. \r\nFor future reference, the formula will look like: [sum] + [dice]\r\n\r\nYou can learn new TECHNIQUES for sculpting RAW ENERGY into various, more intricate forms. This is far more volatile than the academic casting you find in a grimoire. Those spells have been worked out over years like math equations on a chalkboard… most of the time. \r\n\r\nTechniques, Spells, and Mutations will be in the MAGIC SECTION later in the book. \r\n",
            RecurringBenefit = "+1 MAGIC DIE (MD-1D6).\r\n+1 QUIP (see Quips in the List Section)",
            CompatibleCampaigns = {"Godrick", "Sandbox"},
            LevelingSkillBonus = "1 Mind, 1 Any",
            ClassUnlockableTypes = new List<SubFeatureType>
            {
                SubFeatureType.Quip  // Arcanist always has access to Quips
            },
            Features = new List<ClassFeature>
            {
                new ClassFeature
                {
                    Id = 19,
                    Name = "Spell Inscription",
                    Description = "Aquire one new spell from the Arcanist spell pool. This feature may be chosen multiple times.",
                    UnlockableTypes = new List<SubFeatureType>
                    {
                        SubFeatureType.Spell
                    }
                },
                new ClassFeature
                {
                    Id = 20,
                    Name = "Technique",
                    Description = "Aqcuire an additional technique from the Techinque listfor sculpting cantrips. This feature may be chosen multiple times.",
                    UnlockableTypes = new List<SubFeatureType>
                    {
                        SubFeatureType.Technique
                    }
                }
            }
        },
        new CharacterClass
        {
            Id = 4,
            ClassName = "Shaman",
            Description = "You have invited a consecrated spirit to share in your body through careful ritual. \r\nThis spirit lives within you. It listens to your thoughts. It speaks in your mind. \r\nSome spirits were kin. Some spirits are demons. The nature of your spirit depends on your ritual. \r\n\r\nYour spirit assesses your worth, determines your favour. \r\nHow worthy you are at the end of the day determines how much favour you replenish. \r\n\r\nThe spirit within you devours your own spirit in exchange for powerful abilities(features).\r\nAbilities will often require you to roll a Spirit Die (1D6). \r\nANY TIME YOU ROLL SPIRIT DICE:\r\nEach die that shows a 4-6 indicates a loss of 2 SPI. Each die that shows a 1-3 indicates a loss of 1 SPI.\r\n\r\nIf your SPI is reduced to zero the consecrated spirit within you replaces your own.\r\nIf you DIE your spirit JUMPS to the NEAREST VESSEL. \r\n\r\nIf you see [dice], that means NUMBER OF DICE ROLLED. If you see [sum], that means SUM OF DICE ROLLED.\r\n",
            RecurringBenefit = "+1 SPIRIT DIE (SD-1D6). \r\n+2 FAVOUR\r\n",
            CompatibleCampaigns = {"Godrick", "Sandbox"},
            LevelingSkillBonus = "1 Spirit, 1 Any",
            Features = new List<ClassFeature>
            {
                new ClassFeature
                {
                    Id = 21,
                    Name = "Guidance",
                    Description = "Give an ally you can touch a +1 bonus to their next skill check."
                },
                new ClassFeature
                {
                    Id = 22,
                    Name = "Mirror Image",
                    Description = "Your consecrated spirit materializes a duplicate of yourself up to any point within near distance. Invest 1 SD per duplicate you wish to create. Your duplicates cannot attack and vanish upon taking damage. "
                },
                new ClassFeature
                {
                    Id = 23,
                    Name = "Smite",
                    Description = "Invest any number of Spirit Die [sum] and/or Favour into destroying the spirit of any creature you can touch. Damage is dealt directly to their SPI score. SPI Loss rules still apply."
                },
                new ClassFeature
                {
                    Id = 24,
                    Name = "Consecrate Weapon",
                    Description = "Consecrate a weapon of your choosing with the power of your divine spirit. Invest any amount of Favour into your next melee or ranged attack with that weapon. Roll a Spirit Die."
                },
                new ClassFeature
                {
                    Id = 25,
                    Name = "Co-Host",
                    Description = "Invite a minor spirit into the carcass of a freshly dead or spiritless creature. They assume the statistics of the creature and follow basic commands. The creature can be no larger than yourself. You may keep as many creatures this way as you have Spirit Dice. When the creature dies, roll a Spirit Die. "
                },
                new ClassFeature
                {
                    Id = 26,
                    Name = "Aegis",
                    Description = "Sacrifice your spirit to restore the Ability Score Loss of your allies. Roll any number of Spirit Die. On a dice showing 1-3, restore 1 ability point. On a dice showing 4-6, restore 2 ability points. "
                }
            }
        },
        new CharacterClass
        {
            Id = 5,
            ClassName = "Artificer",
            Description = "The Machine Age provides the curious mind with tools and materials to create profound contraptions, gadgets, etc… You’re an inventor. With the right materials, you are able to craft a construct no larger than 2 cubic feet with a singular purpose no longer than 5 words. Every mad scientist needs their little Igors running around to assist… or blow up or whatever. \r\n\r\nThe artificer begins with 1 blueprint from the blueprints list. Any others are selected if you choose the Blueprint feature on level-up. \r\n",
            RecurringBenefit = "+1 Construct",
            CompatibleCampaigns = {"Godrick", "Sandbox"},
            LevelingSkillBonus = "2 Mind",
            ClassUnlockableTypes = new List<SubFeatureType>
            {
                SubFeatureType.Blueprint  // Artificer always has access to Blueprints
            },
            Features = new List<ClassFeature>
            {
                new ClassFeature
                {
                    Id = 27,
                    Name = "Blueprint",
                    Description = "Select a blueprint which allows you to craft items defined by its description. Each blueprint has a materials cost. See the blueprints list in the lists section.",
                    UnlockableTypes = new List<SubFeatureType>
                    {
                        SubFeatureType.Blueprint
                    }
                },
                new ClassFeature
                {
                    Id = 28,
                    Name = "Triple Point",
                    Description = "With one Vial of Vym, you may attempt to change the state of any mostly pure matter into another state."
                },
                new ClassFeature
                {
                    Id = 29,
                    Name = "Overcharge",
                    Description = "You can destroy an item you created to vastly increase its efficacy. Any roll associated with the item is either enhanced or made with advantage."
                },
                new ClassFeature
                {
                    Id = 30,
                    Name = "Enchantment",
                    Description = "With a Vial of Vym and another material associated with the enchantment you wish to perform, you may enchant an item you’ve created, or any other mundane item. Echantments are a handshake mechanism between gamemaster and player, there is no list."
                }
            }
        },
        new CharacterClass
        {
            Id = 6,
            ClassName = "Shifter",
            Description = "Life near the Crucible has altered the collective genome… or it’s a gift from Frjor. Or both?\r\nMany shifters are hunted, whether by fear or for sport or simply to use. Shifting may come with a dose of healthy paranoia… \r\n \r\n Roll an amount of available PD to transform into a different creature. If a dice comes up 4-6, you gain 1 fatigue. \r\nTransforming takes a full turn when done in combat. You do not take damage on double values when transforming. \r\n\r\nRolling and Stability\r\nThe sum of your PD is your Stability. Whenever you take damage, decrease your Stability and make a Stability save. \r\nIf you fail your Stability save, your transformation duration runs out and you revert to your original form. \r\nThe same is true if your Stability is reduced to 0. Your stability returns to its rolled value if combat ends and you’re still an animal.\r\n\r\nWhat you transform into is determined by the traits you choose. What traits you choose are determined by the number of PD you invest:\r\n \r\n PD:    Traits:     Traits List:\r\n 1         2              PD 1\r\n 2         3              PD 1 & PD 2\r\n 3         4              PD 1 & PD 2 & PD 3\r\n 4         5              All Lists\r\n\r\nThe Default Form\r\nIn your new form, your default VIG and AGI are 10. Note that VIG is only used for feats of strength and such, as Stability is used when you take damage. Your SPI stays the same as your base form.\r\n\r\nYour VIG and AGI can be altered by picking certain traits. If you pick 2 traits that alter your attributes, consult the GM to determine the reasonable route forward. If you pick Large (VIG 16) and Strong (VIG 18), it makes sense to use the larger value. However, a combination such as Small (AGI 13, VIG 2) and Strong (VIG 18) would clash (and not really make sense), unless a somewhat plausible explanation for, say, a weightlifting mouse can be agreed upon. Stuart’s on tren, now. \r\n\r\nBy default, you transform into a creature of somewhat similar size and body mass to your base form, with no other notable features, traits or attacks, for a 10 minute duration. That means, without picking any traits, you could be a sheep, or a dog that isn’t very good at smelling or biting, or a pig, a llama, ostrich, etc..\r\n",
            RecurringBenefit = "+1 POLYMORPH DICE (PD-1D6)",
            CompatibleCampaigns = {"Godrick", "Sandbox"},
            LevelingSkillBonus = "2 Spirit"
        }
    };

    public Task<List<CharacterClass>> GetClassesAsync()
    {
        return Task.FromResult(_classes);
    }

    public Task<CharacterClass?> GetClassByIdAsync(int id)
    {
        var classItem = _classes.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(classItem);
    }
}