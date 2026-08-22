using System;
using System.Collections.Generic;

namespace CharManJur.Models;

public class CharacterSaveData
{
    public string FileName { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public Guid PlayerId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public DateTime LastSaved { get; set; }
    public bool IsComplete { get; set; }
    public bool IsSaved { get; set; }
    public string CampaignType { get; set; } = string.Empty;
    public string CurrentPage { get; set; } = string.Empty;

    public CharacterData Data { get; set; } = new();
}

public class CharacterData
{
    public string CurrentPage { get; set; } = string.Empty;

    // Campaign Data
    public string CampaignType { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public Guid PlayerId { get; set; }   // NEW
    public string CharacterName { get; set; } = string.Empty;

    // ===== RACE DATA =====
    public string CharacterRace { get; set; } = string.Empty;
    public string CharacterRaceDescription { get; set; } = string.Empty;
    public string CharacterRaceFeatureName { get; set; } = string.Empty;
    public string CharacterRaceFeatureDescription { get; set; } = string.Empty;
    public int? SelectedRaceId { get; set; }

    // ===== RACE BONUSES =====
    public int RaceVigorBonus { get; set; }
    public int RaceAgilityBonus { get; set; }
    public int RaceMindBonus { get; set; }
    public int RaceSpiritBonus { get; set; }
    public List<RaceSkillBonus> RaceSkillBonuses { get; set; } = new();

    // ===== PREHENSILE LIMBS =====
    public List<PrehensileLimbSet> LimbSets { get; set; } = new();

    // Class Data
    public string CharacterClassName { get; set; } = string.Empty;
    public string CharacterClassDescription { get; set; } = string.Empty;
    public string CharacterClassFeatureName { get; set; } = string.Empty;
    public string CharacterClassFeatureDescription { get; set; } = string.Empty;
    public string LevelUpAllocationRequirement { get; set; } = string.Empty;  // Renamed from CharacterLevelingSkillBonus
    public string CharacterRecurringBenefit { get; set; } = string.Empty;

    // Class & Feature IDs
    public int? SelectedClassId { get; set; }
    public List<int> AcquiredFeatureIds { get; set; } = new();

    // Ability Scores
    public int? StatVigor { get; set; }
    public int? StatAgility { get; set; }
    public int? StatMind { get; set; }
    public int? StatSpirit { get; set; }
    public int? Hitpoints { get; set; }
    public int ASMStatVigor { get; set; }
    public int ASMStatAgility { get; set; }
    public int ASMStatMind { get; set; }
    public int ASMStatSpirit { get; set; }

    // Background Bonuses (Container for ALL creation bonuses)
    public int BackgroundVigorBonus { get; set; }
    public int BackgroundAgilityBonus { get; set; }
    public int BackgroundMindBonus { get; set; }
    public int BackgroundSpiritBonus { get; set; }
    public List<BGSkillBonuses> BackgroundSkillBonuses { get; set; } = new();

    // Hinderance Effects
    public int? HinderanceVigorPenalty { get; set; }
    public int? HinderanceAgilityPenalty { get; set; }
    public int? HinderanceMindPenalty { get; set; }
    public int? HinderanceSpiritPenalty { get; set; }

    // Sub-Features
    public List<int> AcquiredBlueprintIds { get; set; } = new();
    public List<int> AcquiredQuipIds { get; set; } = new();
    public List<int> AcquiredSpellIds { get; set; } = new();
    public List<int> AcquiredTechniqueIds { get; set; } = new();

    // Full Sub-Feature Objects
    public List<Blueprint> AcquiredBlueprints { get; set; } = new();
    public List<Quip> AcquiredQuips { get; set; } = new();
    public List<Spell> AcquiredSpells { get; set; } = new();
    public List<Technique> AcquiredTechniques { get; set; } = new();

    // ===== BACKGROUND DATA =====
    public int? SelectedBackgroundId { get; set; }
    public string SelectedBackgroundName { get; set; } = string.Empty;
    public string SelectedBackgroundDescription { get; set; } = string.Empty;
    public List<StartingItem> SelectedStartingItems { get; set; } = new();
    public List<ItemChoice> SelectedItemChoices { get; set; } = new();

    // ===== LANGUAGES =====
    public List<Language> SelectedLanguages { get; set; } = new();

    // ===== FAMILIARS =====
    public List<Familiar> AcquiredFamiliars { get; set; } = new();

    // ===== TRAINING POINTS =====
    public Dictionary<string, int> SkillTrainingLevels { get; set; } = new();

    // ===== HINDERANCE DATA =====
    public Hinderance? SelectedHinderance { get; set; }
    public HinderanceRewardType SelectedRewardType { get; set; }
    public string? SelectedRewardSkillName { get; set; }
    public int RewardStatBonusAmount { get; set; }
    public string? RewardStatName { get; set; }

    // ===== TOTAL BONUSES =====
    public int TotalVigorBonus { get; set; }
    public int TotalAgilityBonus { get; set; }
    public int TotalMindBonus { get; set; }
    public int TotalSpiritBonus { get; set; }
    public int TotalTrainingPointsBonus { get; set; }

    // ===== INVENTORY =====
    public List<CharacterItem> Inventory { get; set; } = new();
}