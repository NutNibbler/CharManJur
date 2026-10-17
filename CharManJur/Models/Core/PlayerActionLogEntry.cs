using System;
using System.Text.Json.Serialization;

namespace CharManJur.Models;

public enum PlayerActionType
{
    ItemDropped,
    ItemBestowed,
    ItemRecovered,
    DamageTaken,
    VigorLost,
    AgilityLost,
    MindLost,
    SpiritLost,
    CharacterDied,      // Vigor reaches 0
    Paralyzed,           // Agility reaches 0
    MadnessOnset,        // Mind reaches 0
    EmptinessOnset,      // Spirit reaches 0
    AbilityScoreRestored,
    ShortRestTaken,
    LongRestTaken,
    FatigueGained,
    FatigueCleared,
    EffectApplied,
    EffectCleared,
    CombatReactionUsed,
    VigorSaveTriggered,
    VigorSavePassed,
    VigorSaveFailed,
    DeathsDoorCleared,
    SkillCheckRolled,
    AbilitySaveRolled,
    EffectEdited,
    PackDropped,
    PackRecovered,
    FamiliarBestowed,
    FamiliarReleased,
    FamiliarDied,
    ItemHandedToFamiliar,
    ItemTakenFromFamiliar,
    ItemSentToPlayer,
    ItemReceivedFromPlayer,
    ItemEquipped,
    ItemUnequipped,
    ItemMoved,
    ItemEdited,
    ItemPurchased,
    ItemSold,
    SubFeatureBestowed,
    SubFeatureForgotten,
    SubFeatureCreated,
    SubFeatureUsed,
    BlueprintCraftedItem,
    SandboxCraftedItem,
    ConstructCrafted,
    ItemConsumed,
    ItemRefilled,
    FeatureUsed,
    RuleBypassed,
    DayPassed,
    FavourChanged,
    Transformed,
    StabilityDamaged,
    StabilitySaveRolled,
    Reverted
}

public enum ActionSource
{
    Player,
    GM
}

public class PlayerActionLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public PlayerActionType ActionType { get; set; }
    public Guid? SessionId { get; set; }
    public ActionSource Source { get; set; } = ActionSource.Player;
    // Set when a feature's effect is meant for the whole party (relayed to everyone once multiplayer exists).
    public bool NotifyParty { get; set; }

    [JsonIgnore]
    public bool IsRuleSkating => ActionType == PlayerActionType.RuleBypassed;

    public string PlayerName { get; set; } = string.Empty;
    public Guid PlayerId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public Guid CharacterGuid { get; set; }

    // Item-specific fields; left at defaults for non-item events.
    public string? ItemName { get; set; }
    public int? ItemId { get; set; }   // TemplateId, per earlier decision

    // The human-readable sentence a GM reads. Written once at log time and
    // never recomputed, so later renames or deletions can't rewrite history.
    public string Summary { get; set; } = string.Empty;

    private string ActionTypeDisplay => ActionType switch
    {
        PlayerActionType.ItemDropped => "ITEM DROPPED",
        PlayerActionType.ItemBestowed => "ITEM BESTOWED",
        PlayerActionType.ItemRecovered => "ITEM RECOVERED",
        PlayerActionType.DamageTaken => "DAMAGE TAKEN",
        PlayerActionType.VigorLost => "VIGOR LOST",
        PlayerActionType.AgilityLost => "AGILITY LOST",
        PlayerActionType.MindLost => "MIND LOST",
        PlayerActionType.SpiritLost => "SPIRIT LOST",
        PlayerActionType.Paralyzed => "PARALYZED",
        PlayerActionType.MadnessOnset => "MADNESS ONSET",
        PlayerActionType.EmptinessOnset => "EMPTINESS ONSET",
        PlayerActionType.CharacterDied => "CHARACTER DIED",
        PlayerActionType.AbilityScoreRestored => "ABILITY SCORE RESTORED",
        PlayerActionType.ShortRestTaken => "SHORT REST",
        PlayerActionType.LongRestTaken => "LONG REST",
        PlayerActionType.FatigueGained => "FATIGUE GAINED",
        PlayerActionType.FatigueCleared => "FATIGUE CLEARED",
        PlayerActionType.EffectApplied => "EFFECT APPLIED",
        PlayerActionType.EffectCleared => "EFFECT CLEARED",
        PlayerActionType.CombatReactionUsed => "COMBAT REACTION",
        PlayerActionType.VigorSaveTriggered => "VIGOR SAVE REQUIRED",
        PlayerActionType.VigorSavePassed => "VIGOR SAVE PASSED",
        PlayerActionType.VigorSaveFailed => "VIGOR SAVE FAILED",
        PlayerActionType.DeathsDoorCleared => "DEATH'S DOOR CLEARED",
        PlayerActionType.SkillCheckRolled => "SKILL CHECK",
        PlayerActionType.AbilitySaveRolled => "ABILITY SAVE",
        PlayerActionType.EffectEdited => "EFFECT EDITED",
        PlayerActionType.PackDropped => "PACK DROPPED",
        PlayerActionType.PackRecovered => "PACK RECOVERED",
        PlayerActionType.FamiliarBestowed => "FAMILIAR BESTOWED",
        PlayerActionType.FamiliarReleased => "FAMILIAR RELEASED",
        PlayerActionType.FamiliarDied => "FAMILIAR DIED",
        PlayerActionType.ItemHandedToFamiliar => "ITEM HANDED TO FAMILIAR",
        PlayerActionType.ItemTakenFromFamiliar => "ITEM TAKEN FROM FAMILIAR",
        PlayerActionType.ItemSentToPlayer => "ITEM SENT TO PLAYER",
        PlayerActionType.ItemReceivedFromPlayer => "ITEM RECEIVED FROM PLAYER",
        PlayerActionType.ItemEquipped => "ITEM EQUIPPED",
        PlayerActionType.ItemUnequipped => "ITEM UNEQUIPPED",
        PlayerActionType.ItemMoved => "ITEM MOVED",
        PlayerActionType.ItemEdited => "ITEM EDITED",
        PlayerActionType.ItemPurchased => "ITEM PURCHASED",
        PlayerActionType.ItemSold => "ITEM SOLD",
        PlayerActionType.SubFeatureBestowed => "SUB-FEATURE BESTOWED",
        PlayerActionType.SubFeatureForgotten => "SUB-FEATURE FORGOTTEN",
        PlayerActionType.SubFeatureCreated => "SUB-FEATURE CREATED",
        PlayerActionType.SubFeatureUsed => "SUB-FEATURE USED",
        PlayerActionType.BlueprintCraftedItem => "BLUEPRINT CRAFTED ITEM",
        PlayerActionType.SandboxCraftedItem => "SANDBOX CRAFTED ITEM",
        PlayerActionType.ConstructCrafted => "CONSTRUCT CRAFTED",
        PlayerActionType.ItemConsumed => "ITEM CONSUMED",
        PlayerActionType.ItemRefilled => "ITEM REFILLED",
        PlayerActionType.FeatureUsed => "FEATURE USED",
        PlayerActionType.RuleBypassed => "RULE SKATING",
        PlayerActionType.DayPassed => "DAY PASSED",
        PlayerActionType.FavourChanged => "FAVOUR CHANGED",
        PlayerActionType.Transformed => "TRANSFORMED",
        PlayerActionType.StabilityDamaged => "STABILITY DAMAGE",
        PlayerActionType.StabilitySaveRolled => "STABILITY SAVE",
        PlayerActionType.Reverted => "REVERTED",
        _ => ActionType.ToString()
    };

    public string FormattedEntry =>
        $"{Timestamp:u} | {ActionTypeDisplay} - {PlayerName} ({CharacterName}) | {PlayerId} / {CharacterGuid} : {Summary}";

    // ===== PLAYER-FACING DISPLAY (the Event Tray) =====
    [JsonIgnore]
    public string FriendlyLabel =>
        System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ActionTypeDisplay.ToLowerInvariant());

    [JsonIgnore]
    public string Icon => ActionType switch
    {
        PlayerActionType.ItemDropped or PlayerActionType.ItemBestowed or PlayerActionType.ItemRecovered
            or PlayerActionType.ItemHandedToFamiliar or PlayerActionType.ItemTakenFromFamiliar
            or PlayerActionType.ItemSentToPlayer or PlayerActionType.ItemReceivedFromPlayer
            or PlayerActionType.ItemEdited => "📦",
        PlayerActionType.ItemEquipped or PlayerActionType.ItemUnequipped or PlayerActionType.ItemMoved
            or PlayerActionType.PackDropped or PlayerActionType.PackRecovered => "🎒",
        PlayerActionType.ItemPurchased => "🛒",
        PlayerActionType.ItemSold => "💰",
        PlayerActionType.DamageTaken or PlayerActionType.VigorLost or PlayerActionType.AgilityLost
            or PlayerActionType.MindLost or PlayerActionType.SpiritLost => "💥",
        PlayerActionType.CharacterDied or PlayerActionType.Paralyzed or PlayerActionType.MadnessOnset
            or PlayerActionType.EmptinessOnset or PlayerActionType.FamiliarDied => "☠️",
        PlayerActionType.AbilityScoreRestored => "✨",
        PlayerActionType.ShortRestTaken or PlayerActionType.LongRestTaken => "🏕️",
        PlayerActionType.FatigueGained or PlayerActionType.FatigueCleared => "💤",
        PlayerActionType.EffectApplied or PlayerActionType.EffectCleared or PlayerActionType.EffectEdited => "🌀",
        PlayerActionType.CombatReactionUsed => "⚔️",
        PlayerActionType.VigorSaveTriggered or PlayerActionType.VigorSavePassed or PlayerActionType.VigorSaveFailed
            or PlayerActionType.DeathsDoorCleared or PlayerActionType.SkillCheckRolled
            or PlayerActionType.AbilitySaveRolled => "🎲",
        PlayerActionType.FamiliarBestowed or PlayerActionType.FamiliarReleased => "🐾",
        PlayerActionType.SubFeatureBestowed or PlayerActionType.SubFeatureForgotten
            or PlayerActionType.SubFeatureCreated or PlayerActionType.SubFeatureUsed => "📜",
        PlayerActionType.BlueprintCraftedItem or PlayerActionType.SandboxCraftedItem
            or PlayerActionType.ConstructCrafted => "🔨",
        PlayerActionType.ItemConsumed => "➖",
        PlayerActionType.ItemRefilled => "➕",
        PlayerActionType.FeatureUsed => "⭐",
        PlayerActionType.RuleBypassed => "🛹",
        PlayerActionType.DayPassed => "☀️",
        PlayerActionType.FavourChanged => "🙏",
        PlayerActionType.Transformed or PlayerActionType.Reverted => "🐺",
        PlayerActionType.StabilityDamaged => "💥",
        PlayerActionType.StabilitySaveRolled => "🎲",
        _ => "•"
    };

}