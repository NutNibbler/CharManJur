using System;
using System.Collections.Generic;

namespace CharManJur.Models;

// Dice pools classes grant. They are re-usable (never used up): the size is the cap on dice rolled at once.
public enum DicePool { Magic, Spirit, Polymorph }

public enum CooldownPeriod { ShortRest, Day }
public enum FeatureCostType { Fatigue, Favour, Item }
public enum ConditionCheck { None, NoArmorWorn, HoldingShield }

public static class DicePools
{
    public static string Name(DicePool pool) => pool switch
    {
        DicePool.Magic => "Magic Dice",
        DicePool.Spirit => "Spirit Dice",
        _ => "Polymorph Dice"
    };
}

// A guideline, never a block: using a feature past it warns, then allows, and is logged as rule skating.
public class FeatureCooldown
{
    public int Uses { get; set; } = 1;
    public CooldownPeriod Period { get; set; }
}

public class FeatureCost
{
    public FeatureCostType Type { get; set; }
    public int Amount { get; set; } = 1;     // the fixed amount, or the minimum when Variable
    public bool Variable { get; set; }       // the player chooses how much (Favour)
    public int? ItemId { get; set; }         // Item costs: the item template id
    public string? ItemName { get; set; }
}

// A dice pool roll. Every die has consequences, set here so a custom feature can change them.
public class FeatureRoll
{
    public DicePool Pool { get; set; }
    public int MinDice { get; set; } = 1;
    public bool Variable { get; set; }         // false = exactly MinDice dice
    public int FatiguePerHighDie { get; set; }    // per die showing 4-6
    public int SpiritLossPerLowDie { get; set; }  // per die showing 1-3
    public int SpiritLossPerHighDie { get; set; } // per die showing 4-6

    public static FeatureRoll For(DicePool pool, int minDice = 1, bool variable = false)
        => pool == DicePool.Spirit
            ? new FeatureRoll { Pool = pool, MinDice = minDice, Variable = variable, SpiritLossPerLowDie = 1, SpiritLossPerHighDie = 2 }
            : new FeatureRoll { Pool = pool, MinDice = minDice, Variable = variable, FatiguePerHighDie = 1 };
}

// Check = None is a plain reminder; anything else is verified and warns when it isn't met.
public class FeatureCondition
{
    public string Text { get; set; } = string.Empty;
    public ConditionCheck Check { get; set; }
}

public class FeatureBehavior
{
    public bool IsPassive { get; set; }                    // always on: no Use button
    public FeatureCooldown? Cooldown { get; set; }
    public List<FeatureCost> Costs { get; set; } = new();
    public FeatureRoll? Roll { get; set; }
    public List<FeatureCondition> Conditions { get; set; } = new();
    public string? UseMessage { get; set; }                // tokens: [dice] [sum] [low] [high] [favour] [total]
    public bool NotifyParty { get; set; }
    public bool IsSustained { get; set; }                  // adds an effect chip the player clears
    public bool RequiresInvestment { get; set; }           // at least one die or one Favour must be invested
}

// One use of a feature, stamped with the rest and day counters it happened under.
public class FeatureUseRecord
{
    public int FeatureId { get; set; }
    public int ShortRestMark { get; set; }
    public int DayMark { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.Now;
}

// Behaviors of the foundation features, by feature id. A feature missing here is a plain log-only Use.
public static class FoundationFeatureBehaviors
{
    private static readonly FeatureBehavior LogOnly = new();

    private static FeatureBehavior Passive() => new() { IsPassive = true };
    private static FeatureCooldown Per(int uses, CooldownPeriod period) => new() { Uses = uses, Period = period };
    private static FeatureCost Fatigue(int amount) => new() { Type = FeatureCostType.Fatigue, Amount = amount };
    private static FeatureCost ChosenFavour() => new() { Type = FeatureCostType.Favour, Amount = 0, Variable = true };
    private static FeatureCost Vial() => new() { Type = FeatureCostType.Item, Amount = 1, ItemId = 128, ItemName = "Vial of Vym" };

    private static readonly Dictionary<int, FeatureBehavior> Table = new()
    {
        // ----- Warrior -----
        [1] = Passive(),   // Ironclad
        [2] = new()        // Deafening Roar
        {
            Cooldown = Per(1, CooldownPeriod.Day),
            UseMessage = "Enemies near you make a Morale Check (your GM runs it). Enemies that fail make impaired damage rolls against creatures other than you."
        },
        [3] = new()        // Rage
        {
            Cooldown = Per(1, CooldownPeriod.ShortRest),
            Conditions = { new FeatureCondition { Text = "Not wearing armor", Check = ConditionCheck.NoArmorWorn } },
            IsSustained = true,
            UseMessage = "You gain 1 armor and a bonus weapon die. Rage lasts until you stop attacking on one of your turns."
        },
        [4] = new()        // Defender
        {
            Cooldown = Per(2, CooldownPeriod.ShortRest),
            Conditions = { new FeatureCondition { Text = "Holding a shield", Check = ConditionCheck.HoldingShield } },
            UseMessage = "You make the block reaction for a close ally."
        },
        [5] = new()        // Berserker
        {
            Costs = { Fatigue(3) },
            Conditions = { new FeatureCondition { Text = "When combat ends, roll on the wounds table." } },
            UseMessage = "You ignore the consequences of critical damage and raise your weapon die. Damage still goes to your VIG as normal."
        },
        [6] = new()        // Sentinel
        {
            Cooldown = Per(2, CooldownPeriod.ShortRest),
            Costs = { Fatigue(1) },
            UseMessage = "A close creature of similar size can't leave your vicinity."
        },
        [7] = Passive(),   // Tank
        [8] = Passive(),   // Brawler
        [9] = Passive(),   // Pack Mule
        [31] = Passive(),  // Resourceful

        // ----- Rogue -----
        [10] = LogOnly,    // Guerrilla
        [11] = new()       // Stunning Strike
        {
            Costs = { Fatigue(1) },
            UseMessage = "An enemy creature loses 1 AP on their next turn."
        },
        [12] = LogOnly,    // Quick Shot
        [13] = Passive(),  // Shadow Step
        [14] = LogOnly,    // Deadeye
        [15] = new() { Costs = { Fatigue(1) } },   // Quickened Step
        [16] = LogOnly,    // Swift Strike
        [17] = Passive(),  // Duelist
        [18] = new()       // Evasion
        {
            Costs = { Fatigue(3) },
            UseMessage = "You fully negate the damage of a blast effect no greater than close range."
        },

        // ----- Shaman -----
        [21] = new() { UseMessage = "An ally you can touch gets +1 on their next skill check." },   // Guidance
        [22] = new()       // Mirror Image
        {
            Roll = FeatureRoll.For(DicePool.Spirit, 1, variable: true),
            IsSustained = true,
            UseMessage = "[dice] duplicate(s) of you appear within near distance. They can't attack and vanish if they take damage."
        },
        [23] = new()       // Smite
        {
            Roll = FeatureRoll.For(DicePool.Spirit, 0, variable: true),
            Costs = { ChosenFavour() },
            RequiresInvestment = true,
            UseMessage = "Damage dealt directly to the target's SPI: [total] ([sum] from Spirit Dice + [favour] Favour). Spirit loss rules still apply to them."
        },
        [24] = new()       // Consecrate Weapon
        {
            Roll = FeatureRoll.For(DicePool.Spirit, 1),
            Costs = { ChosenFavour() },
            UseMessage = "Favour invested in your next attack with the weapon: [favour]. Spirit Die rolled: [sum]. Tell your GM."
        },
        [25] = new()       // Co-Host
        {
            IsSustained = true,
            Conditions = { new FeatureCondition { Text = "Roll a Spirit Die when the creature dies." } }
        },
        [26] = new()       // Aegis
        {
            Roll = FeatureRoll.For(DicePool.Spirit, 1, variable: true),
            NotifyParty = true,
            UseMessage = "Restore ability points for your allies: each of your [low] dice showing 1-3 restores 1, and each of your [high] dice showing 4-6 restores 2."
        },

        // ----- Artificer (27 Blueprint is a sub-feature) -----
        [28] = new() { Costs = { Vial() } },   // Triple Point
        [29] = new() { UseMessage = "The item you created is destroyed. Any roll associated with it is enhanced or made with advantage." },   // Overcharge
        [30] = new()       // Enchantment
        {
            Costs = { Vial() },
            UseMessage = "Tell your GM which other material you're using for the enchantment."
        }
    };

    public static FeatureBehavior For(ClassFeature feature)
        => feature.Behavior ?? (Table.TryGetValue(feature.Id, out var behavior) ? behavior : LogOnly);
}