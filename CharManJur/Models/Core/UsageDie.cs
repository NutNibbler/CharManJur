namespace CharManJur.Models;

// Rules for usage dice. The die belongs to each item instance and is rolled whenever the item calls for it
// (end of combat is one trigger). A 1 or 2 steps the die down one size, and a D4 that does so is used up.
public static class UsageDie
{
    public static readonly int[] Sizes = { 4, 6, 8, 10, 12, 20 };

    public static string Label(int sides) => $"D{sides}";

    public static bool IsValid(int sides) => Array.IndexOf(Sizes, sides) >= 0;

    public static bool IsConsumingRoll(int roll) => roll <= 2;

    // The next die down, or null when this is a D4 (stepping down means the item is used up).
    public static int? StepDown(int sides)
    {
        int index = Array.IndexOf(Sizes, sides);
        return index > 0 ? Sizes[index - 1] : null;
    }

    // Price weight by die size. Tune the numbers here; the shop scales a listed price by these.
    public static int PriceWeight(int sides) => sides switch
    {
        4 => 1,
        6 => 2,
        8 => 3,
        10 => 4,
        12 => 5,
        20 => 8,
        _ => 1
    };

    // Price for a die of `sides`, given the price listed at the item's own default die.
    public static long ScaledPrice(long priceAtDefaultDie, int defaultSides, int sides)
        => Math.Max(1, (long)Math.Round(
            priceAtDefaultDie * (double)PriceWeight(sides) / PriceWeight(defaultSides),
            MidpointRounding.AwayFromZero));
}

public record UsageDieOutcome(string ItemName, int Sides, int Roll, bool Consumed, int? NewSides, bool Depleted, int? UsesLeft = null)
{
    // A hybrid item spends a Use instead of shrinking its die
    public bool IsHybrid => UsesLeft.HasValue;

    public string Summary
    {
        get
        {
            string head = $"{ItemName}: rolled {Roll} on {UsageDie.Label(Sides)}.";
            if (!Consumed) return $"{head} Kept.";
            if (IsHybrid) return UsesLeft == 0 ? $"{head} Last use spent. Now empty." : $"{head} A use was spent ({UsesLeft} left).";
            if (Depleted) return $"{head} Used up and lost.";
            return $"{head} Now {UsageDie.Label(NewSides!.Value)}.";
        }
    }
}