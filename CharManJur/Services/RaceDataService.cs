using CharManJur.Models;

namespace CharManJur.Services;

public class RaceDataService : IRaceDataService
{
    private readonly ICustomRaceStorageService _customRaceStorage;
    // Hardcoded data for now - easily replaceable with SQLite later
    private readonly List<Race> _races = new()
    {
        new Race
        {
            Id = 1,
            RaceNameId = "godrick_Dwarf",
            CompatibleCampaigns = new List<string> { "Godrick", "Sandbox" },
            Name = "Dwarf",
            Description = "Stout-hearted dwarves champion their family lines for running longer and deeper than the roots of a bloodwood tree. Traditionally, your family and your relatives convey your status and power and open doors that otherwise may have remained shut.",
            FeatureName = "Roll For Cousin",
            FeatureDescription = "Odds are you have a cousin here... wherever here, is. Ask the GM to roll for cousin whenever you feel is appropriate. Roll a D6. On a 4-6, you have a cousin here. have fun GMs!",LimbSets = new List<PrehensileLimbSet>
            {
                new PrehensileLimbSet
                {
                    Id = 1,
                    Name = "Hands",
                    PairType = LimbPairType.Paired,
                    SlotCount = 2,
                    DisplayName = "Hands",
                    Icon = "🫳",
                    SlotIndices = new List<int> { 0, 1 }
                }
            }
        },
        new Race
        {
            Id = 2,
            RaceNameId = "godrick_Fae",
            CompatibleCampaigns = new List<string> { "Godrick", "Sandbox" },
            Name = "Fae",
            Description = "The Fae had only just immigrated from the Faelands when the Arc erupted, cutting their Leylines so that those who came could never return. They attempt to salvage an ancient way of living in a world not their own.",
            FeatureName = "Fae Echoes",
            FeatureDescription = "You sport a physically unique feature with connections to the magical echoes of the long lost Leylines and the Fae spirits that congregate there. You always know the location of the nearest Leyline and can speak with the spirits there too.",LimbSets = new List<PrehensileLimbSet>
            {
                new PrehensileLimbSet
                {
                    Id = 1,
                    Name = "Hands",
                    PairType = LimbPairType.Paired,
                    SlotCount = 2,
                    DisplayName = "Hands",
                    Icon = "🫳",
                    SlotIndices = new List<int> { 0, 1 }
                }
            }
        },
        new Race
        {
            Id = 3,
            RaceNameId = "godrick_Gnole",
            CompatibleCampaigns = new List<string> { "Godrick", "Sandbox" },
            Name = "Gnole",
            Description = "Beavers started forming tight-knit clans along river banks... then they started speaking. Since the Arc erupted, they've formed elaborate, riverside colonies and became effective fishermen, swimmers, dam builders, lumberjacks, shipwrights, etc... ",
            FeatureName = "Gnawtooth",
            FeatureDescription = "There is no wood you've found you can't chew through and wood will suffice for sustenance just as a ration would.",LimbSets = new List<PrehensileLimbSet>
            {
                new PrehensileLimbSet
                {
                    Id = 1,
                    Name = "Hands",
                    PairType = LimbPairType.Paired,
                    SlotCount = 2,
                    DisplayName = "Hands",
                    Icon = "🫳",
                    SlotIndices = new List<int> { 0, 1 }
                }
            }
        },
        new Race
        {
            Id = 4,
            RaceNameId = "godrick_Human",
            CompatibleCampaigns = new List<string> { "Godrick", "Sandbox" },
            Name = "Human",
            Description = "Humans were the Kin of the First Era, the oldest recorded time in Tiver according to the Constelliary. Fast and Resilient, it's no surprise they've persisted.",
            FeatureName = "Ratchet",
            FeatureDescription = "+1 to all lore checks. You've been around, or someone you know has... and they told you about it.",LimbSets = new List<PrehensileLimbSet>
            {
                new PrehensileLimbSet
                {
                    Id = 1,
                    Name = "Hands",
                    PairType = LimbPairType.Paired,
                    SlotCount = 2,
                    DisplayName = "Hands",
                    Icon = "🫳",
                    SlotIndices = new List<int> { 0, 1 }
                }
            },
            VigorModifier = 0,
            AgilityModifier = 0,
            MindModifier = 0,
            SpiritModifier = 0,
            SkillBonuses = new List<RaceSkillBonus>
            {
                new RaceSkillBonus { SkillName = "Lore", Bonus = 1 }
            }
        },
        new Race
        {
            Id = 5,
            RaceNameId = "godrick_Kestrel",
            CompatibleCampaigns = new List<string> { "Godrick", "Sandbox" },
            Name = "Kestrel",
            Description = "Avian kin from Marabou: hovering glass city spheres. Kestrels care deeply for the roles they're born into and their personal freedoms. They were one of the few to allow shelter to contracted Fiends before the Balkham War.",
            FeatureName = "Avian",
            FeatureDescription = "You can fly and you have hollow bones. <i>Remember the hollow bones part, bludgeoning damage dice are always raised against you.</i>",LimbSets = new List<PrehensileLimbSet>
            {
                new PrehensileLimbSet
                {
                    Id = 1,
                    Name = "Hands",
                    PairType = LimbPairType.Paired,
                    SlotCount = 2,
                    DisplayName = "Hands",
                    Icon = "🫳",
                    SlotIndices = new List<int> { 0, 1 }
                }
            }
        },
        new Race
        {
            Id = 6,
            RaceNameId = "godrick_Pygmy",
            CompatibleCampaigns = new List<string> { "Godrick", "Sandbox" },
            Name = "Pygmy",
            Description = "Short, nimble kin from the Kapaluan archipelago. Took to the seas so naturally, it's rare to find a ship crew that doesn't have at least one. Since the Arc fell, and the Kapluan archipelago fell within the Crucible Zone, all that's left of the Pygmy homeland is warped and dangerous.",
            FeatureName = "Rationed",
            FeatureDescription = "Less than half as tall and wide as your contemporaries, you require half the sustenance. Necessary rations for the day are halved.",LimbSets = new List<PrehensileLimbSet>
            {
                new PrehensileLimbSet
                {
                    Id = 1,
                    Name = "Hands",
                    PairType = LimbPairType.Paired,
                    SlotCount = 2,
                    DisplayName = "Hands",
                    Icon = "🫳",
                    SlotIndices = new List<int> { 0, 1 }
                }
            }
        },
        new Race
        {
            Id = 7,
            RaceNameId = "godrick_Fiend",
            CompatibleCampaigns = new List<string> { "Godrick", "Sandbox" },
            Name = "Fiend",
            Description = "Curses, hexlings and other cruel names, as a Fiend, you may have been born in another world - summoned to Tiver under an unethical contract between some Tiverian benefactor and Hell - or you're one of the subsequent generations born in Tiver. Whether you were born under contract or not, life is a struggle for you. Few places in tiver afford an escape from magical servitude.",
            FeatureName = "Infernal",
            FeatureDescription = "You speak a language no other Kin can speak or hear by normal means. It's a vocal cord thing... and a hearing thing?",LimbSets = new List<PrehensileLimbSet>
            {
                new PrehensileLimbSet
                {
                    Id = 1,
                    Name = "Hands",
                    PairType = LimbPairType.Paired,
                    SlotCount = 2,
                    DisplayName = "Hands",
                    Icon = "🫳",
                    SlotIndices = new List<int> { 0, 1 }
                }
            }
        },
        new Race
        {
            Id = 8,
            RaceNameId = "godrick_Construct",
            CompatibleCampaigns = new List<string> { "Godrick", "Sandbox" },
            Name = "Construct",
            Description = "Highly variable, sentient mechanisms. Some were created in the present, some have awoken after centuries of slumber with a blank slate. Some constructs are even born of the will of a battlefield, an amalgamation of armours and weapons magically welded together to form a creature.",
            FeatureName = "Routine Maintenance",
            FeatureDescription = "Your sustenance is daily maintenance. You need 1 flask of oil and a half bell in the morning to tide you over for the day. You needn't sleep, eat, or drink anything else.",LimbSets = new List<PrehensileLimbSet>
            {
                new PrehensileLimbSet
                {
                    Id = 1,
                    Name = "Hands",
                    PairType = LimbPairType.Paired,
                    SlotCount = 2,
                    DisplayName = "Hands",
                    Icon = "🫳",
                    SlotIndices = new List<int> { 0, 1 }
                }
            }
        },
        new Race
        {
            Id = 9,
            RaceNameId = "godrick_Dacoesian",
            CompatibleCampaigns = new List<string> { "Godrick", "Sandbox" },
            Name = "Dacoesian",
            Description = "Solitary wanderers, they walk the lands as the sole survivors of a continent they once dominated deep in the Zhek Sea - an area now known as The Crucible in regards to its relation to The Disruption. Dacoesians sport reptillian features though it's rare for any two to look exactly alike.",
            FeatureName = "Reptile",
            FeatureDescription = "You can survive unnaturally hot temperatures and go without water twice as long as most other Kin.",LimbSets = new List<PrehensileLimbSet>
            {
                new PrehensileLimbSet
                {
                    Id = 1,
                    Name = "Hands",
                    PairType = LimbPairType.Paired,
                    SlotCount = 2,
                    DisplayName = "Hands",
                    Icon = "🫳",
                    SlotIndices = new List<int> { 0, 1 }
                }
            }
        },
        new Race
        {
            Id = 10,
            RaceNameId = "godrick_N'rukk",
            CompatibleCampaigns = new List<string> { "Godrick", "Sandbox" },
            Name = "N'rukk",
            Description = "Hive-Kin that burrow underground. Each burrow has a Matron that bears a psychic connection to each individual N'rukk. No matter where you are, a matron can connect with you... unless you've found a way to sever that communication. N'rukk sport intricate carapace that they typically keep wrapped from head to toe when exposed to sunlight and other mantis-like characteristics.",
            FeatureName = "Handsy",
            FeatureDescription = "You have four arms. Mechanically, two extra hand slots. Goodluck with the character sheet, bitch.",
            // ===== N'RUKK HAS TWO PAIRED LIMB SETS =====
            LimbSets = new List<PrehensileLimbSet>
            {
                new PrehensileLimbSet
                {
                    Id = 1,
                    Name = "Upper Hands",
                    PairType = LimbPairType.Paired,
                    SlotCount = 2,
                    DisplayName = "Upper Hands",
                    Icon = "🫳",
                    SlotIndices = new List<int> { 0, 1 }
                },
                new PrehensileLimbSet
                {
                    Id = 2,
                    Name = "Lower Hands",
                    PairType = LimbPairType.Paired,
                    SlotCount = 2,
                    DisplayName = "Lower Hands",
                    Icon = "🫳",
                    SlotIndices = new List<int> { 2, 3 }
                }
            }
        },
        new Race
        {
            Id = 999,
            RaceNameId = "sandbox_Blank",
            CompatibleCampaigns = new List<string> { "Sandbox" },
            Name = "Blank",
            Description = "Nothin",
            FeatureName = "Nothin",
            FeatureDescription = "Nothing",
        },
        // Add more races here until SQL database integration
    };

    public RaceDataService(ICustomRaceStorageService customRaceStorage)
    {
        _customRaceStorage = customRaceStorage;
    }

    public async Task<List<Race>> GetRacesAsync()
    {
        // For backwards compatibility, return only foundation races
        // This keeps existing code working without changes
        return await Task.FromResult(_races);
    }

    public async Task<Race?> GetRaceByIdAsync(int id)
    {
        // For backwards compatibility, check only foundation races
        var race = _races.FirstOrDefault(r => r.Id == id);
        return await Task.FromResult(race);
    }

    public async Task<List<Race>> GetAllRacesAsync()
    {
        var customRaces = await _customRaceStorage.LoadCustomRacesAsync();
        var allRaces = new List<Race>(_races);
        allRaces.AddRange(customRaces);
        return allRaces;
    }

    public async Task<Race?> GetRaceByIdCombinedAsync(int id)
    {
        // Check foundation races first
        var race = _races.FirstOrDefault(r => r.Id == id);
        if (race != null) return race;

        // Check custom races
        var customRaces = await _customRaceStorage.LoadCustomRacesAsync();
        return customRaces.FirstOrDefault(r => r.Id == id);
    }

}