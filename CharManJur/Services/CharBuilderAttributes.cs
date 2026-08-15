using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CharManJur.Services;

// Step 1A: Define the interface (the contract)
public interface ICharAttribDataService
{

    Task<List<CharacterItem>> GetRecoverableItemsAsync();
    Task<bool> RecoverItemAsync(Guid instanceId);

    // === HINDERANCE DATA ===
    Hinderance? SelectedHinderance { get; set; }
    bool HasSelectedHinderance { get; }
    string SelectedHinderanceName { get; }
    string SelectedHinderanceDescription { get; }

    // === HINDERANCE REWARD ===
    HinderanceRewardType SelectedRewardType { get; set; }
    string? SelectedRewardSkillName { get; set; }
    int RewardStatBonusAmount { get; set; }
    string? RewardStatName { get; set; }

    // === HINDERANCE PENALTIES ===
    int HinderanceVigorPenalty { get; set; }
    int HinderanceAgilityPenalty { get; set; }
    int HinderanceMindPenalty { get; set; }
    int HinderanceSpiritPenalty { get; set; }

    // === HINDERANCE METHODS ===
    void ApplyHinderanceReward();

    // === BACKGROUND DATA ===
    string SelectedBackgroundName { get; set; }
    string SelectedBackgroundDescription { get; set; }

    // Legacy properties (kept for compatibility)
    int BGVigorBonus { get; set; }
    int BGAgilityBonus { get; set; }
    int BGMindBonus { get; set; }
    int BGSpiritBonus { get; set; }

    // New background bonus properties
    int BackgroundVigorBonus { get; set; }
    int BackgroundAgilityBonus { get; set; }
    int BackgroundMindBonus { get; set; }
    int BackgroundSpiritBonus { get; set; }

    List<StartingItem> SelectedStartingItems { get; set; }
    List<BGSkillBonuses> SelectedSkillBonuses { get; set; }
    List<BGSkillBonuses> BackgroundSkillBonuses { get; set; }
    List<ItemChoice> SelectedItemChoices { get; set; }

    // === FAMILIARS ===
    List<Familiar> AcquiredFamiliars { get; set; }
    void AddFamiliar(Familiar familiar);
    void RemoveFamiliar(int familiarId);
    bool HasFamiliar(int familiarId);
    void ClearFamiliars();

    // === PAGE TRACKING ===
    string CurrentPage { get; set; }
    void SetCurrentPage(string pageRoute);

    // === TRAINING POINTS ===
    int AvailableTrainingPoints { get; set; }
    Dictionary<string, int> SkillTrainingLevels { get; set; }

    // === STAT-SKILL MAPPING ===
    int GetAbilityModifier(int score);
    int GetSkillTotal(string skillName);

    // === TOTAL STATS (Base + Bonuses + Penalties) ===
    int TotalStatVigor { get; }
    int TotalStatAgility { get; }
    int TotalStatMind { get; }
    int TotalStatSpirit { get; }

    // === TOTAL ASM (Modifier from Total Stats) ===
    int TotalASMStatVigor { get; }
    int TotalASMStatAgility { get; }
    int TotalASMStatMind { get; }
    int TotalASMStatSpirit { get; }

    // === SUB-FEATURE MANAGEMENT (Full Objects) ===
    void AddQuip(Quip quip);
    void RemoveQuip(int quipId);
    bool HasQuip(int quipId);

    void AddSpell(Spell spell);
    void RemoveSpell(int spellId);
    bool HasSpell(int spellId);

    void AddTechnique(Technique technique);
    void RemoveTechnique(int techniqueId);
    bool HasTechnique(int techniqueId);

    void AddBlueprint(Blueprint blueprint);
    void RemoveBlueprint(int blueprintId);
    bool HasBlueprint(int blueprintId);

    // === CHARACTER DATA (Strings) ===
    string CampaignType { get; set; }
    Guid PlayerId { get; set; }
    string PlayerName { get; set; }
    string CharacterName { get; set; }

    // === RACE DATA ===
    string CharacterRace { get; set; }
    string CharacterRaceDescription { get; set; }
    string CharacterRaceFeatureName { get; set; }
    string CharacterRaceFeatureDescription { get; set; }
    int? SelectedRaceId { get; set; }

    // === PREHENSILE LIMBS ===
    List<PrehensileLimbSet> LimbSets { get; set; }
    int GetTotalLimbSlots();
    PrehensileLimbSet? GetLimbSetById(int id);
    List<PrehensileLimbSet> GetLimbSetsByType(LimbPairType pairType);
    int GetUsedHandSlots();

    // === TWO-HANDED ITEM MANAGEMENT ===
    bool CanEquipTwoHandedItem(int characterItemId);
    void EquipTwoHandedItem(int characterItemId, int limbSetId);
    void UnequipTwoHandedItem(int characterItemId);
    PrehensileLimbSet? GetLimbSetForTwoHandedItem(int characterItemId);
    bool IsLimbSetOccupiedByTwoHandedItem(int limbSetId);

    // === HOT-SWAP METHODS ===
    bool CanSwapToBelt(int characterItemId);
    bool CanSwapToHand(int characterItemId);
    void SwapToBelt(int characterItemId);
    void SwapToHand(int characterItemId);
    int? FindAvailableBeltSlot(int slotsNeeded = 1);
    int? FindAvailableHandSlot(int slotsNeeded = 1);

    // === RACE BONUSES ===
    int RaceVigorBonus { get; set; }
    int RaceAgilityBonus { get; set; }
    int RaceMindBonus { get; set; }
    int RaceSpiritBonus { get; set; }
    List<RaceSkillBonus> RaceSkillBonuses { get; set; }
    string CharacterClassName { get; set; }
    string? CharacterClassDescription { get; set; }
    string CharacterClassFeatureName { get; set; }
    string CharacterClassFeatureDescription { get; set; }
    string LevelUpAllocationRequirement { get; set; }
    string CharacterRecurringBenefit { get; set; }

    // === LANGUAGES ===
    List<Language> SelectedLanguages { get; set; }
    void AddLanguage(Language language);
    void RemoveLanguage(int languageId);
    bool HasLanguage(int languageId);
    void ClearLanguages();

    // === CLASS & FEATURE IDs ===
    int? SelectedClassId { get; set; }
    List<int> AcquiredFeatureIds { get; set; }

    // === SUB-FEATURE IDs ===
    List<int> AcquiredBlueprintIds { get; set; }
    List<int> AcquiredQuipIds { get; set; }
    List<int> AcquiredSpellIds { get; set; }
    List<int> AcquiredTechniqueIds { get; set; }

    // === ACQUIRED SUB-FEATURE LISTS (Full Objects) ===
    List<Quip> AcquiredQuips { get; set; }
    List<Spell> AcquiredSpells { get; set; }
    List<Technique> AcquiredTechniques { get; set; }
    List<Blueprint> AcquiredBlueprints { get; set; }

    // === ABILITY SCORES ===
    int? StatVigor { get; set; }
    int? StatAgility { get; set; }
    int? StatMind { get; set; }
    int? StatSpirit { get; set; }
    int Hitpoints { get; }
    int ASMStatVigor { get; set; }
    int ASMStatAgility { get; set; }
    int ASMStatMind { get; set; }
    int ASMStatSpirit { get; set; }

    // === LISTS ===
    List<string> CampaignTypes { get; set; }
    List<string> CharacterClasses { get; set; }

    // === LOOKUP METHODS ===
    CharacterClass? GetSelectedClass();
    List<ClassFeature> GetAcquiredFeatures();
    List<Blueprint> GetAcquiredBlueprints();
    List<Quip> GetAcquiredQuips();
    List<Spell> GetAcquiredSpells();
    List<Technique> GetAcquiredTechniques();

    // === FEATURE MANAGEMENT ===
    void AddFeature(int featureId);
    void RemoveFeature(int featureId);
    bool HasFeature(int featureId);
    void ClearFeatures();

    // === SUB-FEATURE ID MANAGEMENT ===
    void AddBlueprintById(int blueprintId);
    void AddQuipById(int quipId);
    void AddSpellById(int spellId);
    void AddTechniqueById(int techniqueId);

    // === CLEAR ===
    void ClearCharacterCreationData();

    // === CHARACTER PERSISTENCE ===
    bool IsCharacterComplete { get; set; }
    bool IsCharacterSaved { get; set; }
    DateTime? LastSavedDate { get; set; }
    string? SaveFileName { get; set; }

    void MarkCharacterComplete();
    void MarkCharacterIncomplete();
    void MarkCharacterSaved();
    void MarkCharacterUnsaved();

    // === CHARACTER SAVE/LOAD ===
    void PopulateFromSaveData(CharacterSaveData saveData);
    CharacterSaveData CreateSaveData();

    // ====================================== LIVE GAME SERVICES ====================================== //

    // === INVENTORY SYSTEM ===
    List<CharacterItem> Inventory { get; set; }
    List<CharacterItem> EquippedItems { get; }
    List<CharacterItem> DroppedItems { get; }

    CharacterItem? GetEquippedHandSlot(int slot);
    CharacterItem? GetEquippedBeltSlot(int slot);
    CharacterItem? GetEquippedArmor();

    CharacterItem AddItemToInventory(int templateId, int quantity = 1);
    CharacterItem AddItemToInventory(Item template, int quantity = 1);
    void RemoveItemFromInventory(int characterItemId);
    void UpdateItemUses(int characterItemId, int usesRemaining);
    void EquipItem(int characterItemId, int slot);
    void EquipItemAsArmor(int characterItemId);
    void UnequipItem(int characterItemId);
    void DropItem(int characterItemId);
    void RestoreDroppedItem(int characterItemId);
    void TransferItemToPlayer(int characterItemId, int targetCharacterId);

    // === CONVERT STARTING ITEMS TO INVENTORY ===
    void InitializeInventoryFromStartingItems();


    // ====================================== END OF LIVE GAME SERVICES ====================================== //
}

// Step 1B: Implement the interface (the actual class)


public class CharAttribDataService : ICharAttribDataService
{
    // === HINDERANCE DATA ===
    private Hinderance? _selectedHinderance;
    public Hinderance? SelectedHinderance
    {
        get => _selectedHinderance;
        set
        {
            _selectedHinderance = value;
            // ===== FIX: Only clear rewards if NOT saving =====
            if (!_isSaving && value == null)
            {
                SelectedRewardType = HinderanceRewardType.None;
                SelectedRewardSkillName = null;
                RewardStatBonusAmount = 0;
                RewardStatName = null;
            }
        }
    }

    private bool _isSaving = false;
    public bool HasSelectedHinderance => SelectedHinderance != null;
    public string SelectedHinderanceName => SelectedHinderance?.Name ?? "No Hinderance Selected";
    public string SelectedHinderanceDescription => SelectedHinderance?.Description ?? string.Empty;

    // === HINDERANCE REWARD ===
    public HinderanceRewardType SelectedRewardType { get; set; } = HinderanceRewardType.None;
    public string? SelectedRewardSkillName { get; set; }
    public int RewardStatBonusAmount { get; set; } = 1;
    public string? RewardStatName { get; set; }

    // === HINDERANCE METHODS ===
    public void ApplyHinderanceReward()
{
    if (SelectedHinderance == null) return;

    if (SelectedRewardType == HinderanceRewardType.StatBonus && !string.IsNullOrEmpty(RewardStatName))
    {
        // The stat bonus is applied through TotalStat calculations
        // The reward amount is tracked in RewardStatBonusAmount and RewardStatName
        System.Diagnostics.Debug.WriteLine($"=== Applied Hinderance Stat Bonus: +1 {RewardStatName} ===");
    }
    else if (SelectedRewardType == HinderanceRewardType.TrainingPoint)
    {
        // Training point is added to AvailableTrainingPoints
        // This is handled in the ViewModel now
        System.Diagnostics.Debug.WriteLine($"=== Applied Hinderance Training Point ===");
    }
}

    // === BACKGROUND BONUSES ===
    private int _bgVigorBonus;
    private int _bgAgilityBonus;
    private int _bgMindBonus;
    private int _bgSpiritBonus;
    public int HinderanceVigorPenalty { get; set; } = 0;
    public int HinderanceAgilityPenalty { get; set; } = 0;
    public int HinderanceMindPenalty { get; set; } = 0;
    public int HinderanceSpiritPenalty { get; set; } = 0;

    public int BGVigorBonus
    {
        get => _bgVigorBonus;
        set => _bgVigorBonus = value;
    }

    public int BGAgilityBonus
    {
        get => _bgAgilityBonus;
        set => _bgAgilityBonus = value;
    }

    public int BGMindBonus
    {
        get => _bgMindBonus;
        set => _bgMindBonus = value;
    }

    public int BGSpiritBonus
    {
        get => _bgSpiritBonus;
        set => _bgSpiritBonus = value;
    }

    public int BackgroundVigorBonus
    {
        get => BGVigorBonus;
        set => BGVigorBonus = value;
    }

    public int BackgroundAgilityBonus
    {
        get => BGAgilityBonus;
        set => BGAgilityBonus = value;
    }

    public int BackgroundMindBonus
    {
        get => BGMindBonus;
        set => BGMindBonus = value;
    }

    public int BackgroundSpiritBonus
    {
        get => BGSpiritBonus;
        set => BGSpiritBonus = value;
    }

    public List<BGSkillBonuses> BackgroundSkillBonuses
    {
        get => SelectedSkillBonuses;
        set => SelectedSkillBonuses = value ?? new List<BGSkillBonuses>();
    }

    public void MigrateTrainingPointsIfNeeded()
    {
        bool needsMigration = false;
        foreach (var kvp in SkillTrainingLevels)
        {
            if (kvp.Value < 0)
            {
                needsMigration = true;
                break;
            }
        }

        if (needsMigration)
        {
            System.Diagnostics.Debug.WriteLine("=== Migrating old training points format ===");
            var keys = SkillTrainingLevels.Keys.ToList();
            foreach (var key in keys)
            {
                int oldValue = SkillTrainingLevels[key];
                if (oldValue < 0)
                {
                    // Old format: -2 + training points = oldValue
                    // So training points = oldValue + 2
                    int trainingPoints = oldValue + 2;
                    SkillTrainingLevels[key] = trainingPoints > 0 ? trainingPoints : 0;
                    System.Diagnostics.Debug.WriteLine($"  {key}: {oldValue} -> {SkillTrainingLevels[key]}");
                }
            }
        }
    }

    // === FAMILIARS ===
    private List<Familiar> _acquiredFamiliars = new();
    // === FAMILIAR INVENTORY ===
    public void AddItemToFamiliarInventory(int familiarId, Item template, int quantity = 1)
    {
        var familiar = _acquiredFamiliars.FirstOrDefault(f => f.Id == familiarId);
        if (familiar != null)
        {
            familiar.AddItemToInventory(template, quantity);
        }
    }

    public void RemoveItemFromFamiliarInventory(int familiarId, int characterItemId)
    {
        var familiar = _acquiredFamiliars.FirstOrDefault(f => f.Id == familiarId);
        if (familiar != null)
        {
            familiar.RemoveItemFromInventory(characterItemId);
        }
    }

    public List<CharacterItem> GetFamiliarInventory(int familiarId)
    {
        var familiar = _acquiredFamiliars.FirstOrDefault(f => f.Id == familiarId);
        return familiar?.Inventory ?? new List<CharacterItem>();
    }

    public int GetFamiliarInventorySlotsUsed(int familiarId)
    {
        var familiar = _acquiredFamiliars.FirstOrDefault(f => f.Id == familiarId);
        return familiar?.InventorySlotsUsed ?? 0;
    }

    public int GetFamiliarInventorySlotsTotal(int familiarId)
    {
        var familiar = _acquiredFamiliars.FirstOrDefault(f => f.Id == familiarId);
        return familiar?.InventorySlotsTotal ?? 4;
    }

    public bool IsFamiliarEncumbered(int familiarId)
    {
        var familiar = _acquiredFamiliars.FirstOrDefault(f => f.Id == familiarId);
        return familiar?.IsEncumbered ?? false;
    }

    public List<Familiar> AcquiredFamiliars
    {
        get => _acquiredFamiliars;
        set => _acquiredFamiliars = value ?? new List<Familiar>();
    }

    public void AddFamiliar(Familiar familiar)
    {
        if (familiar != null && !HasFamiliar(familiar.Id))
        {
            _acquiredFamiliars.Add(familiar);
        }
    }

    public void RemoveFamiliar(int familiarId)
    {
        var familiar = _acquiredFamiliars.FirstOrDefault(f => f.Id == familiarId);
        if (familiar != null) _acquiredFamiliars.Remove(familiar);
    }

    public bool HasFamiliar(int familiarId)
    {
        return _acquiredFamiliars.Any(f => f.Id == familiarId);
    }

    public void ClearFamiliars()
    {
        _acquiredFamiliars.Clear();
    }

    // === BACKGROUND DATA ===
    public string SelectedBackgroundName { get; set; } = string.Empty;
    public string SelectedBackgroundDescription { get; set; } = string.Empty;
    public List<StartingItem> SelectedStartingItems { get; set; } = new();
    public List<BGSkillBonuses> SelectedSkillBonuses { get; set; } = new();
    public List<ItemChoice> SelectedItemChoices { get; set; } = new();

    private List<Language> _selectedLanguages = new();

    public List<Language> SelectedLanguages
    {
        get => _selectedLanguages;
        set => _selectedLanguages = value ?? new List<Language>();
    }

    // === METHODS ===
    public void AddLanguage(Language language)
    {
        if (language != null && !HasLanguage(language.Id))
        {
            _selectedLanguages.Add(language);
        }
    }

    public void RemoveLanguage(int languageId)
    {
        var language = _selectedLanguages.FirstOrDefault(l => l.Id == languageId);
        if (language != null)
        {
            _selectedLanguages.Remove(language);
        }
    }

    public bool HasLanguage(int languageId)
    {
        return _selectedLanguages.Any(l => l.Id == languageId);
    }

    public void ClearLanguages()
    {
        _selectedLanguages.Clear();
    }

    // === PAGE TRACKING ===
    public string CurrentPage { get; set; } = string.Empty;

    public void SetCurrentPage(string pageRoute)
    {
        CurrentPage = pageRoute;
    }

    // === TRAINING POINTS ===
    public int AvailableTrainingPoints { get; set; } = 4;
    public Dictionary<string, int> SkillTrainingLevels { get; set; } = new()
    {
        // Initialize all skills with 0 training points (base -2 is applied separately)
        { "Athletics", 0 },
        { "Acrobatics", 0 },
        { "Aim", 0 },
        { "Arcana", 0 },
        { "Artifice", 0 },
        { "Commune", 0 },
        { "Constitution", 0 },
        { "Deception", 0 },
        { "Diplomacy", 0 },
        { "Drive", 0 },
        { "Grapple", 0 },
        { "Heal", 0 },
        { "Investigate", 0 },
        { "Lore", 0 },
        { "Sight", 0 },
        { "Presence", 0 },
        { "Ride", 0 },
        { "Stealth", 0 },
        { "Survival", 0 },
        { "Thief", 0 }
    };

    // === TOTAL STATS (Base + Bonuses + Penalties + Rewards) ===
    public int TotalStatVigor
    {
        get
        {
            int baseStat = StatVigor ?? 10;

            int bgModifier = GetBackgroundModifier("Vigor");
            int hindModifier = GetHinderanceModifier("Vigor");
            int hindReward = (RewardStatName == "Vigor") ? RewardStatBonusAmount : 0;
            int classModifier = 0;
            int raceModifier = GetRaceModifier("Vigor");

            return baseStat + bgModifier + hindModifier + hindReward + classModifier + raceModifier;
        }
    }

    public int TotalStatAgility
    {
        get
        {
            int baseStat = StatAgility ?? 10;

            int bgModifier = GetBackgroundModifier("Agility");
            int hindModifier = GetHinderanceModifier("Agility");
            int hindReward = (RewardStatName == "Agility") ? RewardStatBonusAmount : 0;
            int classModifier = 0;
            int raceModifier = GetRaceModifier("Agility");

            return baseStat + bgModifier + hindModifier + hindReward + classModifier + raceModifier;
        }
    }

    public int TotalStatMind
    {
        get
        {
            int baseStat = StatMind ?? 10;

            int bgModifier = GetBackgroundModifier("Mind");
            int hindModifier = GetHinderanceModifier("Mind");
            int hindReward = (RewardStatName == "Mind") ? RewardStatBonusAmount : 0;
            int classModifier = 0;
            int raceModifier = GetRaceModifier("Mind");

            return baseStat + bgModifier + hindModifier + hindReward + classModifier + raceModifier;
        }
    }

    public int TotalStatSpirit
    {
        get
        {
            int baseStat = StatSpirit ?? 10;

            int bgModifier = GetBackgroundModifier("Spirit");
            int hindModifier = GetHinderanceModifier("Spirit");
            int hindReward = (RewardStatName == "Spirit") ? RewardStatBonusAmount : 0;
            int classModifier = 0;
            int raceModifier = GetRaceModifier("Spirit");

            return baseStat + bgModifier + hindModifier + hindReward + classModifier + raceModifier;
        }
    }

    private int GetRaceModifier(string statName)
    {
        if (SelectedRaceId == null) return 0;

        return statName.ToLower() switch
        {
            "vigor" => RaceVigorBonus,
            "agility" => RaceAgilityBonus,
            "mind" => RaceMindBonus,
            "spirit" => RaceSpiritBonus,
            _ => 0
        };
    }

    private int GetBackgroundModifier(string statName)
    {
        if (SelectedBackgroundName == null) return 0;

        // This should be stored when background is selected
        // For now, use the legacy properties
        return statName.ToLower() switch
        {
            "vigor" => BGVigorBonus,
            "agility" => BGAgilityBonus,
            "mind" => BGMindBonus,
            "spirit" => BGSpiritBonus,
            _ => 0
        };
    }

    private int GetHinderanceModifier(string statName)
    {
        if (SelectedHinderance == null) return 0;

        return SelectedHinderance.GetAbilityModifier(statName);
    }

    // === TOTAL ASM (Modifier from Total Stats) ===
    public int TotalASMStatVigor => GetAbilityModifier(TotalStatVigor);
    public int TotalASMStatAgility => GetAbilityModifier(TotalStatAgility);
    public int TotalASMStatMind => GetAbilityModifier(TotalStatMind);
    public int TotalASMStatSpirit => GetAbilityModifier(TotalStatSpirit);

    // === STAT-SKILL MAPPING ===
    public int GetSkillTotal(string skillName)
    {
        int trainingPoints = SkillTrainingLevels.ContainsKey(skillName) ? SkillTrainingLevels[skillName] : 0;
        int statBonus = GetStatBonusForSkill(skillName);
        int bgBonus = GetBackgroundSkillBonus(skillName);
        int raceBonus = GetRaceSkillBonus(skillName);
        int classBonus = 0;
        int basePenalty = -2;

        return basePenalty + trainingPoints + statBonus + bgBonus + raceBonus + classBonus;
    }

    private int GetRaceSkillBonus(string skillName)
    {
        if (RaceSkillBonuses == null) return 0;
        var bonus = RaceSkillBonuses.FirstOrDefault(b => b.SkillName == skillName);
        return bonus?.Bonus ?? 0;
    }

    private int GetBackgroundSkillBonus(string skillName)
    {
        if (SelectedSkillBonuses == null) return 0;
        var bonus = SelectedSkillBonuses.FirstOrDefault(b => b.SkillName == skillName);
        return bonus?.Bonus ?? 0;
    }

    private int GetStatBonusForSkill(string skillName)
    {
        return skillName switch
        {
            // Vigor skills
            "Athletics" => TotalASMStatVigor,
            "Constitution" => TotalASMStatVigor,
            "Grapple" => TotalASMStatVigor,
            "Presence" => TotalASMStatVigor,
            "Ride" => TotalASMStatVigor,

            // Agility skills
            "Acrobatics" => TotalASMStatAgility,
            "Aim" => TotalASMStatAgility,
            "Drive" => TotalASMStatAgility,
            "Stealth" => TotalASMStatAgility,
            "Thief" => TotalASMStatAgility,

            // Mind skills
            "Arcana" => TotalASMStatMind,
            "Artifice" => TotalASMStatMind,
            "Heal" => TotalASMStatMind,
            "Investigate" => TotalASMStatMind,
            "Lore" => TotalASMStatMind,

            // Spirit skills
            "Commune" => TotalASMStatSpirit,
            "Deception" => TotalASMStatSpirit,
            "Diplomacy" => TotalASMStatSpirit,
            "Sight" => TotalASMStatSpirit,
            "Survival" => TotalASMStatSpirit,

            _ => 0
        };
    }

    // === PRIVATE FIELDS ===
    private readonly IClassDataService _classDataService;
    private readonly IItemDataService _itemDataService;
    private readonly IPlayerActionLogService _actionLogService;
    private readonly IItemRecoveryService _recoveryService;
    private List<Quip> _acquiredQuips = new();
    private List<Spell> _acquiredSpells = new();
    private List<Technique> _acquiredTechniques = new();
    private List<Blueprint> _acquiredBlueprints = new();

    // === INVENTORY ===
    private List<CharacterItem> _inventory = new();
    private List<CharacterItem> _droppedItems = new();
    private int _nextCharacterItemId = 1;

    public CharAttribDataService(IClassDataService classDataService, IPlayerActionLogService actionLogService, IItemDataService itemDataService, IItemRecoveryService recoveryService)
    {
        _classDataService = classDataService;
        _actionLogService = actionLogService;
        _recoveryService = recoveryService;
        _itemDataService = itemDataService;
    }

    public async Task<List<CharacterItem>> GetRecoverableItemsAsync()
    {
        return await _recoveryService.GetRecoverablesForPlayerAsync(PlayerId);
    }

    public async Task<bool> RecoverItemAsync(Guid instanceId)
    {
        int newId = GetNextCharacterItemId();
        var recovered = await _recoveryService.RecoverItemAsync(instanceId, newId);
        if (recovered == null) return false;

        _inventory.Add(recovered);
        return true;
    }

    // === RACE BONUSES ===
    private int _raceVigorBonus = 0;
    private int _raceAgilityBonus = 0;
    private int _raceMindBonus = 0;
    private int _raceSpiritBonus = 0;
    private List<RaceSkillBonus> _raceSkillBonuses = new();

    // === RACE DATA ===
    public int? SelectedRaceId { get; set; }

    public string CharacterRace { get; set; } = string.Empty;
    public string CharacterRaceDescription { get; set; } = string.Empty;
    public string CharacterRaceFeatureName { get; set; } = string.Empty;
    public string CharacterRaceFeatureDescription { get; set; } = string.Empty;

    public int RaceVigorBonus
    {
        get => _raceVigorBonus;
        set => _raceVigorBonus = value;
    }

    public int RaceAgilityBonus
    {
        get => _raceAgilityBonus;
        set => _raceAgilityBonus = value;
    }

    public int RaceMindBonus
    {
        get => _raceMindBonus;
        set => _raceMindBonus = value;
    }

    public int RaceSpiritBonus
    {
        get => _raceSpiritBonus;
        set => _raceSpiritBonus = value;
    }

    public List<RaceSkillBonus> RaceSkillBonuses
    {
        get => _raceSkillBonuses;
        set => _raceSkillBonuses = value ?? new List<RaceSkillBonus>();
    }

    // === PREHENSILE LIMBS ===
    private List<PrehensileLimbSet> _limbSets = new();

    public List<PrehensileLimbSet> LimbSets
    {
        get => _limbSets;
        set => _limbSets = value ?? new List<PrehensileLimbSet>();
    }

    public int GetTotalLimbSlots()
    {
        int total = 0;
        foreach (var set in _limbSets)
        {
            total += set.SlotCount;
        }
        return total;
    }

    public int GetUsedHandSlots()
    {
        int usedSlots = 0;
        for (int i = 1; i <= GetTotalLimbSlots(); i++)
        {
            var item = GetEquippedHandSlot(i);
            if (item != null)
            {
                usedSlots += item.SlotsRequired;
            }
        }
        return usedSlots;
    }

    public PrehensileLimbSet? GetLimbSetById(int id)
    {
        return _limbSets.FirstOrDefault(l => l.Id == id);
    }

    public List<PrehensileLimbSet> GetLimbSetsByType(LimbPairType pairType)
    {
        return _limbSets.Where(l => l.PairType == pairType).ToList();
    }

    // === STRING PROPERTIES ===
    public string CampaignType { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public Guid PlayerId { get; set; } = InstallIdentity.GetOrCreateInstallPlayerId();
    public string CharacterName { get; set; } = string.Empty;
    public string CharacterClassName { get; set; } = string.Empty;
    public string? CharacterClassDescription { get; set; } = string.Empty;
    public string CharacterClassFeatureName { get; set; } = string.Empty;
    public string CharacterClassFeatureDescription { get; set; } = string.Empty;
    public string LevelUpAllocationRequirement { get; set; } = string.Empty;
    public string CharacterRecurringBenefit { get; set; } = string.Empty;

    // === CLASS & FEATURE IDs ===
    public int? SelectedClassId { get; set; }
    public List<int> AcquiredFeatureIds { get; set; } = new();

    // === SUB-FEATURE IDs ===
    public List<int> AcquiredBlueprintIds { get; set; } = new();
    public List<int> AcquiredQuipIds { get; set; } = new();
    public List<int> AcquiredSpellIds { get; set; } = new();
    public List<int> AcquiredTechniqueIds { get; set; } = new();

    // === ABILITY SCORES ===
    public int? StatVigor { get; set; }
    public int? StatAgility { get; set; }
    public int? StatMind { get; set; }
    public int? StatSpirit { get; set; }

    // Godrick patch 08/07/2026: HP is now static — 1 base + Class's flat bonus + Vigor Bonus.
    // Never reduced by a negative Vigor Bonus.
    public int HitpointsAdjustment { get; set; } = 0;

    public int Hitpoints
    {
        get
        {
            int classBonus = GetSelectedClass()?.HitProtectionBonus ?? 0;
            int vigorContribution = Math.Max(TotalASMStatVigor, 0);
            return 1 + classBonus + vigorContribution + HitpointsAdjustment;
        }
    }

    public int ASMStatVigor { get; set; }
    public int ASMStatAgility { get; set; }
    public int ASMStatMind { get; set; }
    public int ASMStatSpirit { get; set; }

    // === LISTS ===
    public List<string> CampaignTypes { get; set; } = new List<string>
    {
        "Godrick",
        "Sandbox"
    };
    public List<string> CharacterClasses { get; set; } = new List<string>
    {
        "Artificer",
        "Rogue"
    };

    // === ACQUIRED SUB-FEATURE LISTS (Full Objects) ===
    public List<Quip> AcquiredQuips
    {
        get => _acquiredQuips;
        set => _acquiredQuips = value ?? new List<Quip>();
    }

    public List<Spell> AcquiredSpells
    {
        get => _acquiredSpells;
        set => _acquiredSpells = value ?? new List<Spell>();
    }

    public List<Technique> AcquiredTechniques
    {
        get => _acquiredTechniques;
        set => _acquiredTechniques = value ?? new List<Technique>();
    }

    public List<Blueprint> AcquiredBlueprints
    {
        get => _acquiredBlueprints;
        set => _acquiredBlueprints = value ?? new List<Blueprint>();
    }

    // === CHARACTER PERSISTENCE ===
    public bool IsCharacterComplete { get; set; } = false;
    public bool IsCharacterSaved { get; set; } = false;
    public DateTime? LastSavedDate { get; set; } = null;
    public string? SaveFileName { get; set; } = null;

    public void MarkCharacterComplete()
    {
        IsCharacterComplete = true;
    }

    public void MarkCharacterIncomplete()
    {
        IsCharacterComplete = false;
    }

    public void MarkCharacterSaved()
    {
        IsCharacterSaved = true;
        LastSavedDate = DateTime.Now;
    }

    public void MarkCharacterUnsaved()
    {
        IsCharacterSaved = false;
    }

    // === SUB-FEATURE MANAGEMENT (Full Objects) ===

    public void AddQuip(Quip quip)
    {
        if (!HasQuip(quip.Id))
        {
            _acquiredQuips.Add(quip);
            if (!AcquiredQuipIds.Contains(quip.Id))
                AcquiredQuipIds.Add(quip.Id);
        }
    }

    public void RemoveQuip(int quipId)
    {
        var item = _acquiredQuips.FirstOrDefault(q => q.Id == quipId);
        if (item != null) _acquiredQuips.Remove(item);
        AcquiredQuipIds.Remove(quipId);
    }

    public bool HasQuip(int quipId)
    {
        return _acquiredQuips.Any(q => q.Id == quipId);
    }

    public void AddSpell(Spell spell)
    {
        if (!HasSpell(spell.Id))
        {
            _acquiredSpells.Add(spell);
            if (!AcquiredSpellIds.Contains(spell.Id))
                AcquiredSpellIds.Add(spell.Id);
        }
    }

    public void RemoveSpell(int spellId)
    {
        var item = _acquiredSpells.FirstOrDefault(s => s.Id == spellId);
        if (item != null) _acquiredSpells.Remove(item);
        AcquiredSpellIds.Remove(spellId);
    }

    public bool HasSpell(int spellId)
    {
        return _acquiredSpells.Any(s => s.Id == spellId);
    }

    public void AddTechnique(Technique technique)
    {
        if (!HasTechnique(technique.Id))
        {
            _acquiredTechniques.Add(technique);
            if (!AcquiredTechniqueIds.Contains(technique.Id))
                AcquiredTechniqueIds.Add(technique.Id);
        }
    }

    public void RemoveTechnique(int techniqueId)
    {
        var item = _acquiredTechniques.FirstOrDefault(t => t.Id == techniqueId);
        if (item != null) _acquiredTechniques.Remove(item);
        AcquiredTechniqueIds.Remove(techniqueId);
    }

    public bool HasTechnique(int techniqueId)
    {
        return _acquiredTechniques.Any(t => t.Id == techniqueId);
    }

    public void AddBlueprint(Blueprint blueprint)
    {
        if (!HasBlueprint(blueprint.Id))
        {
            _acquiredBlueprints.Add(blueprint);
            if (!AcquiredBlueprintIds.Contains(blueprint.Id))
                AcquiredBlueprintIds.Add(blueprint.Id);
        }
    }

    public void RemoveBlueprint(int blueprintId)
    {
        var item = _acquiredBlueprints.FirstOrDefault(b => b.Id == blueprintId);
        if (item != null) _acquiredBlueprints.Remove(item);
        AcquiredBlueprintIds.Remove(blueprintId);
    }

    public bool HasBlueprint(int blueprintId)
    {
        return _acquiredBlueprints.Any(b => b.Id == blueprintId);
    }

    // === SUB-FEATURE ID MANAGEMENT ===

    public void AddBlueprintById(int blueprintId)
    {
        if (!AcquiredBlueprintIds.Contains(blueprintId))
            AcquiredBlueprintIds.Add(blueprintId);
    }

    public void AddQuipById(int quipId)
    {
        if (!AcquiredQuipIds.Contains(quipId))
            AcquiredQuipIds.Add(quipId);
    }

    public void AddSpellById(int spellId)
    {
        if (!AcquiredSpellIds.Contains(spellId))
            AcquiredSpellIds.Add(spellId);
    }

    public void AddTechniqueById(int techniqueId)
    {
        if (!AcquiredTechniqueIds.Contains(techniqueId))
            AcquiredTechniqueIds.Add(techniqueId);
    }

    // === FEATURE MANAGEMENT ===

    public void AddFeature(int featureId)
    {
        if (!AcquiredFeatureIds.Contains(featureId))
            AcquiredFeatureIds.Add(featureId);
    }

    public void RemoveFeature(int featureId)
    {
        AcquiredFeatureIds.Remove(featureId);
    }

    public bool HasFeature(int featureId)
    {
        return AcquiredFeatureIds.Contains(featureId);
    }

    public void ClearFeatures()
    {
        AcquiredFeatureIds.Clear();
    }

    // === LOOKUP METHODS ===

    public CharacterClass? GetSelectedClass()
    {
        if (SelectedClassId == null) return null;
        return _classDataService.GetClassByIdAsync(SelectedClassId.Value).Result;
    }

    public List<ClassFeature> GetAcquiredFeatures()
    {
        var classData = GetSelectedClass();
        if (classData == null) return new List<ClassFeature>();

        return classData.Features
            .Where(f => AcquiredFeatureIds.Contains(f.Id))
            .ToList();
    }

    public List<Blueprint> GetAcquiredBlueprints()
    {
        return _acquiredBlueprints;
    }

    public List<Quip> GetAcquiredQuips()
    {
        return _acquiredQuips;
    }

    public List<Spell> GetAcquiredSpells()
    {
        return _acquiredSpells;
    }

    public List<Technique> GetAcquiredTechniques()
    {
        return _acquiredTechniques;
    }

    // ====================================== INVENTORY SYSTEM ====================================== //

    public List<CharacterItem> Inventory
    {
        get => _inventory;
        set => _inventory = value ?? new List<CharacterItem>();
    }

    public List<CharacterItem> EquippedItems => _inventory.Where(i => i.IsEquipped).ToList();

    public List<CharacterItem> DroppedItems => _droppedItems;

    public CharacterItem? GetEquippedHandSlot(int slot)
    {
        return _inventory.FirstOrDefault(i => i.IsEquipped && i.EquipmentSlot == EquipmentSlotType.Hand && i.SlotIndex == slot);
    }

    public CharacterItem? GetEquippedBeltSlot(int slot)
    {
        return _inventory.FirstOrDefault(i => i.IsEquipped && i.EquipmentSlot == EquipmentSlotType.Belt && i.SlotIndex == slot);
    }

    public CharacterItem? GetEquippedArmor()
    {
        return _inventory.FirstOrDefault(i => i.IsEquipped && i.EquipmentSlot == EquipmentSlotType.Armor);
    }

    public CharacterItem AddItemToInventory(int templateId, int quantity = 1)
    {
        var existingStack = _inventory.FirstOrDefault(i =>
            i.TemplateId == templateId &&
            !i.IsEquipped &&
            !i.IsEmpty &&
            i.Quantity < 99);

        if (existingStack != null)
        {
            existingStack.Quantity += quantity;
            existingStack.LastModified = DateTime.Now;
            return existingStack;
        }

        var newItem = new CharacterItem
        {
            Id = GetNextCharacterItemId(),
            TemplateId = templateId,
            Quantity = quantity,
            RemainingUses = 0,
            AcquiredAt = DateTime.Now
        };

        _inventory.Add(newItem);
        return newItem;
    }

    public CharacterItem AddItemToInventory(Item template, int quantity = 1)
    {
        if (template == null) return null;

        if (template.IsStackableItem)
        {
            var existingStack = _inventory.FirstOrDefault(i =>
                i.TemplateId == template.Id &&
                !i.IsEquipped &&
                !i.IsEmpty &&
                i.Quantity < 99);

            if (existingStack != null)
            {
                existingStack.Quantity += quantity;
                existingStack.LastModified = DateTime.Now;
                return existingStack;
            }
        }

        int newId = GetNextCharacterItemId();

        int remainingUses = 0;
        if (template.HasUses)
        {
            remainingUses = template.Uses.Value;
        }

        var newItem = new CharacterItem
        {
            Id = newId,
            TemplateId = template.Id,
            Template = template,
            Quantity = quantity,
            RemainingUses = remainingUses,
            IsEmpty = false,
            AcquiredAt = DateTime.Now
        };

        _inventory.Add(newItem);
        System.Diagnostics.Debug.WriteLine($"Added item: {newItem.DisplayName} with ID: {newItem.Id}, Uses: {remainingUses}/{template.Uses}");
        return newItem;
    }

    private int GetNextCharacterItemId()
    {
        // Find the highest existing ID
        int maxId = 0;
        foreach (var item in _inventory)
        {
            if (item.Id > maxId)
                maxId = item.Id;
        }
        foreach (var item in _droppedItems)
        {
            if (item.Id > maxId)
                maxId = item.Id;
        }

        int nextId = maxId + 1;
        // Ensure we don't use a value that's already taken (safety check)
        while (_inventory.Any(i => i.Id == nextId) || _droppedItems.Any(i => i.Id == nextId))
        {
            nextId++;
        }

        System.Diagnostics.Debug.WriteLine($"Next CharacterItem ID: {nextId}");
        return nextId;
    }

    public void RemoveItemFromInventory(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return;

        // Ensure item is unequipped before removal
        if (item.IsEquipped)
        {
            UnequipItem(item.Id);
        }

        _inventory.Remove(item);
        System.Diagnostics.Debug.WriteLine($"Removed item {item.DisplayName} from inventory");
    }

    public void UpdateItemUses(int characterItemId, int usesRemaining)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item != null)
        {
            item.RemainingUses = usesRemaining;
            item.IsEmpty = usesRemaining <= 0;
            item.LastModified = DateTime.Now;
        }
    }

    public void EquipItem(int characterItemId, int slot)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null || item.IsEmpty) return;

        // ===== JEWELRY =====
        if (item.IsJewelry)
        {
            if (item.IsEquipped) UnequipItem(item.Id);
            item.IsEquipped = true;
            item.EquipmentSlot = EquipmentSlotType.None;
            item.SlotIndex = null;
            item.LastModified = DateTime.Now;
            return;
        }

        // ===== ARMOR (non-shield) =====
        if (item.IsArmor && !item.IsShield)
        {
            if (item.IsEquipped) UnequipItem(item.Id);

            var existingArmor = GetEquippedArmor();
            if (existingArmor != null && existingArmor.Id != item.Id)
            {
                UnequipItem(existingArmor.Id);
            }

            item.IsEquipped = true;
            item.EquipmentSlot = EquipmentSlotType.Armor;
            item.SlotIndex = 1;
            item.LastModified = DateTime.Now;
            return;
        }

        // ===== DETERMINE SLOT TYPE =====
        int totalHandSlots = GetTotalLimbSlots();
        EquipmentSlotType slotType;
        int slotIndex;

        if (slot <= totalHandSlots)
        {
            slotType = EquipmentSlotType.Hand;
            slotIndex = slot;
        }
        else
        {
            slotType = EquipmentSlotType.Belt;
            slotIndex = slot - totalHandSlots; // Convert to 1-4 belt slot index
        }

        int slotsNeeded = item.SlotsRequired;

        // ===== VALIDATE HAND SLOT AVAILABILITY =====
        if (slotType == EquipmentSlotType.Hand)
        {
            // Check if the slot is valid
            if (slotIndex + slotsNeeded - 1 > totalHandSlots)
            {
                Application.Current.MainPage.DisplayAlertAsync("Cannot Equip",
                    $"Not enough hand slots for this item. Available: {totalHandSlots}, Required: {slotsNeeded}.", "OK");
                return;
            }

            // Check if target slots are free
            for (int i = 0; i < slotsNeeded; i++)
            {
                int checkSlot = slotIndex + i;
                var existing = GetEquippedHandSlot(checkSlot);
                if (existing != null && existing.Id != item.Id)
                {
                    Application.Current.MainPage.DisplayAlertAsync("Slot Occupied",
                        $"Hand slot {checkSlot} is already occupied by '{existing.DisplayName}'.", "OK");
                    return;
                }
            }

            // Unequip item from any current slot
            if (item.IsEquipped) UnequipItem(item.Id);

            // Equip to hand slots
            item.IsEquipped = true;
            item.EquipmentSlot = EquipmentSlotType.Hand;
            item.SlotIndex = slotIndex;
            item.LastModified = DateTime.Now;

            System.Diagnostics.Debug.WriteLine($"Equipped {item.DisplayName} to Hand slot {slotIndex} (takes {slotsNeeded} slots, total slots: {totalHandSlots})");
        }
        else if (slotType == EquipmentSlotType.Belt)
        {
            // Check if the slot is valid
            if (slotIndex + slotsNeeded - 1 > 4)
            {
                Application.Current.MainPage.DisplayAlertAsync("Cannot Equip",
                    "Not enough belt slots for this item.", "OK");
                return;
            }

            // Check if target slots are free
            for (int i = 0; i < slotsNeeded; i++)
            {
                int checkSlot = slotIndex + i;
                var existing = GetEquippedBeltSlot(checkSlot);
                if (existing != null && existing.Id != item.Id)
                {
                    Application.Current.MainPage.DisplayAlertAsync("Slot Occupied",
                        $"Belt slot {checkSlot} is already occupied by '{existing.DisplayName}'.", "OK");
                    return;
                }
            }

            // Unequip item from any current slot
            if (item.IsEquipped) UnequipItem(item.Id);

            // Equip to belt slots
            item.IsEquipped = true;
            item.EquipmentSlot = EquipmentSlotType.Belt;
            item.SlotIndex = slotIndex;
            item.LastModified = DateTime.Now;

            System.Diagnostics.Debug.WriteLine($"Equipped {item.DisplayName} to Belt slot {slotIndex} (takes {slotsNeeded} slots)");
        }
    }

    public void EquipItemAsArmor(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null || item.IsEmpty) return;

        // Check if armor slot is already occupied
        var existingArmor = GetEquippedArmor();
        if (existingArmor != null)
        {
            UnequipItem(existingArmor.Id);
        }

        // Unequip from any other slot first
        if (item.IsEquipped)
        {
            UnequipItem(item.Id);
        }

        item.IsEquipped = true;
        item.EquipmentSlot = EquipmentSlotType.Armor;
        item.SlotIndex = 1; // Only one armor slot
        item.LastModified = DateTime.Now;
    }

    public void UnequipItem(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return;

        // Clear the slot data
        item.IsEquipped = false;
        item.EquipmentSlot = EquipmentSlotType.None;
        item.SlotIndex = null;
        item.LastModified = DateTime.Now;

        System.Diagnostics.Debug.WriteLine($"Unequipped {item.DisplayName}");
    }

    public void DropItem(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item != null)
        {
            if (item.IsEquipped)
            {
                UnequipItem(item.Id);
            }

            item.IsDropped = true;
            item.DroppedAt = DateTime.Now;
            _inventory.Remove(item);
            _droppedItems.Add(item);
            System.Diagnostics.Debug.WriteLine($"Dropped item: {item.DisplayName} (ID: {item.Id})");

            _ = _actionLogService.LogItemDroppedAsync(item, PlayerName, PlayerId);   // NEW
            _ = _recoveryService.AddRecoverableAsync(item, PlayerId);
        }
    }

    public void RestoreDroppedItem(int characterItemId)
    {
        var item = _droppedItems.FirstOrDefault(i => i.Id == characterItemId);
        if (item != null)
        {
            item.IsDropped = false;
            item.DroppedAt = null;
            _droppedItems.Remove(item);
            _inventory.Add(item);
        }
    }

    public void TransferItemToPlayer(int characterItemId, int targetCharacterId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item != null)
        {
            if (item.IsEquipped)
            {
                UnequipItem(item.Id);
            }

            item.CharacterId = targetCharacterId;
            item.LastModified = DateTime.Now;
            item.SyncId = Guid.NewGuid().ToString();
        }
    }

    // ===== HOT-SWAP METHODS =====

    public int? FindAvailableBeltSlot(int slotsNeeded = 1)
    {
        if (slotsNeeded < 1 || slotsNeeded > 2) return null;

        // Count used belt slots and track occupied slots
        bool[] occupied = new bool[5]; // 1-indexed, ignore index 0
        for (int i = 1; i <= 4; i++)
        {
            var existing = GetEquippedBeltSlot(i);
            if (existing != null)
            {
                // Mark this slot and any additional slots the item occupies
                occupied[i] = true;
                if (existing.SlotsRequired == 2 && i < 4)
                {
                    occupied[i + 1] = true;
                }
            }
        }

        // Count total used slots
        int usedSlots = 0;
        for (int i = 1; i <= 4; i++)
        {
            if (occupied[i]) usedSlots++;
        }

        // Check if there's room
        if (usedSlots + slotsNeeded > 4) return null;

        // Find first available slot(s)
        if (slotsNeeded == 1)
        {
            for (int i = 1; i <= 4; i++)
            {
                if (!occupied[i]) return i;
            }
        }
        else if (slotsNeeded == 2)
        {
            for (int i = 1; i <= 3; i++)
            {
                if (!occupied[i] && !occupied[i + 1]) return i;
            }
        }

        return null;
    }

    public int? FindAvailableHandSlot(int slotsNeeded = 1)
    {
        int totalSlots = GetTotalLimbSlots();
        if (slotsNeeded < 1 || slotsNeeded > 2 || totalSlots < slotsNeeded) return null;

        // Track occupied slots
        bool[] occupied = new bool[totalSlots + 1]; // 1-indexed
        for (int i = 1; i <= totalSlots; i++)
        {
            if (GetEquippedHandSlot(i) != null)
            {
                occupied[i] = true;
            }
        }

        if (slotsNeeded == 1)
        {
            for (int i = 1; i <= totalSlots; i++)
            {
                if (!occupied[i]) return i;
            }
        }
        else if (slotsNeeded == 2)
        {
            for (int i = 1; i <= totalSlots - 1; i++)
            {
                if (!occupied[i] && !occupied[i + 1]) return i;
            }
        }

        return null;
    }

    public bool CanSwapToBelt(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return false;
        if (!item.IsEquipped) return false;

        // Check if the item is already in a belt slot
        if (item.EquipmentSlot == EquipmentSlotType.Belt) return false;

        // Check if there's an available belt slot with enough space
        int slotsNeeded = item.SlotsRequired;
        var availableSlot = FindAvailableBeltSlot(slotsNeeded);
        return availableSlot.HasValue;
    }

    public bool CanSwapToHand(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return false;
        if (!item.IsEquipped) return false;

        // Check if the item is already in a hand slot
        if (item.EquipmentSlot == EquipmentSlotType.Hand) return false;

        // Check if there's an available hand slot with enough space
        int slotsNeeded = item.SlotsRequired;

        // For two-handed items, we need a paired limb set with both slots free
        if (slotsNeeded > 1)
        {
            // Check if any paired limb set has all slots free
            foreach (var limbSet in _limbSets)
            {
                if (limbSet.PairType != LimbPairType.Paired) continue;
                if (limbSet.IsOccupiedByTwoHandedItem) continue;

                bool allSlotsFree = true;
                foreach (var slotIndex in limbSet.SlotIndices)
                {
                    if (GetEquippedHandSlot(slotIndex + 1) != null)
                    {
                        allSlotsFree = false;
                        break;
                    }
                }

                if (allSlotsFree) return true;
            }
            return false;
        }

        var availableSlot = FindAvailableHandSlot(1);
        return availableSlot.HasValue;
    }

    public void SwapToBelt(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return;
        if (!item.IsEquipped) return;
        if (item.EquipmentSlot == EquipmentSlotType.Belt) return;

        int slotsNeeded = item.SlotsRequired;
        var availableSlot = FindAvailableBeltSlot(slotsNeeded);
        if (!availableSlot.HasValue) return;

        // If it's a two-handed item in a hand slot, clean up the limb set
        if (slotsNeeded > 1 && item.EquipmentSlot == EquipmentSlotType.Hand)
        {
            // Find and clear the limb set
            foreach (var limbSet in _limbSets)
            {
                if (limbSet.TwoHandedItemId == characterItemId)
                {
                    limbSet.TwoHandedItemId = null;
                    break;
                }
            }
        }

        // Unequip from current slot
        int oldSlot = item.SlotIndex ?? 0;
        UnequipItem(item.Id);

        // Equip to belt
        int beltSlotNumber = availableSlot.Value;

        item.IsEquipped = true;
        item.EquipmentSlot = EquipmentSlotType.Belt;
        item.SlotIndex = beltSlotNumber;
        item.LastModified = DateTime.Now;

        System.Diagnostics.Debug.WriteLine($"Swapped '{item.DisplayName}' to Belt slot {beltSlotNumber} (takes {slotsNeeded} slots)");
    }

    public void SwapToHand(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return;
        if (!item.IsEquipped) return;
        if (item.EquipmentSlot == EquipmentSlotType.Hand) return;

        int slotsNeeded = item.SlotsRequired;

        if (slotsNeeded > 1)
        {
            // Find a free paired limb set
            PrehensileLimbSet? targetLimbSet = null;
            foreach (var limbSet in _limbSets)
            {
                if (limbSet.PairType != LimbPairType.Paired) continue;
                if (limbSet.IsOccupiedByTwoHandedItem) continue;

                bool allSlotsFree = true;
                foreach (var slotIndex in limbSet.SlotIndices)
                {
                    if (GetEquippedHandSlot(slotIndex + 1) != null)
                    {
                        allSlotsFree = false;
                        break;
                    }
                }

                if (allSlotsFree)
                {
                    targetLimbSet = limbSet;
                    break;
                }
            }

            if (targetLimbSet == null) return;

            // Unequip from belt
            UnequipItem(item.Id);

            // Equip to hand set
            int firstSlot = targetLimbSet.SlotIndices.First() + 1;

            item.IsEquipped = true;
            item.EquipmentSlot = EquipmentSlotType.Hand;
            item.SlotIndex = firstSlot;
            item.LastModified = DateTime.Now;

            targetLimbSet.TwoHandedItemId = characterItemId;

            System.Diagnostics.Debug.WriteLine($"Swapped '{item.DisplayName}' to Hand set '{targetLimbSet.DisplayName}'");
            return;
        }

        // Single-handed item
        var availableSlot = FindAvailableHandSlot(1);
        if (!availableSlot.HasValue) return;

        // Unequip from current slot
        int oldSlot = item.SlotIndex ?? 0;
        UnequipItem(item.Id);

        // Equip to hand
        int handSlotNumber = availableSlot.Value;

        item.IsEquipped = true;
        item.EquipmentSlot = EquipmentSlotType.Hand;
        item.SlotIndex = handSlotNumber;
        item.LastModified = DateTime.Now;

        System.Diagnostics.Debug.WriteLine($"Swapped '{item.DisplayName}' to Hand slot {handSlotNumber}");
    }

    // === TWO-HANDED ITEM MANAGEMENT ===

    public bool CanEquipTwoHandedItem(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return false;
        if (item.SlotsRequired < 2) return false; // Not a two-handed item
        if (item.IsEquipped) return false;

        // Find a limb set that can accommodate a two-handed item
        foreach (var limbSet in _limbSets)
        {
            if (limbSet.PairType != LimbPairType.Paired) continue;
            if (limbSet.IsOccupiedByTwoHandedItem) continue;

            // Check if all slots in this set are free
            bool allSlotsFree = true;
            foreach (var slotIndex in limbSet.SlotIndices)
            {
                var existingItem = GetEquippedHandSlot(slotIndex + 1); // Convert to 1-based
                if (existingItem != null)
                {
                    allSlotsFree = false;
                    break;
                }
            }

            if (allSlotsFree)
                return true;
        }

        return false;
    }

    public void EquipTwoHandedItem(int characterItemId, int limbSetId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return;
        if (item.SlotsRequired < 2) return; // Not a two-handed item

        // ===== FIND THE SPECIFIED LIMB SET =====
        var limbSet = _limbSets.FirstOrDefault(l => l.Id == limbSetId);
        if (limbSet == null)
        {
            // If no specific limb set found, find the first available paired set
            limbSet = _limbSets.FirstOrDefault(l =>
                l.PairType == LimbPairType.Paired &&
                !l.IsOccupiedByTwoHandedItem &&
                l.SlotIndices.All(slotIndex => GetEquippedHandSlot(slotIndex + 1) == null));
        }

        if (limbSet == null) return;
        if (limbSet.PairType != LimbPairType.Paired) return;
        if (limbSet.IsOccupiedByTwoHandedItem) return;

        // Check if all slots in this set are free
        bool allSlotsFree = true;
        foreach (var slotIndex in limbSet.SlotIndices)
        {
            var existingItem = GetEquippedHandSlot(slotIndex + 1);
            if (existingItem != null)
            {
                allSlotsFree = false;
                break;
            }
        }

        if (!allSlotsFree) return;

        // Equip the item to the first slot of the set
        int firstSlot = limbSet.SlotIndices.First() + 1;

        // Remove any existing item from these slots first
        foreach (var slotIndex in limbSet.SlotIndices)
        {
            var existing = GetEquippedHandSlot(slotIndex + 1);
            if (existing != null && existing.Id != characterItemId)
            {
                UnequipItem(existing.Id);
            }
        }

        // Equip the item
        item.IsEquipped = true;
        item.EquipmentSlot = EquipmentSlotType.Hand;
        item.SlotIndex = firstSlot;
        item.LastModified = DateTime.Now;

        // Mark the limb set as occupied by a two-handed item
        limbSet.TwoHandedItemId = characterItemId;

        System.Diagnostics.Debug.WriteLine($"Equipped two-handed item '{item.DisplayName}' to limb set '{limbSet.DisplayName}'");
    }

    public int? FindAvailablePairedLimbSetId()
    {
        foreach (var limbSet in _limbSets)
        {
            if (limbSet.PairType != LimbPairType.Paired) continue;
            if (limbSet.IsOccupiedByTwoHandedItem) continue;

            // Check if all slots in this set are free
            bool allSlotsFree = true;
            foreach (var slotIndex in limbSet.SlotIndices)
            {
                var existingItem = GetEquippedHandSlot(slotIndex + 1);
                if (existingItem != null)
                {
                    allSlotsFree = false;
                    break;
                }
            }

            if (allSlotsFree)
            {
                return limbSet.Id;
            }
        }

        return null;
    }

    public void UnequipTwoHandedItem(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return;

        // Find which limb set has this item
        foreach (var limbSet in _limbSets)
        {
            if (limbSet.TwoHandedItemId == characterItemId)
            {
                limbSet.TwoHandedItemId = null;
                break;
            }
        }

        // Unequip the item normally
        UnequipItem(characterItemId);
    }

    public PrehensileLimbSet? GetLimbSetForTwoHandedItem(int characterItemId)
    {
        return _limbSets.FirstOrDefault(l => l.TwoHandedItemId == characterItemId);
    }

    public bool IsLimbSetOccupiedByTwoHandedItem(int limbSetId)
    {
        var limbSet = _limbSets.FirstOrDefault(l => l.Id == limbSetId);
        return limbSet?.IsOccupiedByTwoHandedItem ?? false;
    }

    public void InitializeInventoryFromStartingItems()
    {
        // Clear existing inventory first
        _inventory.Clear();
        _droppedItems.Clear();

        foreach (var startingItem in SelectedStartingItems)
        {
            Item? template = null;

            if (startingItem.ItemDetails != null)
            {
                template = startingItem.ItemDetails;
            }
            else
            {
                try
                {
                    var task = _itemDataService?.GetItemByIdAsync(startingItem.ItemId);
                    template = task?.Result;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error resolving item {startingItem.ItemId}: {ex.Message}");
                }
            }

            if (template == null)
            {
                System.Diagnostics.Debug.WriteLine($"Template not found for starting item ID: {startingItem.ItemId}");
                continue;
            }

            int newId = GetNextCharacterItemId();

            // ===== CRITICAL: Starting items should start with FULL uses =====
            int remainingUses = 0;
            if (template.HasUses)
            {
                // Starting items start with full uses
                remainingUses = template.Uses.Value;
            }
            else
            {
                // Items without uses get 0 (unlimited)
                remainingUses = 0;
            }

            var characterItem = new CharacterItem
            {
                Id = newId,
                TemplateId = startingItem.ItemId,
                Template = template,
                Quantity = startingItem.Quantity,
                PlayerNote = startingItem.PlayerNote,
                RemainingUses = remainingUses,  // ← Full uses for new items
                IsEmpty = false,
                AcquiredAt = DateTime.Now
            };

            _inventory.Add(characterItem);
            System.Diagnostics.Debug.WriteLine($"Added starting item: {characterItem.DisplayName} with ID: {characterItem.Id}, Uses: {remainingUses}/{template.Uses}");
        }

        System.Diagnostics.Debug.WriteLine($"=== Inventory initialized with {_inventory.Count} items from starting items ===");
    }

    // ====================================== END OF INVENTORY SYSTEM ====================================== //

    //CREATE CHARACTER SAVE DATA
    public CharacterSaveData CreateSaveData()
    {
        _isSaving = true;

        try
        {
            if ((_inventory == null || _inventory.Count == 0) && SelectedStartingItems != null && SelectedStartingItems.Count > 0)
            {
                InitializeInventoryFromStartingItems();
            }

            return new CharacterSaveData
            {
                PlayerName = PlayerName,
                PlayerId = PlayerId,
                CharacterName = CharacterName,
                LastSaved = DateTime.Now,
                IsComplete = IsCharacterComplete,
                IsSaved = true,
                CampaignType = CampaignType,
                CurrentPage = CurrentPage,
                Data = new CharacterData
                {
                    // ===== HINDERANCE DATA =====
                    SelectedHinderance = SelectedHinderance != null ? new Hinderance
                    {
                        Id = SelectedHinderance.Id,
                        Name = SelectedHinderance.Name,
                        Description = SelectedHinderance.Description,
                        VigorModifier = SelectedHinderance.VigorModifier,
                        AgilityModifier = SelectedHinderance.AgilityModifier,
                        MindModifier = SelectedHinderance.MindModifier,
                        SpiritModifier = SelectedHinderance.SpiritModifier,
                        SkillModifiers = SelectedHinderance.SkillModifiers?.Select(sm => new HinderanceSkillModifier
                        {
                            SkillName = sm.SkillName,
                            Modifier = sm.Modifier
                        }).ToList() ?? new List<HinderanceSkillModifier>(),
                        AllowsStatBonus = SelectedHinderance.AllowsStatBonus,
                        AllowsTrainingPoint = SelectedHinderance.AllowsTrainingPoint
                    } : null,

                    SelectedRewardType = SelectedRewardType,
                    SelectedRewardSkillName = SelectedRewardSkillName,
                    RewardStatBonusAmount = RewardStatBonusAmount,
                    RewardStatName = RewardStatName,

                    // ===== CAMPAIGN DATA =====
                    CampaignType = CampaignType,
                    CurrentPage = CurrentPage,
                    PlayerName = PlayerName,
                    PlayerId = PlayerId,
                    CharacterName = CharacterName,

                    // ===== RACE DATA =====
                    CharacterRace = CharacterRace,
                    CharacterRaceDescription = CharacterRaceDescription,
                    CharacterRaceFeatureName = CharacterRaceFeatureName,
                    CharacterRaceFeatureDescription = CharacterRaceFeatureDescription,
                    SelectedRaceId = SelectedRaceId,
                    // ===== PREHENSILE LIMBS =====
                    LimbSets = new List<PrehensileLimbSet>(_limbSets),

                    // ===== RACE BONUSES =====
                    RaceVigorBonus = RaceVigorBonus,
                    RaceAgilityBonus = RaceAgilityBonus,
                    RaceMindBonus = RaceMindBonus,
                    RaceSpiritBonus = RaceSpiritBonus,
                    RaceSkillBonuses = new List<RaceSkillBonus>(RaceSkillBonuses),

                    // ===== CLASS DATA =====
                    CharacterClassName = CharacterClassName,
                    CharacterClassDescription = CharacterClassDescription,
                    CharacterClassFeatureName = CharacterClassFeatureName,
                    CharacterClassFeatureDescription = CharacterClassFeatureDescription,
                    LevelUpAllocationRequirement = LevelUpAllocationRequirement,
                    CharacterRecurringBenefit = CharacterRecurringBenefit,

                    // ===== CLASS & FEATURE IDs =====
                    SelectedClassId = SelectedClassId,
                    AcquiredFeatureIds = new List<int>(AcquiredFeatureIds),

                    // ===== ABILITY SCORES =====
                    StatVigor = StatVigor,
                    StatAgility = StatAgility,
                    StatMind = StatMind,
                    StatSpirit = StatSpirit,
                    Hitpoints = Hitpoints,
                    ASMStatVigor = ASMStatVigor,
                    ASMStatAgility = ASMStatAgility,
                    ASMStatMind = ASMStatMind,
                    ASMStatSpirit = ASMStatSpirit,

                    // ===== SUB-FEATURES =====
                    AcquiredBlueprintIds = new List<int>(AcquiredBlueprintIds),
                    AcquiredQuipIds = new List<int>(AcquiredQuipIds),
                    AcquiredSpellIds = new List<int>(AcquiredSpellIds),
                    AcquiredTechniqueIds = new List<int>(AcquiredTechniqueIds),
                    AcquiredBlueprints = new List<Blueprint>(AcquiredBlueprints),
                    AcquiredQuips = new List<Quip>(AcquiredQuips),
                    AcquiredSpells = new List<Spell>(AcquiredSpells),
                    AcquiredTechniques = new List<Technique>(AcquiredTechniques),

                    // ===== BACKGROUND DATA =====
                    SelectedBackgroundName = SelectedBackgroundName,
                    SelectedBackgroundDescription = SelectedBackgroundDescription,
                    BackgroundVigorBonus = BGVigorBonus,
                    BackgroundAgilityBonus = BGAgilityBonus,
                    BackgroundMindBonus = BGMindBonus,
                    BackgroundSpiritBonus = BGSpiritBonus,
                    BackgroundSkillBonuses = new List<BGSkillBonuses>(SelectedSkillBonuses),
                    HinderanceVigorPenalty = HinderanceVigorPenalty,
                    HinderanceAgilityPenalty = HinderanceAgilityPenalty,
                    HinderanceMindPenalty = HinderanceMindPenalty,
                    HinderanceSpiritPenalty = HinderanceSpiritPenalty,
                    SelectedStartingItems = new List<StartingItem>(SelectedStartingItems),
                    SelectedLanguages = new List<Language>(SelectedLanguages),

                    // ===== FAMILIARS =====
                    AcquiredFamiliars = new List<Familiar>(_acquiredFamiliars),

                    // ===== TRAINING POINTS =====
                    SkillTrainingLevels = new Dictionary<string, int>(SkillTrainingLevels),

                    // ===== TOTAL BONUSES =====
                    TotalVigorBonus = BGVigorBonus,
                    TotalAgilityBonus = BGAgilityBonus,
                    TotalMindBonus = BGMindBonus,
                    TotalSpiritBonus = BGSpiritBonus,
                    TotalTrainingPointsBonus = AvailableTrainingPoints,

                    // ===== INVENTORY =====
                    Inventory = new List<CharacterItem>(_inventory)
                }
            };
        }
        finally
        {
            _isSaving = false;
        }
    }

    // === POPULATE FROM SAVE DATA ===
    public void PopulateFromSaveData(CharacterSaveData saveData)
    {
        if (saveData?.Data == null) return;

        System.Diagnostics.Debug.WriteLine("=== POPULATING FROM SAVE DATA ===");

        // ===== HINDERANCE DATA =====
        SelectedHinderance = saveData.Data.SelectedHinderance;
        SelectedRewardType = saveData.Data.SelectedRewardType;
        SelectedRewardSkillName = saveData.Data.SelectedRewardSkillName;
        RewardStatBonusAmount = saveData.Data.RewardStatBonusAmount;
        RewardStatName = saveData.Data.RewardStatName;

        // Strings
        CampaignType = saveData.Data.CampaignType ?? string.Empty;
        PlayerName = saveData.Data.PlayerName ?? string.Empty;
        PlayerId = saveData.Data.PlayerId != Guid.Empty
            ? saveData.Data.PlayerId
            : InstallIdentity.GetOrCreateInstallPlayerId();
        CharacterName = saveData.Data.CharacterName ?? string.Empty;

        // ===== RACE DATA =====
        CharacterRace = saveData.Data.CharacterRace ?? string.Empty;
        CharacterRaceDescription = saveData.Data.CharacterRaceDescription ?? string.Empty;
        CharacterRaceFeatureName = saveData.Data.CharacterRaceFeatureName ?? string.Empty;
        CharacterRaceFeatureDescription = saveData.Data.CharacterRaceFeatureDescription ?? string.Empty;
        SelectedRaceId = saveData.Data.SelectedRaceId;

        // ===== PREHENSILE LIMBS =====
        LimbSets = saveData.Data.LimbSets ?? new List<PrehensileLimbSet>();

        // ===== RACE BONUSES =====
        RaceVigorBonus = saveData.Data.RaceVigorBonus;
        RaceAgilityBonus = saveData.Data.RaceAgilityBonus;
        RaceMindBonus = saveData.Data.RaceMindBonus;
        RaceSpiritBonus = saveData.Data.RaceSpiritBonus;
        RaceSkillBonuses = saveData.Data.RaceSkillBonuses ?? new List<RaceSkillBonus>();

        // ===== CLASS DATA =====
        CharacterClassName = saveData.Data.CharacterClassName ?? string.Empty;
        CharacterClassDescription = saveData.Data.CharacterClassDescription ?? string.Empty;
        CharacterClassFeatureName = saveData.Data.CharacterClassFeatureName ?? string.Empty;
        CharacterClassFeatureDescription = saveData.Data.CharacterClassFeatureDescription ?? string.Empty;
        LevelUpAllocationRequirement = saveData.Data.LevelUpAllocationRequirement ?? string.Empty;
        CharacterRecurringBenefit = saveData.Data.CharacterRecurringBenefit ?? string.Empty;
        CurrentPage = saveData.Data.CurrentPage ?? saveData.CurrentPage ?? string.Empty;

        // Class & Feature IDs
        SelectedClassId = saveData.Data.SelectedClassId;
        AcquiredFeatureIds = saveData.Data.AcquiredFeatureIds ?? new List<int>();

        // Ability Scores
        StatVigor = saveData.Data.StatVigor;
        StatAgility = saveData.Data.StatAgility;
        StatMind = saveData.Data.StatMind;
        StatSpirit = saveData.Data.StatSpirit;
        ASMStatVigor = saveData.Data.ASMStatVigor;
        ASMStatAgility = saveData.Data.ASMStatAgility;
        ASMStatMind = saveData.Data.ASMStatMind;
        ASMStatSpirit = saveData.Data.ASMStatSpirit;

        // Sub-features
        AcquiredBlueprintIds = saveData.Data.AcquiredBlueprintIds ?? new List<int>();
        AcquiredQuipIds = saveData.Data.AcquiredQuipIds ?? new List<int>();
        AcquiredSpellIds = saveData.Data.AcquiredSpellIds ?? new List<int>();
        AcquiredTechniqueIds = saveData.Data.AcquiredTechniqueIds ?? new List<int>();
        AcquiredBlueprints = saveData.Data.AcquiredBlueprints ?? new List<Blueprint>();
        AcquiredQuips = saveData.Data.AcquiredQuips ?? new List<Quip>();
        AcquiredSpells = saveData.Data.AcquiredSpells ?? new List<Spell>();
        AcquiredTechniques = saveData.Data.AcquiredTechniques ?? new List<Technique>();

        // ===== BACKGROUND DATA =====
        SelectedBackgroundName = saveData.Data.SelectedBackgroundName ?? string.Empty;
        SelectedBackgroundDescription = saveData.Data.SelectedBackgroundDescription ?? string.Empty;
        BackgroundVigorBonus = saveData.Data.BackgroundVigorBonus;
        BackgroundAgilityBonus = saveData.Data.BackgroundAgilityBonus;
        BackgroundMindBonus = saveData.Data.BackgroundMindBonus;
        BackgroundSpiritBonus = saveData.Data.BackgroundSpiritBonus;
        SelectedSkillBonuses = saveData.Data.BackgroundSkillBonuses ?? new List<BGSkillBonuses>();
        HinderanceVigorPenalty = saveData.Data.HinderanceVigorPenalty ?? 0;
        HinderanceAgilityPenalty = saveData.Data.HinderanceAgilityPenalty ?? 0;
        HinderanceMindPenalty = saveData.Data.HinderanceMindPenalty ?? 0;
        HinderanceSpiritPenalty = saveData.Data.HinderanceSpiritPenalty ?? 0;
        BGVigorBonus = saveData.Data.BackgroundVigorBonus;
        BGAgilityBonus = saveData.Data.BackgroundAgilityBonus;
        BGMindBonus = saveData.Data.BackgroundMindBonus;
        BGSpiritBonus = saveData.Data.BackgroundSpiritBonus;
        SelectedStartingItems = saveData.Data.SelectedStartingItems ?? new List<StartingItem>();
        SelectedLanguages = saveData.Data.SelectedLanguages ?? new List<Language>();

        // ===== FAMILIARS =====
        AcquiredFamiliars = saveData.Data.AcquiredFamiliars ?? new List<Familiar>();

        // ===== TRAINING POINTS =====
        SkillTrainingLevels = saveData.Data.SkillTrainingLevels != null
            ? new Dictionary<string, int>(saveData.Data.SkillTrainingLevels)
            : new Dictionary<string, int>
            {
            { "Athletics", 0 },
            { "Acrobatics", 0 },
            { "Aim", 0 },
            { "Arcana", 0 },
            { "Artifice", 0 },
            { "Commune", 0 },
            { "Constitution", 0 },
            { "Deception", 0 },
            { "Diplomacy", 0 },
            { "Drive", 0 },
            { "Grapple", 0 },
            { "Heal", 0 },
            { "Investigate", 0 },
            { "Lore", 0 },
            { "Sight", 0 },
            { "Presence", 0 },
            { "Ride", 0 },
            { "Stealth", 0 },
            { "Survival", 0 },
            { "Thief", 0 }
            };
        MigrateTrainingPointsIfNeeded();

        // ===== TOTAL BONUSES =====
        AvailableTrainingPoints = saveData.Data.TotalTrainingPointsBonus;

        // ===== INVENTORY =====
        Inventory = saveData.Data.Inventory ?? new List<CharacterItem>();

        // Persistence flags
        IsCharacterComplete = saveData.IsComplete;
        IsCharacterSaved = true;
        SaveFileName = saveData.FileName;
        LastSavedDate = saveData.LastSaved;

        System.Diagnostics.Debug.WriteLine("=== POPULATE COMPLETE ===");
    }

    // === CLEAR ===

    public void ClearCharacterCreationData()
    {
        System.Diagnostics.Debug.WriteLine("=== CLEARING ALL CHARACTER CREATION DATA ===");

        // === HINDERANCE DATA ===
        _selectedHinderance = null;
        SelectedRewardType = HinderanceRewardType.None;
        SelectedRewardSkillName = null;
        RewardStatBonusAmount = 0;
        RewardStatName = null;
        HinderanceVigorPenalty = 0;
        HinderanceAgilityPenalty = 0;
        HinderanceMindPenalty = 0;
        HinderanceSpiritPenalty = 0;

        // === STRINGS ===
        CampaignType = string.Empty;
        PlayerName = string.Empty;
        PlayerId = InstallIdentity.GetOrCreateInstallPlayerId();
        CharacterName = string.Empty;
        CurrentPage = string.Empty;

        // === RACE DATA ===
        CharacterRace = string.Empty;
        CharacterRaceDescription = string.Empty;
        CharacterRaceFeatureName = string.Empty;
        CharacterRaceFeatureDescription = string.Empty;
        SelectedRaceId = null;
        RaceVigorBonus = 0;
        RaceAgilityBonus = 0;
        RaceMindBonus = 0;
        RaceSpiritBonus = 0;
        RaceSkillBonuses.Clear();
        // === PREHENSILE LIMBS ===
        _limbSets.Clear();

        // === CLASS DATA ===
        CharacterClassName = string.Empty;
        CharacterClassDescription = string.Empty;
        CharacterClassFeatureName = string.Empty;
        CharacterClassFeatureDescription = string.Empty;
        LevelUpAllocationRequirement = string.Empty;
        CharacterRecurringBenefit = string.Empty;

        // === STATS ===
        StatVigor = null;
        StatAgility = null;
        StatMind = null;
        StatSpirit = null;
        ASMStatVigor = 0;
        ASMStatAgility = 0;
        ASMStatMind = 0;
        ASMStatSpirit = 0;

        // === CLASS & FEATURE IDs ===
        SelectedClassId = null;
        AcquiredFeatureIds.Clear();

        // === SUB-FEATURE IDs ===
        AcquiredBlueprintIds.Clear();
        AcquiredQuipIds.Clear();
        AcquiredSpellIds.Clear();
        AcquiredTechniqueIds.Clear();

        // === SUB-FEATURE FULL OBJECTS ===
        _acquiredBlueprints.Clear();
        _acquiredQuips.Clear();
        _acquiredSpells.Clear();
        _acquiredTechniques.Clear();

        // === PERSISTENCE FLAGS ===
        IsCharacterComplete = false;
        IsCharacterSaved = false;
        LastSavedDate = null;
        SaveFileName = null;

        // === BACKGROUND INFORMATION ===
        SelectedBackgroundName = string.Empty;
        SelectedBackgroundDescription = string.Empty;
        SelectedStartingItems.Clear();
        SelectedSkillBonuses.Clear();
        SelectedItemChoices.Clear();
        BGVigorBonus = 0;
        BGAgilityBonus = 0;
        BGMindBonus = 0;
        BGSpiritBonus = 0;
        SelectedLanguages.Clear();

        // === FAMILIARS ===
        _acquiredFamiliars.Clear();

        // === TRAINING POINTS ===
        AvailableTrainingPoints = 4;
        foreach (var key in SkillTrainingLevels.Keys.ToList())
        {
            SkillTrainingLevels[key] = 0;
        }

        // === INVENTORY ===
        _inventory.Clear();
        _droppedItems.Clear();

        System.Diagnostics.Debug.WriteLine("=== CLEAR COMPLETE ===");
    }

    // === UTILITY ===

    public int GetAbilityModifier(int score)
    {
        int clampedScore = Math.Clamp(score, 1, 20);

        // For negative numbers, we want floor, not truncate
        int modifier = (clampedScore - 10) / 2;

        // Adjust for negative numbers (since integer division truncates toward zero)
        if (clampedScore < 10 && (clampedScore - 10) % 2 != 0)
        {
            modifier--;
        }

        return Math.Clamp(modifier, -5, 5);
    }
}