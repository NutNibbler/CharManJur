using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static CharManJur.Services.CharAttribDataService;

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
    int? SelectedBackgroundId { get; set; }
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
    Familiar? GetFamiliar(Guid instanceId);
    bool AddItemToFamiliarInventory(Guid familiarInstanceId, Item template, int quantity = 1, int? writtenValueInChips = null, int? startingUses = null, int? startingUsageDie = null);
    bool RemoveItemFromFamiliarInventory(Guid familiarInstanceId, int characterItemId);
    void AdjustFamiliarFatigue(Guid familiarInstanceId, int delta);
    bool SetFamiliarItemEquipped(Guid familiarInstanceId, int characterItemId, bool equipped);
    Familiar BestowFamiliar(Familiar template);
    void SetFamiliarGivenName(Guid familiarInstanceId, string? name);
    bool RemoveFamiliar(Guid familiarInstanceId);
    void AdjustFamiliarAbility(Guid familiarInstanceId, FamiliarStat stat, int delta);
    void AdjustFamiliarHitpoints(Guid familiarInstanceId, int delta);
    void RestoreFamiliar(Guid familiarInstanceId);
    bool HandItemToFamiliar(int characterItemId, Guid familiarInstanceId, int quantity);
    bool TakeItemFromFamiliar(Guid familiarInstanceId, int familiarItemId, int quantity);
    int DropFormPackItems(Guid familiarInstanceId);   // leaves a form pack's items behind, into the recovery table

    // === PAGE TRACKING ===
    string CurrentPage { get; set; }
    void SetCurrentPage(string pageRoute);

    // === TRAINING POINTS & LEVELING ===
    int AvailableTrainingPoints { get; set; }
    Dictionary<string, int> SkillTrainingLevels { get; set; }
    int UnspentTrainingPoints { get; set; }   // live-game pool, spent on skills by Finish Training
    void MigrateCreationTrainingPoints();   // moves points left unspent at creation into the live-game pool (once)
    int ClassTrainingPointsGranted { get; }   // training points the character's classes grant (Rogue: 2 per pick)
    int CreationTrainingPointsBase { get; }   // what character creation starts the training pool at
    int PendingLevels { get; set; }          // level-ups the GM granted that the player hasn't taken yet
    List<int> StatRolls { get; set; }
    List<int> StatRollPool { get; set; }
    int CharacterLevel { get; }              // total class picks (creation is level 1)
    string ClassPicksSummary { get; }        // "Rogue ×2 · Warrior ×1"

    // === STAT-SKILL MAPPING ===
    int GetAbilityModifier(int score);
    int GetSkillTotal(string skillName);
    SkillModifierBreakdown GetSkillBreakdown(string skillName);
    int GetSkillMaxASM(string skillName);          // the skill's modifier from the stat's MAX value
    string GetSkillStatName(string skillName);     // "Vigor", "Agility", "Mind" or "Spirit"

    // === TOTAL STATS (Base + Bonuses + Penalties) ===
    int TotalStatVigor { get; }
    int TotalStatAgility { get; }
    int TotalStatMind { get; }
    int TotalStatSpirit { get; }
    int PackVigorModifier { get; }   // the character's own Vigor modifier; sizes the pack, ignores the form

    // === DEPRIVATION / DAILY NEEDS ===
    bool NeedsMet { get; }
    bool Rested { get; }
    bool IsDeprived { get; }
    int DeprivedDays { get; }
    void AffirmNeedsMet();
    DayEndResult EndDay();

    // === TOTAL ASM (Modifier from Total Stats) ===
    int TotalASMStatVigor { get; }
    int TotalASMStatAgility { get; }
    int TotalASMStatMind { get; }
    int TotalASMStatSpirit { get; }

    // === CURRENT ASM (modifier from the CURRENT stats, after ability loss and effects) ===
    // Skill checks use these, so a depleted stat lowers the roll. Pack slots, Hit Protection and the
    // Fatigue Bar keep using the TotalASM values above.
    int CurrentASMStatVigor { get; }
    int CurrentASMStatAgility { get; }
    int CurrentASMStatMind { get; }
    int CurrentASMStatSpirit { get; }

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
    Guid CharacterGuid { get; set; }
    string FileName { get; set; }
    string PlayerName { get; set; }
    string CharacterName { get; set; }
    WeaponProficiencyState Proficiency { get; set; }

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


    // === LIVE-PLAY CURRENT VALUES ===
    int HitpointsDamage { get; set; }
    int CurrentHitpoints { get; }
    int CurrentVigorAdjustment { get; set; }
    int CurrentAgilityAdjustment { get; set; }
    int CurrentMindAdjustment { get; set; }
    int CurrentSpiritAdjustment { get; set; }
    int CurrentStatVigor { get; }
    int CurrentStatAgility { get; }
    int CurrentStatMind { get; }
    int CurrentStatSpirit { get; }
    DamageResult ApplyDamage(int amount, bool bypassHitProtection = false);
    void ApplyParryLoss(int attackerRoll);
    void LogCombatReaction(CombatReactionType reaction, string? note = null);
    void LogSkillCheck(string skillName, int diceTotal, int modifier, string advantageMode = "None", int? advantageDieResult = null);
    void LogAbilitySave(string statName, int roll, bool success);
    bool HasPendingVigorSave { get; }
    bool HasDeathsDoor { get; }
    void AddVigorSaveReminder();
    void ResolveVigorSave(bool success);
    void ClearDeathsDoor();
    int ArmorRating { get; }
    LongRestResult TakeLongRest(LongRestType type, int? comfortableRoll = null);
    void RestoreHitProtection();
    void AdjustCurrentStat(string statName, int delta);
    bool EquipToHandAuto(int characterItemId);
    bool EquipTwoHandedItemWithSwap(int characterItemId, int limbSetId);
    BeltEquipResult TryEquipToBelt(int characterItemId);
    bool EquipToBeltAtSlot(int characterItemId, int startSlot);
    bool EquipToHandSlot(int characterItemId, int handSlot);
    bool EquipToHandSlotWithSwap(int characterItemId, int handSlot);
    bool EquipToBeltWithSwap(int characterItemId, int hoveredSlot);
    void RepairLimbSetState();

    // === FATIGUE ===
    int FatigueCount { get; set; }
    int FatigueBarMax { get; }
    int FatigueInBar { get; }
    int FatigueInInventory { get; }
    int FatigueTotal { get; }   // FatigueCount adjusted by persistent Fatigue (Current) effects
    void AddFatigue(int amount);
    void ClearFatigue();

    // === PACK DEFINITIONS ===
    int InventorySlotsUsed { get; }
    int InventorySlotsTotal { get; }
    int InventorySlotsAvailable { get; }
    bool IsEncumbered { get; }
    void UpdateEncumbranceState();
    bool IsPackDropped { get; }
    void SetPackDropped(bool dropped);

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

    // === FEATURES & RESOURCES ===
    Dictionary<int, int> ClassSelections { get; set; }
    int Favour { get; set; }
    int DaysPassed { get; set; }
    int ShortRestsTaken { get; set; }
    List<FeatureUseRecord> FeatureUses { get; set; }
    TransformState? Transform { get; set; }
    List<SavedForm> SavedForms { get; set; }
    int GetDicePoolSize(DicePool pool);
    bool UsesFavour { get; }

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
    // startSession: false loads the character without starting a play session or purging its recoverable items
    // (the GM's tools use that to change a saved character without playing it).
    Task PopulateFromSaveDataAsync(CharacterSaveData saveData, bool startSession = true);
    CharacterSaveData CreateSaveData();

    // ====================================== LIVE GAME SERVICES ====================================== //

    // === INVENTORY SYSTEM ===
    List<CharacterItem> Inventory { get; set; }
    List<CharacterItem> EquippedItems { get; }
    List<CharacterItem> DroppedItems { get; }

    CharacterItem? GetEquippedHandSlot(int slot);
    CharacterItem? GetEquippedBeltSlot(int slot);
    CharacterItem? GetEquippedArmor();
    CharacterItem AddItemToInventory(Item template, int quantity = 1, int? writtenValueInChips = null, int? startingUses = null, int? startingUsageDie = null);
    void RemoveItemFromInventory(int characterItemId);
    void UpdateItemUses(int characterItemId, int usesRemaining);
    List<CharacterItem> GetUsageDieItems();
    Task<bool> IsTemplateOwnedAsync(int templateId);
    void RecordItemDefinitionEdit(Item template, IReadOnlyList<string> changes, bool ruleSkated);
    void RecordItemEditRequested(Item template, IReadOnlyList<string> changes);
    UsageDieOutcome? ResolveUsageDieRoll(int characterItemId, int roll);
    bool ExpendInventoryItem(int characterItemId, int quantity);
    bool EditItem(int characterItemId, string? newPlayerNote, int? newWrittenValueInChips);
    void EquipItem(int characterItemId, int slot);
    void EquipItemAsArmor(int characterItemId);
    void UnequipItem(int characterItemId);
    void DropItem(int characterItemId);
    void DropItem(int characterItemId, int quantity);
    StackMergeResult MergeItemStacks(int sourceItemId, int targetItemId);
    void RestoreDroppedItem(int characterItemId);
    void TransferItemToPlayer(int characterItemId, int targetCharacterId);
    ItemTransferPacket? BuildTransferPacket(int characterItemId, int quantity, TransferRecipient recipient);
    void CompleteTransferOut(int characterItemId, ItemTransferPacket packet);
    Task<bool> ReceiveItemTransferAsync(ItemTransferPacket packet);

    // === CONVERT STARTING ITEMS TO INVENTORY ===
    void InitializeInventoryFromStartingItems();

    List<CharacterEffect> ActiveEffects { get; set; }
    void AddEffect(CharacterEffect effect);
    void RemoveEffect(Guid effectId);
    void EditEffect(CharacterEffect updatedEffect);
    int GetEffectModifierSum(EffectTargetType target, string? skillName = null);
    int BaseHitpoints { get; }
    int Direction(int modifiedValue, Func<int> baselineGetter);
    int SkillDirection(string skillName);
    string CharacterStory { get; set; }
    Task SaveNowAsync();

    // === SENT BY THE GM (waiting for the player to take them on) ===
    List<IncomingDamage> PendingDamage { get; set; }
    List<IncomingEffect> PendingEffects { get; set; }
    void QueueIncomingDamage(IncomingDamage damage);
    void QueueIncomingEffect(IncomingEffect effect);
    void RemoveIncomingDamage(Guid id, bool ignored);   // ignored = the player dismissed it; logged as rule skating
    void RemoveIncomingEffect(Guid id, bool ignored);

    // === CURRENCY ===
    long CurrencyTotalChips { get; }
    // === SESSION ===
    Guid SessionId { get; }
    void StartSession(Guid? sessionId = null);
    Task EndSessionAsync();


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

    public List<Familiar> AcquiredFamiliars
    {
        get => _acquiredFamiliars;
        set
        {
            _acquiredFamiliars = value ?? new List<Familiar>();
            foreach (var f in _acquiredFamiliars) f.EnsureInstanceId();
        }
    }

    // Live familiar state is addressed by InstanceId (the character's own copy),
    // never by the template Id, which is shared and can change on custom familiars.
    public Familiar? GetFamiliar(Guid instanceId)
        => _acquiredFamiliars.FirstOrDefault(f => f.InstanceId == instanceId);

    public virtual bool AddItemToFamiliarInventory(Guid familiarInstanceId, Item template, int quantity = 1, int? writtenValueInChips = null, int? startingUses = null, int? startingUsageDie = null)
    {
        var familiar = GetFamiliar(familiarInstanceId);
        if (familiar == null || familiar.IsDead || !familiar.AddItemToInventory(template, quantity, writtenValueInChips, startingUses, startingUsageDie)) return false;

        LogFamiliarEvent(PlayerActionType.ItemBestowed,
            $"Bestowed {quantity}x {template.Name} to familiar {familiar.DisplayName}",
            template.Name, template.Id);
        _ = TriggerAutosaveAsync();
        return true;
    }

    public bool RemoveItemFromFamiliarInventory(Guid familiarInstanceId, int characterItemId)
    {
        var familiar = GetFamiliar(familiarInstanceId);
        if (familiar == null || !familiar.RemoveItemFromInventory(characterItemId)) return false;
        _ = TriggerAutosaveAsync();
        return true;
    }

    public void AdjustFamiliarFatigue(Guid familiarInstanceId, int delta)
    {
        var familiar = GetFamiliar(familiarInstanceId);
        if (familiar == null) return;
        familiar.FatigueCount = Math.Max(0, familiar.FatigueCount + delta);
        _ = TriggerAutosaveAsync();
    }

    public bool SetFamiliarItemEquipped(Guid familiarInstanceId, int characterItemId, bool equipped)
    {
        var familiar = GetFamiliar(familiarInstanceId);
        if (familiar == null || familiar.IsDead || !familiar.SetEquipped(characterItemId, equipped)) return false;
        _ = TriggerAutosaveAsync();
        return true;
    }

    private void LogFamiliarEvent(PlayerActionType type, string summary, string? itemName = null, int? itemId = null)
    {
        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = type,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            ItemName = itemName,
            ItemId = itemId,
            Summary = summary
        });
    }

    public void AdjustFamiliarAbility(Guid familiarInstanceId, FamiliarStat stat, int delta)
    {
        var familiar = GetFamiliar(familiarInstanceId);
        if (familiar == null || familiar.IsDead) return;

        familiar.AdjustStat(stat, delta);

        if (familiar.IsDead)
            LogFamiliarEvent(PlayerActionType.FamiliarDied, $"{familiar.DisplayName} died (Vigor reached 0)");

        _ = TriggerAutosaveAsync();
    }

    public void AdjustFamiliarHitpoints(Guid familiarInstanceId, int delta)
    {
        var familiar = GetFamiliar(familiarInstanceId);
        if (familiar == null || familiar.IsDead) return;
        familiar.AdjustHitpoints(delta);
        _ = TriggerAutosaveAsync();
    }

    public bool HandItemToFamiliar(int characterItemId, Guid familiarInstanceId, int quantity)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        var familiar = GetFamiliar(familiarInstanceId);
        if (item == null || item.Template == null || familiar == null) return false;
        if (item.IsEquipped || IsPackDropped) return false;
        if (familiar.IsDead) return false;

        quantity = Math.Clamp(quantity, 1, item.Quantity);
        string name = item.DisplayName;
        int templateId = item.TemplateId;

        familiar.ReceiveItem(item, quantity);
        ItemRecordTransfer.RemoveQuantity(_inventory, item, quantity);

        LogFamiliarEvent(PlayerActionType.ItemHandedToFamiliar,
            $"Handed {quantity}x {name} to {familiar.DisplayName}", name, templateId);
        _ = TriggerAutosaveAsync();
        return true;
    }

    public bool TakeItemFromFamiliar(Guid familiarInstanceId, int familiarItemId, int quantity)
    {
        var familiar = GetFamiliar(familiarInstanceId);
        var item = familiar?.Inventory.FirstOrDefault(i => i.Id == familiarItemId);
        if (familiar == null || item == null || item.Template == null) return false;
        if (IsPackDropped) return false;

        quantity = Math.Clamp(quantity, 1, item.Quantity);
        string name = item.DisplayName;
        int templateId = item.TemplateId;

        ItemRecordTransfer.MoveInto(_inventory, item, quantity, GetNextCharacterItemId);
        ItemRecordTransfer.RemoveQuantity(familiar.Inventory, item, quantity);

        LogFamiliarEvent(PlayerActionType.ItemTakenFromFamiliar,
            $"Took {quantity}x {name} from {familiar.DisplayName}", name, templateId);
        _ = TriggerAutosaveAsync();
        return true;
    }

    // A Shifter's form pack was left behind: every item becomes a dropped item the player can recover.
    // New records are made on purpose, so nothing merges into (or drops) the character's own stacks.
    public int DropFormPackItems(Guid familiarInstanceId)
    {
        var familiar = GetFamiliar(familiarInstanceId);
        if (familiar == null) return 0;

        int count = 0;
        foreach (var item in familiar.Inventory.Where(i => !i.IsDropped && i.Template != null).ToList())
        {
            var dropped = new CharacterItem
            {
                Id = GetNextCharacterItemId(),
                InstanceId = Guid.NewGuid(),
                TemplateId = item.TemplateId,
                TemplateGuid = item.TemplateGuid,
                Template = item.Template,
                Quantity = item.Quantity,
                RemainingUses = item.RemainingUses,
                UsageDieSides = item.UsageDieSides,
                IsEmpty = item.IsEmpty,
                IsIdentified = item.IsIdentified,
                CustomName = item.CustomName,
                PlayerNote = item.PlayerNote,
                WrittenValueInChips = item.WrittenValueInChips,
                AcquiredAt = item.AcquiredAt,
                LastModified = DateTime.Now,
                IsDropped = true,
                DroppedAt = DateTime.Now
            };

            _droppedItems.Add(dropped);
            _ = _actionLogService.LogItemDroppedAsync(dropped, PlayerName, PlayerId, CharacterName, CharacterGuid);
            if (!IsReadOnly) _ = _recoveryService.AddRecoverableAsync(dropped, PlayerId, SessionId);
            count++;
        }

        familiar.Inventory.Clear();
        _ = TriggerAutosaveAsync();
        return count;
    }

    public void RestoreFamiliar(Guid familiarInstanceId)
    {
        var familiar = GetFamiliar(familiarInstanceId);
        if (familiar == null || familiar.IsDead) return;
        familiar.RestoreAll();
        _ = TriggerAutosaveAsync();
    }

    public void AddFamiliar(Familiar familiar)
    {
        // Each character gets its own copy so live state never leaks into the shared template.
        if (familiar != null && !HasFamiliar(familiar.Id))
        {
            _acquiredFamiliars.Add(familiar.CreateInstanceCopy());
        }
    }

    // GM bestowal: always gives the character its own fresh copy, so duplicates are allowed.
    public virtual Familiar BestowFamiliar(Familiar template)
    {
        var instance = template.CreateInstanceCopy();
        _acquiredFamiliars.Add(instance);
        _ = TriggerAutosaveAsync();
        LogFamiliarEvent(PlayerActionType.FamiliarBestowed, $"Bestowed familiar: {instance.DisplayName}");
        return instance;
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

    public void SetFamiliarGivenName(Guid familiarInstanceId, string? name)
    {
        var familiar = GetFamiliar(familiarInstanceId);
        if (familiar == null) return;
        familiar.FmlrPlayerName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        _ = TriggerAutosaveAsync();
    }

    public bool RemoveFamiliar(Guid familiarInstanceId)
    {
        var familiar = GetFamiliar(familiarInstanceId);
        if (familiar == null) return false;

        LogFamiliarEvent(PlayerActionType.FamiliarReleased,
            familiar.IsDead ? $"Removed dead familiar: {familiar.DisplayName}"
                            : $"Released familiar: {familiar.DisplayName}");

        _acquiredFamiliars.Remove(familiar);
        _ = TriggerAutosaveAsync();
        return true;
    }

    // === BACKGROUND DATA ===
    public int? SelectedBackgroundId { get; set; }
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
    public int UnspentTrainingPoints { get; set; }
    // Training points the character's classes grant (the Rogue's recurring benefit), times picked.
    public int ClassTrainingPointsGranted => ClassSelections.Sum(pick =>
        (_classDataService.GetClassByIdAsync(pick.Key).Result?.TrainingPointsPerSelection ?? 0) * pick.Value);

    // The 4 every character starts with, plus class benefits. The Hinderance step builds on this
    // (a hinderance's training point reward is added to it), so a class benefit survives its resets.
    public int CreationTrainingPointsBase => 4 + ClassTrainingPointsGranted;
    private bool _creationTrainingMigrated;

    // ===== LEVEL =====
    public int PendingLevels { get; set; }
    // "Roll for me" at character creation: one roll per character.
    public List<int> StatRolls { get; set; } = new();
    public List<int> StatRollPool { get; set; } = new();

    // Every pick of a class counts once; creating the character was the first one.
    public int CharacterLevel => ClassSelections.Values.Sum();

    public string ClassPicksSummary => string.Join(" · ", ClassSelections
        .Where(p => p.Value > 0)
        .Select(p => $"{_classDataService.GetClassByIdAsync(p.Key).Result?.ClassName ?? "Unknown"} ×{p.Value}"));

    // Adds up one class benefit over every pick of every class. A character with no recorded picks
    // (an old save, before it is loaded) falls back to the original class counted once.
    private int ClassPickSum(Func<CharacterClass, int> benefit)
    {
        if (ClassSelections.Count == 0)
            return GetSelectedClass() is { } original ? benefit(original) : 0;

        int total = 0;
        foreach (var (classId, picks) in ClassSelections)
            if (_classDataService.GetClassByIdAsync(classId).Result is { } cls) total += benefit(cls) * picks;
        return total;
    }

    // Points a player didn't spend during character creation carry over into the live-game pool.
    // AvailableTrainingPoints is the creation total, so the leftover is that total minus what the skills already hold.
    // The creation total is then set to the amount spent, which keeps the carried-over points from being counted twice.
    public void MigrateCreationTrainingPoints()
    {
        if (_creationTrainingMigrated) return;

        int spent = SkillTrainingLevels.Values.Where(v => v > 0).Sum();
        int leftover = Math.Max(0, AvailableTrainingPoints - spent);

        UnspentTrainingPoints += leftover;
        AvailableTrainingPoints = spent;
        _creationTrainingMigrated = true;

        System.Diagnostics.Debug.WriteLine(
            $"[TRAINING] Carried {leftover} unspent creation point(s) into the live pool (skills hold {spent}).");
    }
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
    public int NormalTotalStatVigor
    {
        get
        {
            int baseStat = StatVigor ?? 10;
            int bgModifier = GetBackgroundModifier("Vigor");
            int hindModifier = GetHinderanceModifier("Vigor");
            int hindReward = (RewardStatName == "Vigor") ? RewardStatBonusAmount : 0;
            int raceModifier = GetRaceModifier("Vigor");
            int effectModifier = GetEffectModifierSum(EffectTargetType.VigorMax);
            return baseStat + bgModifier + hindModifier + hindReward + raceModifier + effectModifier;
        }
    }

    // While transformed, the form's VIG replaces the character's (Fatigue, HP, skill checks and saves).
    public int TotalStatVigor => Transform is { IsTransformed: true } t ? t.FormVigor : NormalTotalStatVigor;
    public int PackVigorModifier => GetAbilityModifier(NormalTotalStatVigor);

    public int NormalTotalStatAgility
    {
        get
        {
            int baseStat = StatAgility ?? 10;
            int bgModifier = GetBackgroundModifier("Agility");
            int hindModifier = GetHinderanceModifier("Agility");
            int hindReward = (RewardStatName == "Agility") ? RewardStatBonusAmount : 0;
            int raceModifier = GetRaceModifier("Agility");
            int effectModifier = GetEffectModifierSum(EffectTargetType.AgilityMax);
            return baseStat + bgModifier + hindModifier + hindReward + raceModifier + effectModifier;
        }
    }

    public int TotalStatAgility => Transform is { IsTransformed: true } t ? t.FormAgility : NormalTotalStatAgility;

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
            int effectModifier = GetEffectModifierSum(EffectTargetType.MindMax);

            return baseStat + bgModifier + hindModifier + hindReward + classModifier + raceModifier + effectModifier;
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
            int effectModifier = GetEffectModifierSum(EffectTargetType.SpiritMax);

            return baseStat + bgModifier + hindModifier + hindReward + classModifier + raceModifier + effectModifier;
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

    // === CURRENT ASM (modifier from the CURRENT stats) ===
    public int CurrentASMStatVigor => GetAbilityModifier(CurrentStatVigor);
    public int CurrentASMStatAgility => GetAbilityModifier(CurrentStatAgility);
    public int CurrentASMStatMind => GetAbilityModifier(CurrentStatMind);
    public int CurrentASMStatSpirit => GetAbilityModifier(CurrentStatSpirit);

    // === STAT-SKILL MAPPING ===
    public int GetSkillTotal(string skillName)
    {
        int trainingPoints = SkillTrainingLevels.ContainsKey(skillName) ? SkillTrainingLevels[skillName] : 0;
        int statBonus = GetStatBonusForSkill(skillName);
        int bgBonus = GetBackgroundSkillBonus(skillName);
        int raceBonus = GetRaceSkillBonus(skillName);
        int hindBonus = GetHinderanceSkillModifier(skillName);
        int effectBonus = GetEffectModifierSum(EffectTargetType.Skill, skillName);
        int classBonus = 0;
        int basePenalty = -2;

        return basePenalty + trainingPoints + statBonus + bgBonus + raceBonus + hindBonus + effectBonus + classBonus;
    }

    public SkillModifierBreakdown GetSkillBreakdown(string skillName)
    {
        int trainingPoints = SkillTrainingLevels.ContainsKey(skillName) ? SkillTrainingLevels[skillName] : 0;
        int statBonus = GetStatBonusForSkill(skillName);
        int bgBonus = GetBackgroundSkillBonus(skillName);
        int raceBonus = GetRaceSkillBonus(skillName);
        int hindBonus = GetHinderanceSkillModifier(skillName);
        int effectBonus = GetEffectModifierSum(EffectTargetType.Skill, skillName);
        int classBonus = 0;

        return new SkillModifierBreakdown
        {
            SkillName = skillName,
            NaturalNegative = -2,
            ASM = statBonus,
            TrainingPoints = trainingPoints,
            OtherModifiers = bgBonus + raceBonus + hindBonus + effectBonus + classBonus
        };
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

    private int GetHinderanceSkillModifier(string skillName)
    {
        if (SelectedHinderance?.SkillModifiers == null) return 0;
        var modifier = SelectedHinderance.SkillModifiers.FirstOrDefault(m => m.SkillName == skillName);
        return modifier?.Modifier ?? 0;
    }

    // Which ability score governs a skill ("" for an unknown skill).
    public string GetSkillStatName(string skillName) => skillName switch
    {
        "Athletics" or "Constitution" or "Grapple" or "Presence" or "Ride" => "Vigor",
        "Acrobatics" or "Aim" or "Drive" or "Stealth" or "Thief" => "Agility",
        "Arcana" or "Artifice" or "Heal" or "Investigate" or "Lore" => "Mind",
        "Commune" or "Deception" or "Diplomacy" or "Sight" or "Survival" => "Spirit",
        _ => string.Empty
    };

    // The skill's modifier from its stat: the CURRENT value (what rolls use) or the MAX value.
    private int GetStatBonusForSkill(string skillName, bool useCurrent = true)
        => GetSkillStatName(skillName) switch
        {
            "Vigor" => useCurrent ? CurrentASMStatVigor : TotalASMStatVigor,
            "Agility" => useCurrent ? CurrentASMStatAgility : TotalASMStatAgility,
            "Mind" => useCurrent ? CurrentASMStatMind : TotalASMStatMind,
            "Spirit" => useCurrent ? CurrentASMStatSpirit : TotalASMStatSpirit,
            _ => 0
        };

    public int GetSkillMaxASM(string skillName) => GetStatBonusForSkill(skillName, useCurrent: false);

    // === PRIVATE FIELDS ===
    private readonly IClassDataService _classDataService;
    private readonly IItemDataService _itemDataService;
    private readonly IPlayerActionLogService _actionLogService;
    private readonly IItemRecoveryService _recoveryService;
    private readonly ICharacterPersistenceService _persistenceService;
    private List<Quip> _acquiredQuips = new();
    private List<Spell> _acquiredSpells = new();
    private List<Technique> _acquiredTechniques = new();
    private List<Blueprint> _acquiredBlueprints = new();

    // === INVENTORY ===
    private List<CharacterItem> _inventory = new();
    private List<CharacterItem> _droppedItems = new();
    private int _nextCharacterItemId = 1;

    public CharAttribDataService(IClassDataService classDataService, IPlayerActionLogService actionLogService, IItemDataService itemDataService, IItemRecoveryService recoveryService, ICharacterPersistenceService persistenceService)
    {
        _actionLogService = actionLogService;
        _persistenceService = persistenceService;
        _classDataService = classDataService;
        _recoveryService = recoveryService;
        _itemDataService = itemDataService;
    }

    // True for the GM's Player Viewer: the character is looked at, never saved.
    public bool IsReadOnly { get; set; }

    // Fires after every logged event. If this becomes a performance problem with
    // frequent events, switch to an interval-based save instead — flagged by the developer.
    private async Task TriggerAutosaveAsync()
    {
        if (IsReadOnly) return;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrEmpty(FileName))
            {
                FileName = await _persistenceService.GenerateFileName(PlayerName, CharacterName);
            }

            var saveData = CreateSaveData();
            saveData.FileName = FileName;
            saveData.LastSaved = DateTime.Now;

            await _persistenceService.SaveCharacterDataAsync(saveData);
            System.Diagnostics.Debug.WriteLine($"[PERF] autosave: {sw.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Autosave error: {ex.Message}");
        }
    }

    public async Task<List<CharacterItem>> GetRecoverableItemsAsync()
    {
        var items = await _recoveryService.GetRecoverablesForSessionAsync(PlayerId, SessionId);
        foreach (var item in items) await ResolveItemTemplateAsync(item);
        return items;
    }

    public async Task<bool> RecoverItemAsync(Guid instanceId)
    {
        int newId = GetNextCharacterItemId();
        var recovered = await _recoveryService.RecoverItemAsync(instanceId, newId);
        if (recovered == null) return false;

        await ResolveItemTemplateAsync(recovered);   // templates aren't serialized

        if (recovered.Template == null)
            _inventory.Add(recovered);   // template unresolved: keep it as its own record rather than lose it
        else
            ItemRecordTransfer.MoveInto(_inventory, recovered, recovered.Quantity, GetNextCharacterItemId);
        return true;
    }

    // ===== SESSION =====
    // One play session = one character load (solo) or one GM-hosted session. Runtime only: never saved.
    // Dropped items are recoverable only within the session they were dropped in.
    private Guid _sessionId = Guid.Empty;

    // Created on first use, so every route into the live game has one.
    public Guid SessionId
    {
        get
        {
            if (_sessionId == Guid.Empty)
            {
                _sessionId = Guid.NewGuid();
                _actionLogService.CurrentSessionId = _sessionId;
            }
            return _sessionId;
        }
    }

    // Pass the GM's id to join a hosted session; otherwise a fresh one is generated.
    public void StartSession(Guid? sessionId = null)
    {
        _sessionId = sessionId ?? Guid.NewGuid();
        _actionLogService.CurrentSessionId = _sessionId;
    }

    // Leaving the session: anything this player dropped can no longer be recovered.
    public async Task EndSessionAsync()
    {
        await _recoveryService.PurgePlayerRecoverablesAsync(PlayerId, null);
        _sessionId = Guid.Empty;
        _actionLogService.CurrentSessionId = null;
    }

    private async Task ResolveItemTemplateAsync(CharacterItem item)
    {
        if (item.Template != null) return;
        try
        {
            item.Template = item.TemplateGuid.HasValue
                ? await _itemDataService.GetItemByGuidAsync(item.TemplateGuid.Value)
                : await _itemDataService.GetItemByIdAsync(item.TemplateId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error resolving recovered item template {item.TemplateId}/{item.TemplateGuid}: {ex.Message}");
        }
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
        // GetEquippedHandSlot now reports an item at every slot it spans, so each
        // hit is one occupied slot. Summing SlotsRequired here would double-count.
        int usedSlots = 0;
        for (int i = 1; i <= GetTotalLimbSlots(); i++)
        {
            if (GetEquippedHandSlot(i) != null)
            {
                usedSlots++;
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
    public Guid CharacterGuid { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public WeaponProficiencyState Proficiency { get; set; } = new();
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

    // === LIVE-PLAY CURRENT VALUES ===
    // Stored as offsets from the computed totals, so they stay correct if a total changes.
    public int HitpointsDamage { get; set; } = 0;
    public int CurrentHitpoints => Math.Max(0, Hitpoints - HitpointsDamage + GetEffectModifierSum(EffectTargetType.HitpointsCurrent));

    // Ability score loss is stored as a value <= 0 (0 = no loss).
    public int CurrentVigorAdjustment { get; set; } = 0;
    public int CurrentAgilityAdjustment { get; set; } = 0;
    public int CurrentMindAdjustment { get; set; } = 0;
    public int CurrentSpiritAdjustment { get; set; } = 0;

    public int CurrentStatVigor => Math.Max(0, TotalStatVigor + CurrentVigorAdjustment + GetEffectModifierSum(EffectTargetType.VigorCurrent));
    public int CurrentStatAgility => Math.Max(0, TotalStatAgility + CurrentAgilityAdjustment + GetEffectModifierSum(EffectTargetType.AgilityCurrent));
    public int CurrentStatMind => Math.Max(0, TotalStatMind + CurrentMindAdjustment + GetEffectModifierSum(EffectTargetType.MindCurrent));
    public int CurrentStatSpirit => Math.Max(0, TotalStatSpirit + CurrentSpiritAdjustment + GetEffectModifierSum(EffectTargetType.SpiritCurrent));

    public int ArmorRating
    {
        get
        {
            var equippedArmor = Inventory
                .Where(i => !i.IsDropped && (i.Template?.IsArmor ?? false) &&
                    (i.EquipmentSlot == EquipmentSlotType.Armor ||
                     (i.EquipmentSlot == EquipmentSlotType.Hand && (i.Template?.IsShield ?? false))))
                .ToList();

            bool wearingHeavy = equippedArmor.Any(i => i.Template?.ArmorType == ArmorType.Heavy);

            int total = 0;
            foreach (var item in equippedArmor)
            {
                bool isShield = item.Template?.IsShield ?? false;
                if (isShield && wearingHeavy) continue;
                total += item.Template?.ArmorValue ?? 0;
            }

            if (Transform is { IsTransformed: true } t) total += t.Armor;
            return Math.Min(3, total);
        }
    }

    public DamageResult ApplyDamage(int amount, bool bypassHitProtection = false)
    {
        if (amount <= 0) return new DamageResult();

        int armor = ArmorRating;
        int mitigated = Math.Max(0, amount - armor);

        if (mitigated <= 0)
        {
            _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
            {
                ActionType = PlayerActionType.DamageTaken,
                PlayerName = PlayerName,
                PlayerId = PlayerId,
                CharacterName = CharacterName,
                CharacterGuid = CharacterGuid,
                Summary = $"Took {amount} damage, fully absorbed by Armor ({armor})"
            });
            _ = TriggerAutosaveAsync();
            return new DamageResult { ArmorMitigated = amount };
        }

        int hpLost = bypassHitProtection ? 0 : Math.Min(mitigated, CurrentHitpoints);
        int overflow = mitigated - hpLost;

        HitpointsDamage += hpLost;

        int vigorBefore = CurrentStatVigor;
        if (overflow > 0)
        {
            AdjustCurrentStat("vigor", -overflow);   // logs VigorLost / CharacterDied, clamped internally
        }
        int vigorLost = vigorBefore - CurrentStatVigor;

        bool triggersVigorSave = !bypassHitProtection && overflow > 0;
        if (triggersVigorSave)
        {
            AddVigorSaveReminder();
        }

        string armorNote = armor > 0 ? $", {armor} absorbed by Armor" : "";
        string summary = bypassHitProtection
            ? $"Took {amount} damage{armorNote}, {mitigated} to Vigor ({vigorLost} lost)"
            : overflow > 0
                ? $"Took {amount} damage{armorNote}: {hpLost} to Hit Protection, {vigorLost} to Vigor (Vigor save required)"
                : $"Took {amount} damage{armorNote}, fully absorbed by Hit Protection";

        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.DamageTaken,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = summary
        });

        _ = TriggerAutosaveAsync();

        return new DamageResult
        {
            HitProtectionLost = hpLost,
            VigorLost = vigorLost,
            ArmorMitigated = armor,
            TriggersVigorSave = triggersVigorSave,
            IsDead = CurrentStatVigor <= 0
        };
    }

    public void ApplyParryLoss(int attackerRoll)
    {
        if (attackerRoll <= 0) return;
        AdjustCurrentStat("vigor", -attackerRoll);   // direct to Vigor, bypasses armor and HP, no save

        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.CombatReactionUsed,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = $"Parry lost — {attackerRoll} damage dealt directly to Vigor (no save, no armor)"
        });
        _ = TriggerAutosaveAsync();
    }

    public void LogCombatReaction(CombatReactionType reaction, string? note = null)
    {
        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.CombatReactionUsed,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = note ?? $"Used reaction: {reaction}"
        });
        _ = TriggerAutosaveAsync();
    }

    public void LogSkillCheck(string skillName, int diceTotal, int modifier, string advantageMode = "None", int? advantageDieResult = null)
    {
        int adjustment = advantageMode switch
        {
            "Advantage" => advantageDieResult ?? 0,
            "Disadvantage" => -(advantageDieResult ?? 0),
            _ => 0
        };
        int total = diceTotal + modifier + adjustment;

        string summary = advantageMode == "None"
            ? $"{skillName} check: 2d6 ({diceTotal}) + {modifier} = {total}"
            : $"{skillName} check: 2d6 ({diceTotal}) {(adjustment >= 0 ? "+" : "-")}1d4 {advantageMode} ({advantageDieResult}) + {modifier} = {total}";

        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.SkillCheckRolled,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = summary
        });
        _ = TriggerAutosaveAsync();
    }

    public void LogAbilitySave(string statName, int roll, bool success)
    {
        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.AbilitySaveRolled,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = $"{statName} save: rolled {roll} — {(success ? "passed" : "failed")}"
        });
        _ = TriggerAutosaveAsync();
    }

    public bool HasPendingVigorSave => ActiveEffects.Any(e => e.HardcodedType == HardcodedEffectType.VigorSave);
    public bool HasDeathsDoor => ActiveEffects.Any(e => e.HardcodedType == HardcodedEffectType.DeathsDoor);

    public void AddVigorSaveReminder()
    {
        if (HasPendingVigorSave) return;   // don't stack duplicate reminders

        ActiveEffects.Add(new CharacterEffect
        {
            Name = "VIG SAVE",
            HardcodedType = HardcodedEffectType.VigorSave,
            Note = "Tap to roll your Vigor Save."
        });

        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.VigorSaveTriggered,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = "Vigor Save required"
        });
        _ = TriggerAutosaveAsync();
    }

    public void ResolveVigorSave(bool success)
    {
        var pending = ActiveEffects.FirstOrDefault(e => e.HardcodedType == HardcodedEffectType.VigorSave);
        if (pending != null) ActiveEffects.Remove(pending);

        if (success)
        {
            _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
            {
                ActionType = PlayerActionType.VigorSavePassed,
                PlayerName = PlayerName,
                PlayerId = PlayerId,
                CharacterName = CharacterName,
                CharacterGuid = CharacterGuid,
                Summary = "Vigor Save passed"
            });
        }
        else
        {
            if (!HasDeathsDoor)
            {
                ActiveEffects.Add(new CharacterEffect
                {
                    Name = "DEATH'S DOOR",
                    HardcodedType = HardcodedEffectType.DeathsDoor,
                    Note = "Tap to clear once resolved."
                });
            }

            _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
            {
                ActionType = PlayerActionType.VigorSaveFailed,
                PlayerName = PlayerName,
                PlayerId = PlayerId,
                CharacterName = CharacterName,
                CharacterGuid = CharacterGuid,
                Summary = "Vigor Save failed — Death's Door"
            });
        }

        _ = TriggerAutosaveAsync();
    }

    public void ClearDeathsDoor()
    {
        var effect = ActiveEffects.FirstOrDefault(e => e.HardcodedType == HardcodedEffectType.DeathsDoor);
        if (effect == null) return;

        ActiveEffects.Remove(effect);

        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.DeathsDoorCleared,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = "Death's Door cleared"
        });
        _ = TriggerAutosaveAsync();
    }

    public void RestoreHitProtection()
    {
        ShortRestsTaken++;

        if (IsDeprived)
        {
            // A deprived character can't recover HP.
            _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
            {
                ActionType = PlayerActionType.ShortRestTaken,
                PlayerName = PlayerName,
                PlayerId = PlayerId,
                CharacterName = CharacterName,
                CharacterGuid = CharacterGuid,
                Summary = "Took a short rest — deprived, Hit Protection not restored"
            });
            _ = TriggerAutosaveAsync();
            return;
        }

        HitpointsDamage = 0;
        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ShortRestTaken,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = "Took a short rest — Hit Protection restored"
        });
        _ = TriggerAutosaveAsync();
    }

    // Only a Long Rest grants the Rested chip. Rest types per the rulebook: Perilous restores Hit
    // Protection; Normal also clears Fatigue; Comfortable also restores all four ability scores by
    // one d6. A deprived character whose needs are NOT met gets the Rested chip but no recovery at
    // all. If their needs are met, the rest clears deprivation first and then recovers normally.
    public LongRestResult TakeLongRest(LongRestType type, int? comfortableRoll = null)
    {
        ShortRestsTaken++;
        Rested = true;

        bool recoveryBlocked = IsDeprived && !NeedsMet;
        bool clearsFatigue = false;
        int? roll = null;
        string summary;

        if (recoveryBlocked)
        {
            summary = $"Took a long rest ({type}) — Rested, but deprived with needs unmet: no recovery";
        }
        else
        {
            ClearDeprivationIfEligible();

            HitpointsDamage = 0;
            clearsFatigue = type == LongRestType.Normal || type == LongRestType.Comfortable;

            if (clearsFatigue) ClearFatigue();   // logs its own event and autosaves

            if (type == LongRestType.Comfortable)
            {
                roll = Math.Clamp(comfortableRoll ?? Random.Shared.Next(1, 7), 1, 6);   // six-sided die: 1-6
                AdjustCurrentStat("vigor", roll.Value);
                AdjustCurrentStat("agility", roll.Value);
                AdjustCurrentStat("mind", roll.Value);
                AdjustCurrentStat("spirit", roll.Value);
            }

            string detail = type switch
            {
                LongRestType.Normal => "Hit Protection restored, fatigue cleared",
                LongRestType.Comfortable => $"Hit Protection restored, fatigue cleared, rolled a {roll} on a six-sided die to restore all ability scores",
                _ => "Hit Protection restored"
            };
            summary = $"Took a long rest ({type}) — {detail}";
        }

        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.LongRestTaken,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = summary
        });
        _ = TriggerAutosaveAsync();

        return new LongRestResult { Type = type, FatigueCleared = clearsFatigue, AbilityScoreRestoreRoll = roll };
    }

    // delta < 0 = ability score loss; delta > 0 = restoration. Never above the total
    // (the character's "native" score) or below 0. Logs the loss/restoration, and a
    // debilitation event the moment the stat crosses to 0.
    public void AdjustCurrentStat(string statName, int delta)
    {
        if (delta == 0) return;

        int beforeValue = GetCurrentForStat(statName);

        switch (statName.ToLower())
        {
            case "vigor": CurrentVigorAdjustment = ClampAbilityLoss(CurrentVigorAdjustment + delta, TotalStatVigor); break;
            case "agility": CurrentAgilityAdjustment = ClampAbilityLoss(CurrentAgilityAdjustment + delta, TotalStatAgility); break;
            case "mind": CurrentMindAdjustment = ClampAbilityLoss(CurrentMindAdjustment + delta, TotalStatMind); break;
            case "spirit": CurrentSpiritAdjustment = ClampAbilityLoss(CurrentSpiritAdjustment + delta, TotalStatSpirit); break;
            default: return;
        }

        int afterValue = GetCurrentForStat(statName);
        int actualDelta = afterValue - beforeValue;
        if (actualDelta == 0) return;

        if (actualDelta > 0)
        {
            _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
            {
                ActionType = PlayerActionType.AbilityScoreRestored,
                PlayerName = PlayerName,
                PlayerId = PlayerId,
                CharacterName = CharacterName,
                CharacterGuid = CharacterGuid,
                Summary = $"Restored {actualDelta} {statName} (now {afterValue})"
            });
        }
        else
        {
            _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
            {
                ActionType = GetLossActionType(statName),
                PlayerName = PlayerName,
                PlayerId = PlayerId,
                CharacterName = CharacterName,
                CharacterGuid = CharacterGuid,
                Summary = $"Lost {-actualDelta} {statName} (now {afterValue})"
            });

            if (afterValue <= 0 && beforeValue > 0)
            {
                _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
                {
                    ActionType = GetDebilitationActionType(statName),
                    PlayerName = PlayerName,
                    PlayerId = PlayerId,
                    CharacterName = CharacterName,
                    CharacterGuid = CharacterGuid,
                    Summary = GetDebilitationSummary(statName)
                });
            }
        }

        _ = TriggerAutosaveAsync();
    }

    private int GetCurrentForStat(string statName) => statName.ToLower() switch
    {
        "vigor" => CurrentStatVigor,
        "agility" => CurrentStatAgility,
        "mind" => CurrentStatMind,
        "spirit" => CurrentStatSpirit,
        _ => 0
    };

    private static PlayerActionType GetLossActionType(string statName) => statName.ToLower() switch
    {
        "vigor" => PlayerActionType.VigorLost,
        "agility" => PlayerActionType.AgilityLost,
        "mind" => PlayerActionType.MindLost,
        "spirit" => PlayerActionType.SpiritLost,
        _ => PlayerActionType.VigorLost
    };

    private static PlayerActionType GetDebilitationActionType(string statName) => statName.ToLower() switch
    {
        "vigor" => PlayerActionType.CharacterDied,
        "agility" => PlayerActionType.Paralyzed,
        "mind" => PlayerActionType.MadnessOnset,
        "spirit" => PlayerActionType.EmptinessOnset,
        _ => PlayerActionType.CharacterDied
    };

    private static string GetDebilitationSummary(string statName) => statName.ToLower() switch
    {
        "vigor" => "Vigor reduced to 0 — character died",
        "agility" => "Agility reduced to 0 — character is paralyzed",
        "mind" => "Mind reduced to 0 — character has succumbed to madness",
        "spirit" => "Spirit reduced to 0 — character is an empty husk",
        _ => "Ability score reduced to 0"
    };

    private static int ClampAbilityLoss(int adjustment, int total)
    {
        return Math.Clamp(adjustment, -Math.Max(total, 0), 0);
    }

    // === FATIGUE ===
    // Bar size = AGI modifier, minimum 1 (a negative modifier never shrinks it).
    // Fatigue beyond the bar occupies pack slots. Class bonuses to the bar (e.g. Arcanist +1)
    // aren't included yet.
    public int FatigueCount { get; set; } = 0;
    // Rulebook: bar = AGI modifier, minimum 1. Effects apply on top of that floored base.
    public int FatigueBarMax => Math.Max(1, Math.Max(1, TotalASMStatAgility) + GetEffectModifierSum(EffectTargetType.FatigueMax));

    // Effects on Fatigue (Current): Buff (+) removes fatigue, Debuff (-) adds it
    public int FatigueTotal => Math.Max(0, FatigueCount - GetEffectModifierSum(EffectTargetType.FatigueCurrent));
    public int FatigueInBar => Math.Min(FatigueTotal, FatigueBarMax);
    public int FatigueInInventory => Math.Max(0, FatigueTotal - FatigueBarMax);

    public void AddFatigue(int amount)
    {
        FatigueCount = Math.Max(0, FatigueCount + amount);
        if (amount > 0)
        {
            _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
            {
                ActionType = PlayerActionType.FatigueGained,
                PlayerName = PlayerName,
                PlayerId = PlayerId,
                CharacterName = CharacterName,
                CharacterGuid = CharacterGuid,
                Summary = $"Gained {amount} fatigue (now {FatigueCount})"
            });
            _ = TriggerAutosaveAsync();
        }
    }

    public void ClearFatigue()
    {
        if (FatigueCount > 0)
        {
            _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
            {
                ActionType = PlayerActionType.FatigueCleared,
                PlayerName = PlayerName,
                PlayerId = PlayerId,
                CharacterName = CharacterName,
                CharacterGuid = CharacterGuid,
                Summary = $"Cleared {FatigueCount} fatigue"
            });
        }
        FatigueCount = 0;
        _ = TriggerAutosaveAsync();
    }

    // === DEPRIVATION / DAILY NEEDS ===
    // Needs Met: the player has eaten / done their maintenance today (affirmed by hand).
    // Rested: a Long Rest was taken today. Both chips clear when a day ends.
    // A character created fresh starts with Needs Met and no Rested chip.
    public bool NeedsMet { get; set; } = true;
    public bool Rested { get; set; }
    public bool IsDeprived { get; set; }
    public int DeprivedDays { get; set; }

    public void AffirmNeedsMet()
    {
        if (NeedsMet) return;
        NeedsMet = true;

        LogFamiliarEvent(PlayerActionType.DailyNeedsAffirmed, "Affirmed daily needs are met");
        ClearDeprivationIfEligible();
        _ = TriggerAutosaveAsync();
    }

    // Holding BOTH chips on the same day clears deprivation, in either order. The chips stay,
    // so the player never has to rest or eat twice in one day.
    private void ClearDeprivationIfEligible()
    {
        if (!IsDeprived || !NeedsMet || !Rested) return;

        IsDeprived = false;
        DeprivedDays = 0;
        LogFamiliarEvent(PlayerActionType.DeprivationCleared, "No longer deprived (rested and needs met)");
    }

    // A day ends. Missing either chip makes the character Deprived. Deprived for more than a day
    // (already deprived when this day ends, and still is) adds 1 Fatigue per day; the first
    // deprived day adds none. Both chips are then cleared for the new day.
    public DayEndResult EndDay()
    {
        var result = new DayEndResult { MissedNeeds = !NeedsMet, MissedRest = !Rested };

        if (result.MissedNeeds || result.MissedRest)
        {
            bool alreadyDeprived = IsDeprived;
            IsDeprived = true;
            DeprivedDays++;
            result.BecameDeprived = !alreadyDeprived;

            string missing = result.MissedNeeds && result.MissedRest ? "needs not met and no rest"
                           : result.MissedNeeds ? "needs not met" : "no long rest";

            if (alreadyDeprived)
            {
                AddFatigue(1);   // logs its own event
                result.FatigueGained = 1;
                LogFamiliarEvent(PlayerActionType.DeprivationStarted,
                    $"Still deprived at day's end ({missing}) — deprived {DeprivedDays} days, +1 fatigue");
            }
            else
            {
                LogFamiliarEvent(PlayerActionType.DeprivationStarted,
                    $"Became deprived at day's end ({missing})");
            }
        }
        else
        {
            IsDeprived = false;   // safety: both chips held means no deprivation
            DeprivedDays = 0;
        }

        result.StillDeprived = IsDeprived;
        result.DeprivedDays = DeprivedDays;

        NeedsMet = false;
        Rested = false;

        _ = TriggerAutosaveAsync();
        return result;
    }

    public string CharacterStory { get; set; } = string.Empty;

    public async Task SaveNowAsync()
    {
        await TriggerAutosaveAsync();
    }

    public long CurrencyTotalChips => CurrencyHelper.TotalChips(_inventory);

    public List<CharacterEffect> ActiveEffects { get; set; } = new();

    // ===== SENT BY THE GM =====
    // Damage and effects the GM sent that the player hasn't taken on yet. They wait on the character, and in its
    // save, until the player responds from the Apply Damage / Apply Effect screens.
    public List<IncomingDamage> PendingDamage { get; set; } = new();
    public List<IncomingEffect> PendingEffects { get; set; } = new();

    public void QueueIncomingDamage(IncomingDamage damage)
    {
        PendingDamage.Add(damage);

        string speed = damage.Target == DamageTargetType.Hitpoints ? $" ({damage.Speed} attack)" : string.Empty;
        string note = string.IsNullOrWhiteSpace(damage.Note) ? string.Empty : $": {damage.Note}";
        LogFamiliarEvent(PlayerActionType.IncomingDamageSent, $"The GM sent {damage.Amount} {damage.Target} damage{speed}{note}");
        _ = TriggerAutosaveAsync();
    }

    public void QueueIncomingEffect(IncomingEffect effect)
    {
        PendingEffects.Add(effect);

        string note = string.IsNullOrWhiteSpace(effect.Effect.Note) ? string.Empty : $": {effect.Effect.Note}";
        LogFamiliarEvent(PlayerActionType.IncomingEffectSent, $"The GM sent the effect {effect.Effect.Name}{note}");
        _ = TriggerAutosaveAsync();
    }

    // Taken on (ignored = false) or dismissed by the player (ignored = true, which is rule skating).
    public void RemoveIncomingDamage(Guid id, bool ignored)
    {
        var damage = PendingDamage.FirstOrDefault(d => d.Id == id);
        if (damage == null) return;

        PendingDamage.Remove(damage);
        if (ignored) LogFamiliarEvent(PlayerActionType.RuleBypassed, $"Ignored {damage.Summary} the GM sent");
        _ = TriggerAutosaveAsync();
    }

    public void RemoveIncomingEffect(Guid id, bool ignored)
    {
        var effect = PendingEffects.FirstOrDefault(e => e.Id == id);
        if (effect == null) return;

        PendingEffects.Remove(effect);
        if (ignored) LogFamiliarEvent(PlayerActionType.RuleBypassed, $"Ignored the effect {effect.Effect.Name} the GM sent");
        _ = TriggerAutosaveAsync();
    }

    public void AddEffect(CharacterEffect effect)
    {
        if (effect.Duration == EffectDurationType.OneShot)
        {
            ApplyOneShotEffect(effect);
            return;   // One-Shot effects apply immediately and are never stored/persisted.
        }

        ActiveEffects.Add(effect);

        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.EffectApplied,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = string.IsNullOrWhiteSpace(effect.Note)
                ? $"Effect applied: {effect.Name}"
                : $"Effect applied: {effect.Name} — {effect.Note}"
        });
        _ = TriggerAutosaveAsync();
    }

    // One-Shot effects are never stored — they adjust a Current value once, immediately,
    // then vanish. Only Current-value targets are valid; Max and Skill targets are
    // rejected here as a data-layer guard (the UI shouldn't offer them in the first place).
    private void ApplyOneShotEffect(CharacterEffect effect)
    {
        foreach (var modifier in effect.Modifiers)
        {
            switch (modifier.Target)
            {
                case EffectTargetType.VigorCurrent: AdjustCurrentStat("vigor", modifier.Amount); break;
                case EffectTargetType.AgilityCurrent: AdjustCurrentStat("agility", modifier.Amount); break;
                case EffectTargetType.MindCurrent: AdjustCurrentStat("mind", modifier.Amount); break;
                case EffectTargetType.SpiritCurrent: AdjustCurrentStat("spirit", modifier.Amount); break;
                case EffectTargetType.HitpointsCurrent: AdjustHitpointsDirect(modifier.Amount); break;
                case EffectTargetType.FatigueCurrent: AddFatigue(-modifier.Amount); break;
                default:
                    System.Diagnostics.Debug.WriteLine($"One-Shot effect '{effect.Name}' targeted {modifier.Target}, which is not a valid One-Shot target — skipped.");
                    break;
            }
        }

        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.EffectApplied,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = string.IsNullOrWhiteSpace(effect.Note)
                ? $"One-Shot effect applied: {effect.Name}"
                : $"One-Shot effect applied: {effect.Name} — {effect.Note}"
        });
        _ = TriggerAutosaveAsync();
    }

    // Adjusts Hit Protection directly, with no armor mitigation or Vigor overflow — this
    // is for Effects (a poison tick, a magical heal), not the Apply Damage combat flow.
    // amount > 0 heals, amount < 0 harms. Clamped between 0 and max Hit Protection.
    private void AdjustHitpointsDirect(int amount)
    {
        HitpointsDamage = Math.Clamp(HitpointsDamage - amount, 0, BaseHitpoints);
    }

    public void EditEffect(CharacterEffect updatedEffect)
    {
        var existing = ActiveEffects.FirstOrDefault(e => e.Id == updatedEffect.Id);
        if (existing == null) return;

        string oldSummary = SummarizeEffect(existing);
        existing.Name = updatedEffect.Name;
        existing.Note = updatedEffect.Note;
        existing.Modifiers = updatedEffect.Modifiers;
        // Duration stays Persistent — One-Shot effects are never stored, so they can never reach here.

        string newSummary = SummarizeEffect(existing);

        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.EffectEdited,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = $"Effect edited: {existing.Name} — was [{oldSummary}], now [{newSummary}]"
        });
        _ = TriggerAutosaveAsync();
    }

    private static string SummarizeEffect(CharacterEffect effect)
    {
        if (effect.Modifiers.Count == 0) return "no modifiers";
        var m = effect.Modifiers[0];
        string targetLabel = m.Target == EffectTargetType.Skill ? $"Skill: {m.SkillName}" : m.Target.ToString();
        return $"{targetLabel} {(m.Amount >= 0 ? "+" : "")}{m.Amount}";
    }

    public void RemoveEffect(Guid effectId)
    {
        var effect = ActiveEffects.FirstOrDefault(e => e.Id == effectId);
        if (effect == null) return;

        if (effect.HardcodedType != HardcodedEffectType.None) return;   // hardcoded effects have their own resolution flow
        ActiveEffects.Remove(effect);

        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.EffectCleared,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = $"Effect cleared: {effect.Name}"
        });
        _ = TriggerAutosaveAsync();
    }

    private bool _ignoreEffects;

    public int GetEffectModifierSum(EffectTargetType target, string? skillName = null)
    {
        if (_ignoreEffects) return 0;

        return ActiveEffects
            .SelectMany(e => e.Modifiers)
            .Where(m => m.Target == target && (target != EffectTargetType.Skill || m.SkillName == skillName))
            .Sum(m => m.Amount);
    }

    // -1 = lower than baseline, 0 = same, +1 = higher. The baseline is recomputed with every effect ignored.
    public int Direction(int modifiedValue, Func<int> baselineGetter)
    {
        int baseline;
        _ignoreEffects = true;
        try { baseline = baselineGetter(); }
        finally { _ignoreEffects = false; }

        return Math.Sign(modifiedValue - baseline);
    }

    public int SkillDirection(string skillName)
    => Direction(GetSkillBreakdown(skillName).Total,
                 () => GetSkillBreakdown(skillName).Total);

    // Godrick patch 08/07/2026: HP is now static — 1 base + Class's flat bonus + Vigor Bonus.
    // Never reduced by a negative Vigor Bonus.
    public int HitpointsAdjustment { get; set; } = 0;

    public int BaseHitpoints
    {
        get
        {
            int classBonus = ClassPickSum(c => c.HitProtectionBonus);   // every pick counts, not just the original class
            int vigorContribution = Math.Max(TotalASMStatVigor, 0);
            int effectBonus = GetEffectModifierSum(EffectTargetType.HitpointsMax);
            return 1 + classBonus + vigorContribution + HitpointsAdjustment + effectBonus;
        }
    }

    // Max HP is 0 while encumbered (rulebook).
    public int Hitpoints => _encumbranceApplied ? 0 : BaseHitpoints;

    // ===== PACK CAPACITY & ENCUMBRANCE =====
    // Pack Mule (a Warrior feature) adds 2 slots. Taking it at level-up counts the same as at creation.
    private const int PackMuleFeatureId = 9;
    private const int PackMuleSlots = 2;
    public int InventorySlotsTotal => 6 + PackVigorModifier + (HasFeature(PackMuleFeatureId) ? PackMuleSlots : 0);
    public int InventorySlotsAvailable => InventorySlotCalculator.AvailableSlots(InventorySlotsTotal, FatigueInInventory);

    public int InventorySlotsUsed => _inventory
        .Where(i => !i.IsDropped && !i.IsEmpty && i.Template != null && i.EquipmentSlot == EquipmentSlotType.None)
        .Sum(i => InventorySlotCalculator.SlotsFor(i.Template!, i.Quantity));

    public bool IsEncumbered => !IsPackDropped && InventorySlotsUsed > InventorySlotsAvailable;

    public bool IsPackDropped { get; private set; }

    public void SetPackDropped(bool dropped)
    {
        if (IsPackDropped == dropped) return;
        IsPackDropped = dropped;

        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = dropped ? PlayerActionType.PackDropped : PlayerActionType.PackRecovered,
            PlayerName = PlayerName,
            PlayerId = PlayerId,
            CharacterName = CharacterName,
            CharacterGuid = CharacterGuid,
            Summary = dropped ? "Dropped pack" : "Picked up pack"
        });

        UpdateEncumbranceState();   // also autosaves if encumbrance changed
        _ = TriggerAutosaveAsync();
    }

    private bool _encumbranceApplied;

    public void UpdateEncumbranceState()
    {
        bool now = IsEncumbered;
        if (now == _encumbranceApplied) return;

        _encumbranceApplied = now;
        if (now)
        {
            // Current HP is lost on becoming encumbered. Recovering restores Max HP,
            // but current stays 0 until a rest clears the damage.
            HitpointsDamage = BaseHitpoints;
        }
        _ = TriggerAutosaveAsync();
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
        if (!_acquiredQuips.Any(q => SubFeatureIdentity.Same(q.Guid, q.Id, quip.Guid, quip.Id)))
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
        if (!_acquiredSpells.Any(s => SubFeatureIdentity.Same(s.Guid, s.Id, spell.Guid, spell.Id)))
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
        if (!_acquiredTechniques.Any(t => SubFeatureIdentity.Same(t.Guid, t.Id, technique.Guid, technique.Id)))
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
        if (!_acquiredBlueprints.Any(b => SubFeatureIdentity.Same(b.Guid, b.Id, blueprint.Guid, blueprint.Id)))
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

    // === FEATURES & RESOURCES ===
    public Dictionary<int, int> ClassSelections { get; set; } = new();
    public int Favour { get; set; }
    public int DaysPassed { get; set; }
    public int ShortRestsTaken { get; set; }
    public List<FeatureUseRecord> FeatureUses { get; set; } = new();
    public TransformState? Transform { get; set; }
    public List<SavedForm> SavedForms { get; set; } = new();

    // Dice pools are re-usable. Their size is the number of times the granting class was picked.
    public int GetDicePoolSize(DicePool pool)
    {
        int total = 0;
        foreach (var (classId, times) in ClassSelections)
        {
            var cls = _classDataService.GetClassByIdAsync(classId).Result;
            if (cls?.GrantsDicePool == pool) total += times * Math.Max(1, cls.DicePerSelection);
        }
        return total;
    }

    public bool UsesFavour => ClassSelections.Keys.Any(id =>
        (_classDataService.GetClassByIdAsync(id).Result?.FavourPerSelection ?? 0) > 0);

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
        // Matches any item whose reserved span covers this slot, not just its first
        // slot — a two-handed item stored at SlotIndex=1 also occupies slot 2.
        return _inventory.FirstOrDefault(i =>
            i.IsEquipped && i.EquipmentSlot == EquipmentSlotType.Hand &&
            i.SlotIndex.HasValue && slot >= i.SlotIndex.Value && slot < i.SlotIndex.Value + i.SlotsRequired);
    }

    public CharacterItem? GetEquippedBeltSlot(int slot)
    {
        // Matches any item whose reserved span covers this slot, not just its first
        // slot — a bulky item stored at SlotIndex=2 also occupies slot 3.
        return _inventory.FirstOrDefault(i =>
            i.IsEquipped && i.EquipmentSlot == EquipmentSlotType.Belt &&
            i.SlotIndex.HasValue && slot >= i.SlotIndex.Value && slot < i.SlotIndex.Value + i.SlotsRequired);
    }

    public CharacterItem? GetEquippedArmor()
    {
        return _inventory.FirstOrDefault(i => i.IsEquipped && i.EquipmentSlot == EquipmentSlotType.Armor);
    }

    public virtual CharacterItem AddItemToInventory(Item template, int quantity = 1, int? writtenValueInChips = null, int? startingUses = null, int? startingUsageDie = null)
    {
        if (template == null) return null;
        if (quantity < 1) quantity = 1;

        bool stackable = template.IsStackableItem;
        int capacity = InventorySlotCalculator.MaxPerRecord(template);
        CharacterItem? last = null;

        // Uses a new item starts with: full, unless a lower number was chosen (an empty flask, a half-used torch).
        // Only a single item can start partly used, so a stack of more than one always starts full.
        int fullUses = template.HasUses ? template.Uses!.Value : 0;
        int remainingUses = template.HasUses && startingUses.HasValue && !(stackable && quantity > 1)
            ? Math.Clamp(startingUses.Value, 0, fullUses)
            : fullUses;
        bool partlyUsed = template.HasUses && remainingUses < fullUses;

        // Top up existing partial stacks first (a partly used item stays its own record)
        if (stackable && !partlyUsed)
        {
            foreach (var stack in _inventory.Where(i =>
                         (template.IsPlayerCreated ? i.TemplateGuid == template.Guid : i.TemplateId == template.Id) &&
                         !i.IsDropped && !i.IsEquipped && !i.IsEmpty && i.Quantity < capacity))
            {
                if (quantity <= 0) break;
                int add = Math.Min(quantity, capacity - stack.Quantity);
                stack.Quantity += add;
                stack.LastModified = DateTime.Now;
                quantity -= add;
                last = stack;
            }
        }

        // Whatever is left becomes new records (one per item, or one per legal stack)
        while (quantity > 0)
        {
            int chunk = stackable ? Math.Min(quantity, capacity) : 1;

            last = new CharacterItem
            {
                Id = GetNextCharacterItemId(),
                TemplateId = template.Id,
                TemplateGuid = template.IsPlayerCreated ? template.Guid : null,
                Template = template,
                Quantity = chunk,
                RemainingUses = remainingUses,
                UsageDieSides = template.HasUsageDie
                    ? (startingUsageDie is { } d && UsageDie.IsValid(d) ? d : template.DefaultUsageDieSides)
                    : 0,
                IsEmpty = template.HasUses && remainingUses <= 0,
                AcquiredAt = DateTime.Now,
                WrittenValueInChips = template.IsValueEditable ? writtenValueInChips : null
            };
            _inventory.Add(last);
            quantity -= chunk;
        }

        return last!;
    }

    // Older saves may hold one record with Quantity = N (non-stackable) or a stack over its cap.
    // Split them so every record is one physical item or one legal stack. No-op once migrated.
    private void MigrateInventoryRecords()
    {
        foreach (var record in _inventory.Where(r => r.Template != null).ToList())
        {
            int capacity = InventorySlotCalculator.MaxPerRecord(record.Template!);
            if (record.Quantity <= capacity) continue;

            int excess = record.Quantity - capacity;
            record.Quantity = capacity;

            while (excess > 0)
            {
                int chunk = Math.Min(excess, capacity);
                excess -= chunk;

                _inventory.Add(new CharacterItem
                {
                    Id = GetNextCharacterItemId(),
                    TemplateId = record.TemplateId,
                    TemplateGuid = record.TemplateGuid,
                    Template = record.Template,
                    Quantity = chunk,
                    RemainingUses = record.RemainingUses,
                    UsageDieSides = record.UsageDieSides,
                    PlayerNote = record.PlayerNote,
                    CustomName = record.CustomName,
                    AcquiredAt = record.AcquiredAt
                });
            }
        }
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
            UnequipItemCore(item.Id);
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

    // Items in the pack that carry a usage die. Copies made before the item gained a die get the template's starting die.
    public List<CharacterItem> GetUsageDieItems()
    {
        foreach (var r in _inventory.Where(r => r.Template?.HasUsageDie == true && r.UsageDieSides == 0))
            r.UsageDieSides = r.Template!.DefaultUsageDieSides;

        return _inventory.Where(i => !i.IsDropped && i.HasUsageDie && !(i.Template!.IsHybridUsage && i.IsEmpty)).ToList();
    }

    // Applies one usage die roll to an item. A 1 or 2 steps the die down; a D4 that does so is removed for good.
    // A hybrid item (uses + die) spends a Use instead and keeps its die; at 0 uses it is empty, not removed.
    public UsageDieOutcome? ResolveUsageDieRoll(int characterItemId, int roll)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item?.Template == null || !item.HasUsageDie) return null;

        bool hybrid = item.Template.IsHybridUsage;
        if (hybrid && (item.IsEmpty || item.RemainingUses <= 0)) return null;   // nothing left to spend

        int sides = item.UsageDieSides;
        roll = Math.Clamp(roll, 1, sides);
        string name = item.DisplayName;
        int templateId = item.TemplateId;

        UsageDieOutcome outcome;
        if (!UsageDie.IsConsumingRoll(roll))
        {
            outcome = new UsageDieOutcome(name, sides, roll, false, null, false);
        }
        else if (hybrid)
        {
            UpdateItemUses(item.Id, item.RemainingUses - 1);
            outcome = new UsageDieOutcome(name, sides, roll, true, null, false, item.RemainingUses);
        }
        else
        {
            int? next = UsageDie.StepDown(sides);
            if (next == null)
            {
                RemoveItemFromInventory(item.Id);
                outcome = new UsageDieOutcome(name, sides, roll, true, null, true);
            }
            else
            {
                item.UsageDieSides = next.Value;
                item.LastModified = DateTime.Now;
                outcome = new UsageDieOutcome(name, sides, roll, true, next, false);
            }
        }

        LogFamiliarEvent(PlayerActionType.UsageDieRolled, outcome.Summary, name, templateId);
        _ = TriggerAutosaveAsync();
        return outcome;
    }

    // Destroys an item for good: removed from the pack, never recoverable, logged. A partial stack leaves the rest.
    // Separate from Uses (which tick up and down) and from the usage die (which wears an item down).
    public bool ExpendInventoryItem(int characterItemId, int quantity)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null || quantity < 1) return false;

        quantity = Math.Min(quantity, item.Quantity);
        string name = item.DisplayName;
        int templateId = item.TemplateId;

        if (quantity >= item.Quantity)
        {
            RemoveItemFromInventory(item.Id);   // unequips first if needed
        }
        else
        {
            item.Quantity -= quantity;
            item.LastModified = DateTime.Now;
        }

        LogFamiliarEvent(PlayerActionType.ItemExpended,
            quantity == 1 ? $"Expended {name}" : $"Expended {quantity}x {name}",
            name, templateId);
        _ = TriggerAutosaveAsync();
        return true;
    }

    // Player edits to one item. Each variable that actually changed is logged on its own as "Item Edited"
    // (so the GM sees exactly what was touched); an unchanged field logs nothing.
    public bool EditItem(int characterItemId, string? newPlayerNote, int? newWrittenValueInChips)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null || item.Template == null) return false;

        bool changed = false;
        string name = item.DisplayName;

        // ----- Player note -----
        string? oldNote = string.IsNullOrWhiteSpace(item.PlayerNote) ? null : item.PlayerNote;
        string? note = string.IsNullOrWhiteSpace(newPlayerNote) ? null : newPlayerNote.Trim();
        if (!string.Equals(oldNote, note, StringComparison.Ordinal))
        {
            item.PlayerNote = note;
            string how = oldNote == null ? "added" : note == null ? "cleared" : "changed";
            LogFamiliarEvent(PlayerActionType.ItemEdited,
                $"Edited {name}: Player Note {how}", name, item.TemplateId);
            changed = true;
        }

        // ----- Written value (Notes) -----
        if (newWrittenValueInChips.HasValue && item.Template.IsValueEditable)
        {
            int oldValue = CurrencyHelper.ValueEach(item);
            if (newWrittenValueInChips.Value != oldValue)
            {
                item.WrittenValueInChips = newWrittenValueInChips.Value;
                LogFamiliarEvent(PlayerActionType.ItemEdited,
                    $"Edited {name}: Written Value {oldValue:N0} → {newWrittenValueInChips.Value:N0} chips",
                    name, item.TemplateId);
                changed = true;
            }
        }

        if (changed)
        {
            item.LastModified = DateTime.Now;
            _ = TriggerAutosaveAsync();
        }
        return changed;
    }

    // True if this character, a familiar, the dropped pile or the recovery store holds any copy of the item.
    // Other players' items are the GM's to know about once multiplayer exists.
    public async Task<bool> IsTemplateOwnedAsync(int templateId)
    {
        if (_inventory.Any(i => i.TemplateId == templateId)) return true;
        if (_droppedItems.Any(i => i.TemplateId == templateId)) return true;
        if (_acquiredFamiliars.Any(f => f.Inventory.Any(i => i.TemplateId == templateId))) return true;

        var recoverable = await GetRecoverableItemsAsync();
        return recoverable.Any(i => i.TemplateId == templateId);
    }

    public void RecordItemDefinitionEdit(Item template, IReadOnlyList<string> changes, bool ruleSkated)
    {
        string what = string.Join(", ", changes);
        LogFamiliarEvent(PlayerActionType.ItemEdited,
            $"Edited item definition {template.Name}: {what}", template.Name, template.Id);

        if (ruleSkated)
            LogFamiliarEvent(PlayerActionType.RuleBypassed,
                $"Edited {template.Name} while it is owned (no GM to approve)", template.Name, template.Id);

        _ = TriggerAutosaveAsync();
    }

    public void RecordItemEditRequested(Item template, IReadOnlyList<string> changes)
    {
        LogFamiliarEvent(PlayerActionType.ItemEditRequested,
            $"Requested GM approval to edit {template.Name}: {string.Join(", ", changes)}",
            template.Name, template.Id);
    }

    // While the pack is dropped, only items already equipped/belted can be touched.
    private bool PackBlocks(CharacterItem item) => IsPackDropped && !item.IsEquipped;

    public void EquipItem(int characterItemId, int slot) => LoggedEquipVoid(() => EquipItemCore(characterItemId, slot));
    private void EquipItemCore(int characterItemId, int slot)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null || item.IsEmpty) return;
        if (PackBlocks(item)) return;

        // ===== WORN ITEMS (Jewelry, Trinkets): equipped without a slot, no limit =====
        if (item.IsWorn)
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

    public void EquipItemAsArmor(int characterItemId) => LoggedEquipVoid(() => EquipItemAsArmorCore(characterItemId));
    private void EquipItemAsArmorCore(int characterItemId)
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

    public void UnequipItem(int characterItemId) => LoggedEquipVoid(() => UnequipItemCore(characterItemId));
    private void UnequipItemCore(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return;

        // A two-handed item also holds its limb set. Release it here so every caller
        // (flyout, drop, drop mode, swaps) leaves the set free instead of "occupied".
        foreach (var limbSet in _limbSets)
        {
            if (limbSet.TwoHandedItemId == characterItemId)
            {
                limbSet.TwoHandedItemId = null;
                break;
            }
        }

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
                UnequipItemCore(item.Id);
            }

            item.IsDropped = true;
            item.DroppedAt = DateTime.Now;
            _inventory.Remove(item);
            _droppedItems.Add(item);
            System.Diagnostics.Debug.WriteLine($"Dropped item: {item.DisplayName} (ID: {item.Id})");
            _ = _actionLogService.LogItemDroppedAsync(item, PlayerName, PlayerId, CharacterName, CharacterGuid);
            if (!IsReadOnly) _ = _recoveryService.AddRecoverableAsync(item, PlayerId, SessionId);
            _ = TriggerAutosaveAsync();
        }
    }

    // Drops part of a stack. Dropping the whole stack goes through DropItem(int) unchanged.
    // The dropped portion is always full-use units: the opened item stays with the carried stack.
    public void DropItem(int characterItemId, int quantity)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return;

        if (quantity >= item.Quantity)
        {
            DropItem(characterItemId);
            return;
        }
        if (quantity < 1) return;

        int stackBefore = item.Quantity;

        var dropped = new CharacterItem
        {
            Id = GetNextCharacterItemId(),
            InstanceId = Guid.NewGuid(),
            TemplateId = item.TemplateId,
            TemplateGuid = item.TemplateGuid,
            Template = item.Template,
            Quantity = quantity,
            RemainingUses = ItemRecordTransfer.FullUses(item),   // the opened item stays with the carried stack
            UsageDieSides = item.UsageDieSides,
            IsEmpty = false,
            IsIdentified = item.IsIdentified,
            CustomName = item.CustomName,
            PlayerNote = item.PlayerNote,
            AcquiredAt = item.AcquiredAt,
            LastModified = DateTime.Now,
            IsDropped = true,
            DroppedAt = DateTime.Now
        };

        item.Quantity -= quantity;
        item.LastModified = DateTime.Now;

        _droppedItems.Add(dropped);
        LogFamiliarEvent(PlayerActionType.ItemDropped,
            $"Dropped {quantity} of {stackBefore} {dropped.DisplayName}",
            dropped.DisplayName, dropped.TemplateId);
        if (!IsReadOnly) _ = _recoveryService.AddRecoverableAsync(dropped, PlayerId, SessionId);   // the DROPPED portion, not the carried stack
        _ = TriggerAutosaveAsync();
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

    // Drag-and-drop between two pack tiles of the same item. Neither equipped: full units from
    // the dragged stack merge into the target up to the stack cap. A partly used (opened) item
    // never leaves its stack, so from an opened source only the full units move. If one stack is
    // equipped, they swap as if they were different items.
    // Drag-and-drop between two pack tiles of the same item. Neither equipped:
    //  - the whole dragged stack fits: everything merges, and an opened item gives the target its uses;
    //  - it doesn't fit: full units move first up to the stack cap, and an opened item stays on the source.
    // If one stack is equipped, they swap as if they were different items.
    public StackMergeResult MergeItemStacks(int sourceItemId, int targetItemId)
    {
        var source = _inventory.FirstOrDefault(i => i.Id == sourceItemId);
        var target = _inventory.FirstOrDefault(i => i.Id == targetItemId);
        if (source?.Template == null || target?.Template == null || source.Id == target.Id
            || !ItemRecordTransfer.SameItem(source, target)) return StackMergeResult.NotSameItem;
        if (IsPackDropped || (source.IsEquipped && target.IsEquipped)) return StackMergeResult.Blocked;

        if (source.IsEquipped || target.IsEquipped)
        {
            var equipped = source.IsEquipped ? source : target;
            var carried = source.IsEquipped ? target : source;
            return SwapWithEquipped(equipped, carried) ? StackMergeResult.Swapped : StackMergeResult.Blocked;
        }

        if (!target.Template.IsStackableItem) return StackMergeResult.NotStackable;
        if (ItemRecordTransfer.IsEmptyItem(source) || ItemRecordTransfer.IsEmptyItem(target)) return StackMergeResult.EmptyItem;

        bool sourceOpened = ItemRecordTransfer.IsOpened(source);
        if (sourceOpened && ItemRecordTransfer.IsOpened(target)) return StackMergeResult.BothOpened;

        int room = InventorySlotCalculator.MaxPerRecord(target.Template) - target.Quantity;
        if (room <= 0) return StackMergeResult.TargetFull;

        // Anything short of the whole stack leaves at least one item behind, and that is
        // the opened one, because only a full move changes the uses.
        int moved = Math.Min(room, source.Quantity);
        bool wholeStack = moved >= source.Quantity;

        target.Quantity += moved;
        target.LastModified = DateTime.Now;
        if (wholeStack && sourceOpened) target.RemainingUses = source.RemainingUses;

        if (wholeStack)
        {
            // The dragged record is used up, so its note joins the target's. A remainder keeps its own note.
            target.PlayerNote = ItemRecordTransfer.CombineNotes(target.PlayerNote, source.PlayerNote);
            _inventory.Remove(source);
        }
        else
        {
            source.Quantity -= moved;
            source.LastModified = DateTime.Now;
        }

        _ = TriggerAutosaveAsync();
        return StackMergeResult.Merged;
    }

    // The carried item takes the equipped item's slot and the equipped one goes back to the pack.
    // It reuses the existing equip methods inside one LoggedEquip, so the swap logs once as net changes.
    private bool SwapWithEquipped(CharacterItem equipped, CharacterItem carried)
    {
        if (carried.IsEmpty) return false;
        int index = equipped.SlotIndex ?? 1;

        bool ok = LoggedEquip(() =>
        {
            switch (equipped.EquipmentSlot)
            {
                case EquipmentSlotType.Hand: return EquipToHandSlot(carried.Id, index);
                case EquipmentSlotType.Belt: return EquipToBeltAtSlot(carried.Id, index);
                case EquipmentSlotType.Armor: EquipItemAsArmor(carried.Id); return carried.IsEquipped;
                default: return false;
            }
        });

        if (ok) _ = TriggerAutosaveAsync();
        return ok;
    }

    // ===== EQUIP LOGGING (characters only) =====
    // Public equip methods are wrapped so only the OUTERMOST call logs, comparing equipment
    // state before and after. Swaps and displacements are therefore logged once, as net changes.
    private int _equipLogDepth;

    private readonly record struct EquipSnapshot(bool Equipped, EquipmentSlotType Slot, int? Index);

    private T LoggedEquip<T>(Func<T> op)
    {
        if (_equipLogDepth > 0) return op();

        var before = new Dictionary<int, EquipSnapshot>();
        foreach (var i in _inventory)
            before[i.Id] = new EquipSnapshot(i.IsEquipped, i.EquipmentSlot, i.SlotIndex);

        _equipLogDepth++;
        try
        {
            return op();
        }
        finally
        {
            _equipLogDepth--;
            try { LogEquipChanges(before); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Equip log error: {ex.Message}"); }
        }
    }

    private void LoggedEquipVoid(Action op) => LoggedEquip(() => { op(); return true; });

    private void LogEquipChanges(Dictionary<int, EquipSnapshot> before)
    {
        var entries = new List<(int Order, PlayerActionLogEntry Entry)>();

        foreach (var item in _inventory)
        {
            if (!before.TryGetValue(item.Id, out var old)) continue;

            bool now = item.IsEquipped;
            bool unchanged = old.Equipped == now &&
                             (!now || (old.Slot == item.EquipmentSlot && old.Index == item.SlotIndex));
            if (unchanged) continue;

            PlayerActionType type;
            string summary;
            int order;

            if (!old.Equipped && now)
            {
                type = PlayerActionType.ItemEquipped;
                summary = $"Equipped {item.DisplayName} to {DescribeEquipSlot(item.EquipmentSlot, item.SlotIndex)}";
                order = 2;
            }
            else if (old.Equipped && !now)
            {
                type = PlayerActionType.ItemUnequipped;
                summary = $"Unequipped {item.DisplayName} (was {DescribeEquipSlot(old.Slot, old.Index)})";
                order = 0;
            }
            else
            {
                type = PlayerActionType.ItemMoved;
                summary = $"Moved {item.DisplayName} from {DescribeEquipSlot(old.Slot, old.Index)} " +
                          $"to {DescribeEquipSlot(item.EquipmentSlot, item.SlotIndex)}";
                order = 1;
            }

            entries.Add((order, new PlayerActionLogEntry
            {
                ActionType = type,
                PlayerName = PlayerName,
                PlayerId = PlayerId,
                CharacterName = CharacterName,
                CharacterGuid = CharacterGuid,
                ItemName = item.DisplayName,
                ItemId = item.TemplateId,
                Summary = summary
            }));
        }

        // Things leaving slots first, then moves, then new equips: reads naturally for swaps.
        foreach (var e in entries.OrderBy(e => e.Order))
            _ = _actionLogService.LogEventAsync(e.Entry);
    }

    private static string DescribeEquipSlot(EquipmentSlotType slot, int? index) => slot switch
    {
        EquipmentSlotType.Hand => index.HasValue ? $"Hand {index}" : "Hand",
        EquipmentSlotType.Belt => index.HasValue ? $"Belt slot {index}" : "Belt",
        EquipmentSlotType.Armor => "Armor slot",
        _ => "Worn"
    };

    public void TransferItemToPlayer(int characterItemId, int targetCharacterId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item != null)
        {
            if (item.IsEquipped)
            {
                UnequipItemCore(item.Id);
            }

            item.CharacterId = targetCharacterId;
            item.LastModified = DateTime.Now;
            item.SyncId = Guid.NewGuid().ToString();
        }
    }

    // Step 1 of sending: snapshot the item into a packet. Changes nothing.
    public ItemTransferPacket? BuildTransferPacket(int characterItemId, int quantity, TransferRecipient recipient)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null || item.Template == null || item.IsEquipped || IsPackDropped) return null;

        int qty = Math.Clamp(quantity, 1, item.Quantity);
        return new ItemTransferPacket
        {
            FromPlayerId = PlayerId,
            FromPlayerName = PlayerName,
            FromCharacterGuid = CharacterGuid,
            FromCharacterName = CharacterName,
            ToCharacterGuid = recipient.CharacterGuid,
            ToCharacterName = recipient.CharacterName,
            ToPlayerName = recipient.PlayerName,
            TemplateId = item.TemplateId,
            TemplateGuid = item.TemplateGuid,
            ItemName = item.DisplayName,
            Quantity = qty,
            ItemInstanceId = qty == item.Quantity ? item.InstanceId : Guid.NewGuid(),
            RemainingUses = qty == item.Quantity ? item.RemainingUses : ItemRecordTransfer.FullUses(item),
            UsageDieSides = item.UsageDieSides,
            CustomName = item.CustomName,
            PlayerNote = item.PlayerNote,
            WrittenValueInChips = item.WrittenValueInChips,
            IsIdentified = item.IsIdentified
        };
    }

    // Step 2 of sending: call only after the gateway reports success.
    public void CompleteTransferOut(int characterItemId, ItemTransferPacket packet)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return;

        ItemRecordTransfer.RemoveQuantity(_inventory, item, packet.Quantity);

        LogFamiliarEvent(PlayerActionType.ItemSentToPlayer,
            $"Sent {packet.Quantity}x {packet.ItemName} to {packet.ToCharacterName} ({packet.ToPlayerName})",
            packet.ItemName, packet.TemplateId);
        _ = TriggerAutosaveAsync();
    }

    // Receiving side: rebuilds the item from the packet, keeping uses, notes and custom name.
    public async Task<bool> ReceiveItemTransferAsync(ItemTransferPacket packet)
    {
        if (packet == null || packet.Quantity < 1 || packet.ToCharacterGuid != CharacterGuid) return false;

        var template = packet.TemplateGuid.HasValue
            ? await _itemDataService.GetItemByGuidAsync(packet.TemplateGuid.Value)
            : await _itemDataService.GetItemByIdAsync(packet.TemplateId);
        if (template == null) return false;

        var source = new CharacterItem
        {
            TemplateId = packet.TemplateId,
            TemplateGuid = packet.TemplateGuid,
            Template = template,
            Quantity = packet.Quantity,
            InstanceId = packet.ItemInstanceId == Guid.Empty ? Guid.NewGuid() : packet.ItemInstanceId,
            RemainingUses = packet.RemainingUses,
            UsageDieSides = packet.UsageDieSides,
            CustomName = packet.CustomName,
            PlayerNote = packet.PlayerNote,
            WrittenValueInChips = packet.WrittenValueInChips,
            IsIdentified = packet.IsIdentified
        };
        ItemRecordTransfer.MoveInto(_inventory, source, packet.Quantity, GetNextCharacterItemId);

        LogFamiliarEvent(PlayerActionType.ItemReceivedFromPlayer,
            $"Received {packet.Quantity}x {packet.ItemName} from {packet.FromCharacterName} ({packet.FromPlayerName})",
            packet.ItemName, packet.TemplateId);
        _ = TriggerAutosaveAsync();
        return true;
    }

    // ===== HOT-SWAP METHODS =====

    public int? FindAvailableBeltSlot(int slotsNeeded = 1)
    {
        if (slotsNeeded < 1 || slotsNeeded > 2) return null;

        bool[] occupied = new bool[5]; // 1-indexed
        for (int i = 1; i <= 4; i++)
        {
            occupied[i] = GetEquippedBeltSlot(i) != null;
        }

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

    public void EquipTwoHandedItem(int characterItemId, int limbSetId) => LoggedEquipVoid(() => EquipTwoHandedItemCore(characterItemId, limbSetId));
    private void EquipTwoHandedItemCore(int characterItemId, int limbSetId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return;
        if (PackBlocks(item)) return;
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

    public void UnequipTwoHandedItem(int characterItemId) => LoggedEquipVoid(() => UnequipTwoHandedItemCore(characterItemId));
    private void UnequipTwoHandedItemCore(int characterItemId)
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

    // ===== EQUIP HELPERS (used by EquipmentActionHelper) =====
    // Rule: an item that is already equipped may always be MOVED, even if it has run out
    // of uses. Only a depleted item sitting in the pack is blocked from being equipped.

    private void PlaceInHandSlot(CharacterItem item, int handSlot)
    {
        if (item.IsEquipped) UnequipItem(item.Id);   // leaves its old slot (and old limb set)
        item.IsEquipped = true;
        item.EquipmentSlot = EquipmentSlotType.Hand;
        item.SlotIndex = handSlot;
        item.LastModified = DateTime.Now;
    }

    private void PlaceInBeltSlot(CharacterItem item, int beltSlot)
    {
        if (item.IsEquipped) UnequipItem(item.Id);
        item.IsEquipped = true;
        item.EquipmentSlot = EquipmentSlotType.Belt;
        item.SlotIndex = beltSlot;   // 1-4, same convention EquipItem and SwapToBelt use
        item.LastModified = DateTime.Now;
    }

    // Single-handed only. If every hand slot is full, the occupant of the highest-numbered
    // slot goes back to the pack. Two-handed items use EquipTwoHandedItemWithSwap, because
    // which paired set to use is the player's choice.
    public bool EquipToHandAuto(int characterItemId) => LoggedEquip(() => EquipToHandAutoCore(characterItemId));
    private bool EquipToHandAutoCore(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return false;
        if (PackBlocks(item)) return false;
        if (item.SlotsRequired > 1) return false;
        if (item.IsEmpty && !item.IsEquipped) return false;

        int totalSlots = GetTotalLimbSlots();
        if (totalSlots < 1) return false;

        var availableSlot = FindAvailableHandSlot(1);
        if (!availableSlot.HasValue)
        {
            for (int i = totalSlots; i >= 1; i--)
            {
                var occupant = GetEquippedHandSlot(i);
                if (occupant != null)
                {
                    UnequipItem(occupant.Id);   // also frees a two-handed occupant's set
                    break;
                }
            }

            availableSlot = FindAvailableHandSlot(1);
            if (!availableSlot.HasValue) return false;
        }

        PlaceInHandSlot(item, availableSlot.Value);
        return true;
    }

    // Clears whatever occupies the chosen paired set, then equips the item there.
    // Returns false (and says so) instead of silently doing nothing.
    public bool EquipTwoHandedItemWithSwap(int characterItemId, int limbSetId) => LoggedEquip(() => EquipTwoHandedItemWithSwapCore(characterItemId, limbSetId));
    private bool EquipTwoHandedItemWithSwapCore(int characterItemId, int limbSetId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        var limbSet = _limbSets.FirstOrDefault(l => l.Id == limbSetId);
        if (item == null || limbSet == null) return false;
        if (PackBlocks(item)) return false;
        if (item.SlotsRequired < 2 || limbSet.PairType != LimbPairType.Paired) return false;
        if (item.IsEmpty && !item.IsEquipped) return false;

        if (limbSet.TwoHandedItemId.HasValue && limbSet.TwoHandedItemId != characterItemId)
        {
            UnequipItem(limbSet.TwoHandedItemId.Value);
        }

        foreach (var slotIndex in limbSet.SlotIndices)
        {
            var occupant = GetEquippedHandSlot(slotIndex + 1);
            if (occupant != null && occupant.Id != characterItemId)
            {
                UnequipItem(occupant.Id);
            }
        }

        if (item.IsEquipped) UnequipItem(item.Id);   // leaves the belt, or another set

        EquipTwoHandedItem(characterItemId, limbSetId);
        return item.IsEquipped && limbSet.TwoHandedItemId == characterItemId;
    }

    // Equips straight onto the belt if a free spot exists (adjacent, for a bulky item).
    public BeltEquipResult TryEquipToBelt(int characterItemId) => LoggedEquip(() => TryEquipToBeltCore(characterItemId));
    private BeltEquipResult TryEquipToBeltCore(int characterItemId)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return BeltEquipResult.NotEligible;
        if (PackBlocks(item)) return BeltEquipResult.NotEligible;
        if (item.IsWorn || (item.IsArmor && !item.IsShield)) return BeltEquipResult.NotEligible;
        if (item.IsEmpty && !item.IsEquipped) return BeltEquipResult.NotEligible;

        var availableSlot = FindAvailableBeltSlot(item.SlotsRequired);
        if (!availableSlot.HasValue)
        {
            // A bulky item with no free adjacent pair: shift belted items aside when the belt has room overall.
            if (TryMakeRoomOnBelt(item)) return BeltEquipResult.Equipped;
            return BeltEquipResult.NeedsSlotChoice;   // belt genuinely can't fit it: the player picks what goes
        }

        PlaceInBeltSlot(item, availableSlot.Value);
        return BeltEquipResult.Equipped;
    }

    // A bulky item needs two adjacent belt slots. When none are free but the belt has room overall,
    // belted items shift to the nearest free slots instead of the player choosing a victim.
    // Picks the window that moves the fewest items, then the least distance. False if no window works.
    private bool TryMakeRoomOnBelt(CharacterItem item)
    {
        int size = item.SlotsRequired;
        if (size < 2 || size > SwapBeltSlots) return false;

        int? bestStart = null;
        List<(CharacterItem Item, int Start)>? bestPlan = null;
        int bestMovers = int.MaxValue, bestDistance = int.MaxValue;

        for (int start = 1; start + size - 1 <= SwapBeltSlots; start++)
        {
            var movers = Enumerable.Range(start, size)
                .Select(s => GetEquippedBeltSlot(s))
                .Where(i => i != null && i.Id != item.Id)
                .Select(i => i!)
                .DistinctBy(i => i.Id)
                .ToList();

            var plan = PlanBeltPushAside(movers, item, start, size);
            if (plan == null) continue;

            int distance = plan.Sum(p => Math.Abs(p.Start - (p.Item.SlotIndex ?? p.Start)));
            if (movers.Count < bestMovers || (movers.Count == bestMovers && distance < bestDistance))
            {
                bestStart = start;
                bestPlan = plan;
                bestMovers = movers.Count;
                bestDistance = distance;
            }
        }

        if (bestStart == null || bestPlan == null) return false;

        foreach (var (mover, newStart) in bestPlan) PlaceInBeltSlot(mover, newStart);
        PlaceInBeltSlot(item, bestStart.Value);
        return true;
    }

    // Puts the item at a chosen belt position; whatever is there returns to the pack.
    public bool EquipToBeltAtSlot(int characterItemId, int startSlot) => LoggedEquip(() => EquipToBeltAtSlotCore(characterItemId, startSlot));
    private bool EquipToBeltAtSlotCore(int characterItemId, int startSlot)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return false;
        if (PackBlocks(item)) return false;
        if (item.IsWorn || (item.IsArmor && !item.IsShield)) return false;
        if (item.IsEmpty && !item.IsEquipped) return false;

        int lastSlot = startSlot + item.SlotsRequired - 1;
        if (startSlot < 1 || lastSlot > 4) return false;

        for (int slot = startSlot; slot <= lastSlot; slot++)
        {
            var occupant = GetEquippedBeltSlot(slot);
            if (occupant != null && occupant.Id != characterItemId)
            {
                UnequipItem(occupant.Id);
            }
        }

        PlaceInBeltSlot(item, startSlot);
        return true;
    }

    // Drag-and-drop target: puts the item on a specific hand slot. Whatever is there returns to the pack.
    // A two-handed item takes the paired set that contains the chosen slot.
    public bool EquipToHandSlot(int characterItemId, int handSlot) => LoggedEquip(() => EquipToHandSlotCore(characterItemId, handSlot));
    private bool EquipToHandSlotCore(int characterItemId, int handSlot)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return false;
        if (PackBlocks(item)) return false;
        if (item.IsWorn || (item.IsArmor && !item.IsShield)) return false;
        if (item.IsEmpty && !item.IsEquipped) return false;

        int totalSlots = GetTotalLimbSlots();
        if (handSlot < 1 || handSlot > totalSlots) return false;

        if (item.SlotsRequired > 1)
        {
            var limbSet = _limbSets.FirstOrDefault(l => l.SlotIndices.Contains(handSlot - 1));
            if (limbSet == null || limbSet.PairType != LimbPairType.Paired) return false;
            return EquipTwoHandedItemWithSwap(characterItemId, limbSet.Id);
        }

        var occupant = GetEquippedHandSlot(handSlot);
        if (occupant != null && occupant.Id != item.Id)
        {
            UnequipItem(occupant.Id);   // also frees a two-handed occupant's set
        }

        PlaceInHandSlot(item, handSlot);
        return true;
    }

    private const int SwapBeltSlots = 4; // if you already have a belt-size constant, use that instead

    public bool EquipToHandSlotWithSwap(int characterItemId, int handSlot) => LoggedEquip(() => EquipToHandSlotWithSwapCore(characterItemId, handSlot));
    private bool EquipToHandSlotWithSwapCore(int characterItemId, int handSlot)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return false;

        // Only belt sources get the swap; everything else keeps the existing behaviour.
        if (!item.IsEquipped || item.EquipmentSlot != EquipmentSlotType.Belt)
            return EquipToHandSlot(characterItemId, handSlot);

        if (PackBlocks(item)) return false;
        if (item.IsWorn || (item.IsArmor && !item.IsShield)) return false;
        if (handSlot < 1 || handSlot > GetTotalLimbSlots()) return false;

        var targetSlots = new List<int> { handSlot };
        var pairSet = item.SlotsRequired > 1
            ? _limbSets.FirstOrDefault(l => l.SlotIndices.Contains(handSlot - 1))
            : null;

        if (item.SlotsRequired > 1)
        {
            if (pairSet == null || pairSet.PairType != LimbPairType.Paired) return false;
            targetSlots = pairSet.SlotIndices.Select(i => i + 1).ToList();
        }

        var displaced = targetSlots
            .Select(s => GetEquippedHandSlot(s))
            .Where(o => o != null && o.Id != item.Id)
            .GroupBy(o => o!.Id)
            .Select(g => g.First()!)
            .OrderBy(o => o.SlotIndex ?? 0)
            .ToList();

        // Nothing to swap with: the existing path handles it.
        if (displaced.Count == 0) return EquipToHandSlot(characterItemId, handSlot);

        int originStart = item.SlotIndex ?? 1;

        // Single-handed items leave the belt first so their slot is free.
        // Two-handed items leave via EquipTwoHandedItemWithSwap, which keeps the pack-drop guard happy.
        if (pairSet == null) UnequipItem(item.Id);

        for (int k = 0; k < displaced.Count; k++)
            if (!TrySendToBelt(displaced[k], originStart + k, item.Id))
                UnequipItem(displaced[k].Id); // no room on the belt: falls back to the pack

        if (pairSet != null) return EquipTwoHandedItemWithSwap(item.Id, pairSet.Id);

        PlaceInHandSlot(item, handSlot);
        return true;
    }

    private bool TrySendToBelt(CharacterItem displaced, int preferredStart, int leavingItemId)
    {
        int size = Math.Max(1, displaced.SlotsRequired);
        if (size > SwapBeltSlots) return false;

        // Occupancy map of the belt (1-based), ignoring the item that is leaving.
        var map = new CharacterItem?[SwapBeltSlots + 2];
        foreach (var b in _inventory.Where(i => i.Id != displaced.Id && i.Id != leavingItemId
                 && i.IsEquipped && i.EquipmentSlot == EquipmentSlotType.Belt && i.SlotIndex.HasValue))
        {
            int first = b.SlotIndex!.Value;
            for (int s = first; s < first + Math.Max(1, b.SlotsRequired) && s <= SwapBeltSlots; s++)
                if (s >= 1) map[s] = b;
        }

        bool Fits(CharacterItem?[] m, int start, int len)
        {
            if (start < 1 || start + len - 1 > SwapBeltSlots) return false;
            for (int s = start; s < start + len; s++) if (m[s] != null) return false;
            return true;
        }

        IEnumerable<int> StartsNear(int len, int pref) =>
            Enumerable.Range(1, SwapBeltSlots - len + 1)
                      .OrderBy(s => Math.Abs(s - pref)).ThenBy(s => s);

        // Pass 1: a free window, nearest to the preferred slot.
        foreach (var start in StartsNear(size, preferredStart))
            if (Fits(map, start, size)) { PlaceInBeltSlot(displaced, start); return true; }

        // Pass 2: push whatever is in the window aside to the nearest free slots.
        foreach (var start in StartsNear(size, preferredStart))
        {
            var movers = Enumerable.Range(start, size)
                .Select(s => map[s]).Where(m => m != null)
                .GroupBy(m => m!.Id).Select(g => g.First()!)
                .OrderBy(m => m.SlotIndex).ToList();

            var trial = (CharacterItem?[])map.Clone();
            for (int s = 1; s <= SwapBeltSlots; s++)
                if (trial[s] != null && movers.Any(m => m.Id == trial[s]!.Id)) trial[s] = null;
            for (int s = start; s < start + size; s++) trial[s] = displaced;

            var plan = new List<(CharacterItem item, int slot)>();
            bool ok = true;
            foreach (var m in movers)
            {
                int len = Math.Max(1, m.SlotsRequired);
                int? dest = StartsNear(len, m.SlotIndex ?? 1).Cast<int?>()
                                .FirstOrDefault(s => Fits(trial, s!.Value, len));
                if (dest == null) { ok = false; break; }
                for (int s = dest.Value; s < dest.Value + len; s++) trial[s] = m;
                plan.Add((m, dest.Value));
            }
            if (!ok) continue;

            foreach (var (mover, slot) in plan) PlaceInBeltSlot(mover, slot);
            PlaceInBeltSlot(displaced, start);
            return true;
        }
        return false;
    }

    // Drag-and-drop: an item held in the hands is dropped on the belt.
    //  - The hovered belt item trades places with it (it goes to the hands the item just left).
    //  - A bulky item may also cover a second belt item; that item is pushed aside to the nearest
    //    free belt slot. Only if there is no room for it does it go to the hands too.
    //  - A hand item that can't be taken in (no free slots) falls back to the pack.
    // Anything not coming from the hands keeps the old behavior: displaced items go to the pack.
    public bool EquipToBeltWithSwap(int characterItemId, int hoveredSlot) => LoggedEquip(() => EquipToBeltWithSwapCore(characterItemId, hoveredSlot));
    private bool EquipToBeltWithSwapCore(int characterItemId, int hoveredSlot)
    {
        var item = _inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item == null) return false;
        if (PackBlocks(item)) return false;
        if (item.IsWorn || (item.IsArmor && !item.IsShield)) return false;
        if (item.IsEmpty && !item.IsEquipped) return false;

        int size = item.SlotsRequired;
        int start = Math.Min(hoveredSlot, 5 - size);
        if (start < 1) return false;

        if (item.EquipmentSlot != EquipmentSlotType.Hand)
            return EquipToBeltAtSlot(characterItemId, start);

        var hovered = GetEquippedBeltSlot(hoveredSlot);

        // Distinct belt items overlapping the slots this item would cover
        var inTarget = Enumerable.Range(start, size)
            .Select(s => GetEquippedBeltSlot(s))
            .Where(i => i != null && i.Id != item.Id)
            .Select(i => i!)
            .DistinctBy(i => i.Id)
            .ToList();

        int originHandSlot = item.SlotIndex ?? 1;

        var displaced = new List<CharacterItem>();                       // go to the hands
        var relocations = new List<(CharacterItem Item, int Start)>();   // pushed aside on the belt

        if (hovered != null) displaced.Add(hovered);

        // Overlapped but not hovered: push aside if possible, otherwise they go to the hands as well
        var others = inTarget.Where(i => hovered == null || i.Id != hovered.Id).ToList();
        if (others.Count > 0)
        {
            var plan = PlanBeltPushAside(others, hovered, start, size);
            if (plan != null) relocations = plan;
            else displaced.AddRange(others);
        }

        // 1. The dragged item leaves the hands (this also frees a two-handed set)
        UnequipItem(item.Id);

        // 2. Displaced belt items take its place in the hands, or fall back to the pack
        foreach (var other in displaced)
        {
            if (!TrySendToHands(other, originHandSlot))
            {
                UnequipItem(other.Id);
            }
        }

        // 3. Pushed-aside items shift, then the dragged item takes its place
        foreach (var (mover, newStart) in relocations)
        {
            PlaceInBeltSlot(mover, newStart);
        }

        PlaceInBeltSlot(item, start);
        return true;
    }

    // Finds a new belt position for each item that must make way for a bulky item. Items not in the
    // way never move. Each mover goes to the nearest free spot; returns null if any has nowhere to go.
    private List<(CharacterItem Item, int Start)>? PlanBeltPushAside(
        List<CharacterItem> movers, CharacterItem? leaving, int bulkyStart, int bulkySize)
    {
        var moverIds = movers.Select(m => m.Id).ToHashSet();
        var occupied = new bool[6];   // belt slots 1-4

        foreach (var b in _inventory.Where(i => i.IsEquipped && i.EquipmentSlot == EquipmentSlotType.Belt))
        {
            if (leaving != null && b.Id == leaving.Id) continue;   // being swapped out
            if (moverIds.Contains(b.Id)) continue;                 // will be re-placed below

            for (int s = b.SlotIndex!.Value; s < b.SlotIndex.Value + b.SlotsRequired; s++)
                occupied[s] = true;
        }

        for (int s = bulkyStart; s < bulkyStart + bulkySize; s++)
            occupied[s] = true;

        var plan = new List<(CharacterItem, int)>();
        foreach (var mover in movers.OrderBy(m => m.SlotIndex))
        {
            int need = mover.SlotsRequired;
            int current = mover.SlotIndex ?? 1;
            int? best = null;

            for (int candidate = 1; candidate + need - 1 <= 4; candidate++)
            {
                bool fits = true;
                for (int s = candidate; s < candidate + need; s++)
                {
                    if (occupied[s]) { fits = false; break; }
                }
                if (!fits) continue;

                if (!best.HasValue || Math.Abs(candidate - current) < Math.Abs(best.Value - current))
                    best = candidate;
            }

            if (!best.HasValue) return null;

            for (int s = best.Value; s < best.Value + need; s++)
                occupied[s] = true;

            plan.Add((mover, best.Value));
        }

        return plan;
    }
    // Sends a belted item to the hands. Returns false if the hands have no room for it.
    private bool TrySendToHands(CharacterItem belted, int preferredHandSlot)
    {
        if (belted.SlotsRequired > 1)
        {
            var setId = FindAvailablePairedLimbSetId();
            if (!setId.HasValue) return false;
            return EquipTwoHandedItemWithSwap(belted.Id, setId.Value);
        }

        int totalSlots = GetTotalLimbSlots();
        int? slot = preferredHandSlot >= 1 && preferredHandSlot <= totalSlots && GetEquippedHandSlot(preferredHandSlot) == null
            ? preferredHandSlot
            : FindAvailableHandSlot(1);

        if (!slot.HasValue) return false;

        PlaceInHandSlot(belted, slot.Value);
        return true;
    }

    // Clears a two-handed "occupied" flag whose item is no longer held in hand. Earlier
    // builds could leave one behind (and autosave stored it), so existing characters
    // may already carry a stuck set.
    public void RepairLimbSetState()
    {
        foreach (var limbSet in _limbSets)
        {
            if (!limbSet.TwoHandedItemId.HasValue) continue;

            var holder = _inventory.FirstOrDefault(i => i.Id == limbSet.TwoHandedItemId.Value);
            bool stillHeld = holder != null && holder.IsEquipped && holder.EquipmentSlot == EquipmentSlotType.Hand;
            if (!stillHeld) limbSet.TwoHandedItemId = null;
        }
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
            else if (startingItem.ItemGuid.HasValue)
            {
                try
                {
                    var task = _itemDataService?.GetItemByGuidAsync(startingItem.ItemGuid.Value);
                    template = task?.Result;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error resolving item {startingItem.ItemGuid}: {ex.Message}");
                }
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

            // Starting items begin with full uses
            int remainingUses = template.HasUses ? template.Uses!.Value : 0;

            // One record per physical item (or per legal stack)
            foreach (int chunk in InventorySlotCalculator.SplitQuantity(template, Math.Max(1, startingItem.Quantity)))
            {
                var characterItem = new CharacterItem
                {
                    Id = GetNextCharacterItemId(),
                    TemplateId = startingItem.ItemId,
                    Template = template,
                    Quantity = chunk,
                    PlayerNote = startingItem.PlayerNote,
                    RemainingUses = remainingUses,
                    UsageDieSides = template.HasUsageDie ? template.DefaultUsageDieSides : 0,
                    IsEmpty = false,
                    AcquiredAt = DateTime.Now
                };

                _inventory.Add(characterItem);
                System.Diagnostics.Debug.WriteLine($"Added starting item: {characterItem.DisplayName} x{chunk} with ID: {characterItem.Id}");
            }
        }

        System.Diagnostics.Debug.WriteLine($"=== Inventory initialized with {_inventory.Count} items from starting items ===");
    }

    // ====================================== END OF INVENTORY SYSTEM ====================================== //

    // Legacy saves have no CharacterGuid. Derive a stable one from the names so it
    // doesn't change between sessions even if the character is never re-saved.
    private static Guid DeriveLegacyCharacterGuid(string? playerName, string? characterName)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{playerName}|{characterName}"));
        return new Guid(hash);
    }

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
                CharacterGuid = CharacterGuid,
                CharacterName = CharacterName,
                LastSaved = DateTime.Now,
                IsComplete = IsCharacterComplete,
                IsSaved = true,
                CampaignType = CampaignType,
                CurrentPage = CurrentPage,
                Data = new CharacterData
                {
                    // ===== HINDERANCE DATA =====
                    // Saved as it is: a copy would reset LastModified, the hinderance's own fingerprint, on every save.
                    SelectedHinderance = SelectedHinderance,

                    SelectedRewardType = SelectedRewardType,
                    RewardStatBonusAmount = RewardStatBonusAmount,
                    RewardStatName = RewardStatName,

                    // ===== CAMPAIGN DATA =====
                    CampaignType = CampaignType,
                    CurrentPage = CurrentPage,
                    PlayerName = PlayerName,
                    PlayerId = PlayerId,
                    CharacterGuid = CharacterGuid,
                    CharacterName = CharacterName,
                    CharacterStory = CharacterStory,

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

                    ClassSelections = new Dictionary<int, int>(ClassSelections),
                    Favour = Favour,
                    DaysPassed = DaysPassed,
                    ShortRestsTaken = ShortRestsTaken,
                    FeatureUses = new List<FeatureUseRecord>(FeatureUses),
                    Transform = Transform,
                    SavedForms = new List<SavedForm>(SavedForms),
                    Proficiency = Proficiency,

                    // ===== ABILITY SCORES =====
                    StatVigor = StatVigor,
                    StatAgility = StatAgility,
                    StatMind = StatMind,
                    StatSpirit = StatSpirit,
                    Hitpoints = BaseHitpoints,
                    ASMStatVigor = ASMStatVigor,
                    ASMStatAgility = ASMStatAgility,
                    ASMStatMind = ASMStatMind,
                    ASMStatSpirit = ASMStatSpirit,

                    HitpointsDamage = HitpointsDamage,
                    CurrentVigorAdjustment = CurrentVigorAdjustment,
                    CurrentAgilityAdjustment = CurrentAgilityAdjustment,
                    CurrentMindAdjustment = CurrentMindAdjustment,
                    CurrentSpiritAdjustment = CurrentSpiritAdjustment,
                    FatigueCount = FatigueCount,
                    NeedsMet = NeedsMet,
                    Rested = Rested,
                    IsDeprived = IsDeprived,
                    DeprivedDays = DeprivedDays,
                    IsPackDropped = IsPackDropped,
                    ActiveEffects = ActiveEffects,
                    PendingDamage = new List<IncomingDamage>(PendingDamage),
                    PendingEffects = new List<IncomingEffect>(PendingEffects),

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
                    SelectedBackgroundId = SelectedBackgroundId,
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

                    SkillTrainingLevels = new Dictionary<string, int>(SkillTrainingLevels),
                    SkillTotals = SkillTrainingLevels.Keys.ToDictionary(skill => skill, GetSkillTotal),

                    // ===== TOTAL BONUSES =====
                    TotalVigorBonus = BGVigorBonus,
                    TotalAgilityBonus = BGAgilityBonus,
                    TotalMindBonus = BGMindBonus,
                    TotalSpiritBonus = BGSpiritBonus,
                    TotalTrainingPointsBonus = AvailableTrainingPoints,
                    UnspentTrainingPoints = UnspentTrainingPoints,
                    CreationTrainingMigrated = _creationTrainingMigrated,
                    PendingLevels = PendingLevels,
                    StatRolls = new List<int>(StatRolls),
                    StatRollPool = new List<int>(StatRollPool),

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
    public async Task PopulateFromSaveDataAsync(CharacterSaveData saveData, bool startSession = true)
    {
        if (saveData?.Data == null) return;

        System.Diagnostics.Debug.WriteLine("=== POPULATING FROM SAVE DATA ===");

        // ===== HINDERANCE DATA =====
        SelectedHinderance = saveData.Data.SelectedHinderance;
        SelectedRewardType = saveData.Data.SelectedRewardType;
        RewardStatBonusAmount = saveData.Data.RewardStatBonusAmount;
        RewardStatName = saveData.Data.RewardStatName;

        // Strings
        CampaignType = saveData.Data.CampaignType ?? string.Empty;
        PlayerName = saveData.Data.PlayerName ?? string.Empty;
        PlayerId = saveData.Data.PlayerId != Guid.Empty
            ? saveData.Data.PlayerId
            : InstallIdentity.GetOrCreateInstallPlayerId();
        CharacterGuid = saveData.Data.CharacterGuid != Guid.Empty
            ? saveData.Data.CharacterGuid
            : DeriveLegacyCharacterGuid(saveData.Data.PlayerName, saveData.Data.CharacterName);
        FileName = saveData.FileName;
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

        // Feature resources. Older saves have none: the original class counts as picked once and
        // a Shaman starts with the Favour its class grants.
        bool legacy = saveData.Data.ClassSelections == null || saveData.Data.ClassSelections.Count == 0;
        ClassSelections = !legacy
            ? new Dictionary<int, int>(saveData.Data.ClassSelections!)
            : SelectedClassId.HasValue
                ? new Dictionary<int, int> { { SelectedClassId.Value, 1 } }
                : new Dictionary<int, int>();
        Favour = legacy ? (GetSelectedClass()?.FavourPerSelection ?? 0) : saveData.Data.Favour;
        DaysPassed = saveData.Data.DaysPassed;
        ShortRestsTaken = saveData.Data.ShortRestsTaken;
        FeatureUses = saveData.Data.FeatureUses ?? new List<FeatureUseRecord>();
        Transform = saveData.Data.Transform;
        SavedForms = saveData.Data.SavedForms ?? new List<SavedForm>();

        // Ability Scores
        StatVigor = saveData.Data.StatVigor;
        StatAgility = saveData.Data.StatAgility;
        StatMind = saveData.Data.StatMind;
        StatSpirit = saveData.Data.StatSpirit;
        ASMStatVigor = saveData.Data.ASMStatVigor;
        ASMStatAgility = saveData.Data.ASMStatAgility;
        ASMStatMind = saveData.Data.ASMStatMind;
        ASMStatSpirit = saveData.Data.ASMStatSpirit;

        HitpointsDamage = saveData.Data.HitpointsDamage;
        CurrentVigorAdjustment = saveData.Data.CurrentVigorAdjustment;
        CurrentAgilityAdjustment = saveData.Data.CurrentAgilityAdjustment;
        CurrentMindAdjustment = saveData.Data.CurrentMindAdjustment;
        CurrentSpiritAdjustment = saveData.Data.CurrentSpiritAdjustment;
        FatigueCount = saveData.Data.FatigueCount;
        NeedsMet = saveData.Data.NeedsMet;
        Rested = saveData.Data.Rested;
        IsDeprived = saveData.Data.IsDeprived;
        DeprivedDays = saveData.Data.DeprivedDays;
        IsPackDropped = saveData.Data.IsPackDropped;
        ActiveEffects = saveData.Data.ActiveEffects ?? new List<CharacterEffect>();
        Proficiency = saveData.Data.Proficiency ?? new WeaponProficiencyState();
        PendingDamage = saveData.Data.PendingDamage ?? new List<IncomingDamage>();
        PendingEffects = saveData.Data.PendingEffects ?? new List<IncomingEffect>();

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
        SelectedBackgroundId = saveData.Data.SelectedBackgroundId;
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
        CharacterStory = saveData.Data.CharacterStory ?? string.Empty;

        // ===== FAMILIARS =====
        AcquiredFamiliars = saveData.Data.AcquiredFamiliars ?? new List<Familiar>();

        // ===== TRAINING POINTS =====
        UnspentTrainingPoints = saveData.Data.UnspentTrainingPoints;
        PendingLevels = saveData.Data.PendingLevels;
        StatRolls = saveData.Data.StatRolls ?? new List<int>();
        StatRollPool = saveData.Data.StatRollPool ?? new List<int>();
        _creationTrainingMigrated = saveData.Data.CreationTrainingMigrated;
        if (!_creationTrainingMigrated && saveData.IsComplete) MigrateCreationTrainingPoints();
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

        foreach (var item in Inventory)
        {
            try
            {
                item.Template = item.TemplateGuid.HasValue
                    ? await _itemDataService.GetItemByGuidAsync(item.TemplateGuid.Value)
                    : await _itemDataService.GetItemByIdAsync(item.TemplateId);

                if (!item.TemplateGuid.HasValue && item.Template != null && item.Template.IsPlayerCreated)
                {
                    item.TemplateGuid = item.Template.Guid;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resolving item template for {item.TemplateId}/{item.TemplateGuid}: {ex.Message}");
            }
        }

        // ===== FAMILIAR INVENTORIES =====
        // Templates aren't saved, so familiar packs need the same lookup the character pack gets.
        foreach (var familiar in AcquiredFamiliars)
        {
            familiar.Inventory ??= new List<CharacterItem>();

            foreach (var item in familiar.Inventory)
            {
                try
                {
                    item.Template = item.TemplateGuid.HasValue
                        ? await _itemDataService.GetItemByGuidAsync(item.TemplateGuid.Value)
                        : await _itemDataService.GetItemByIdAsync(item.TemplateId);

                    if (!item.TemplateGuid.HasValue && item.Template != null && item.Template.IsPlayerCreated)
                    {
                        item.TemplateGuid = item.Template.Guid;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error resolving familiar item template for {item.TemplateId}/{item.TemplateGuid}: {ex.Message}");
                }
            }
        }

        MigrateInventoryRecords();

        // Persistence flags
        IsCharacterComplete = saveData.IsComplete;
        IsCharacterSaved = true;
        SaveFileName = saveData.FileName;
        LastSavedDate = saveData.LastSaved;
        // Every load starts a new session; recoverables from earlier sessions are gone for good.
        if (startSession)
        {
            StartSession();
            await _recoveryService.PurgePlayerRecoverablesAsync(PlayerId, SessionId);
        }

        System.Diagnostics.Debug.WriteLine("=== POPULATE COMPLETE ===");
    }

    // === CLEAR ===

    public void ClearCharacterCreationData()
    {
        System.Diagnostics.Debug.WriteLine("=== CLEARING ALL CHARACTER CREATION DATA ===");

        // === HINDERANCE DATA ===
        _selectedHinderance = null;
        SelectedRewardType = HinderanceRewardType.None;
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
        CharacterGuid = Guid.NewGuid();
        FileName = string.Empty;
        CharacterName = string.Empty;
        CurrentPage = string.Empty;
        _sessionId = Guid.Empty;

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

        HitpointsDamage = 0;
        _encumbranceApplied = false;
        CurrentVigorAdjustment = 0;
        CurrentAgilityAdjustment = 0;
        CurrentMindAdjustment = 0;
        CurrentSpiritAdjustment = 0;
        FatigueCount = 0;
        NeedsMet = true;
        Rested = false;
        IsDeprived = false;
        DeprivedDays = 0;
        ActiveEffects = new List<CharacterEffect>();
        PendingDamage = new List<IncomingDamage>();
        PendingEffects = new List<IncomingEffect>();
        IsPackDropped = false;

        // === CLASS & FEATURE IDs ===
        SelectedClassId = null;
        AcquiredFeatureIds.Clear();
        ClassSelections = new Dictionary<int, int>();
        Favour = 0;
        DaysPassed = 0;
        ShortRestsTaken = 0;
        FeatureUses = new List<FeatureUseRecord>();
        Transform = null;
        SavedForms = new List<SavedForm>();
        Proficiency = new WeaponProficiencyState();
        
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
        SelectedBackgroundId = null;
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
        CharacterStory = string.Empty;

        // === FAMILIARS ===
        _acquiredFamiliars.Clear();

        // === TRAINING POINTS ===
        UnspentTrainingPoints = 0;
        PendingLevels = 0;
        StatRolls = new List<int>();
        StatRollPool = new List<int>();
        _creationTrainingMigrated = false;
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
        int clampedScore = Math.Clamp(score, 1, 30);

        // For negative numbers, we want floor, not truncate
        int modifier = (clampedScore - 10) / 2;

        if (clampedScore < 10 && (clampedScore - 10) % 2 != 0)
        {
            modifier--;
        }

        return Math.Clamp(modifier, -5, 10);
    }
}