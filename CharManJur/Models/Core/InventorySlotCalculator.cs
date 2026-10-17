namespace CharManJur.Models;

/// <summary>Pure pack-slot math. One call per record (a record is one physical item or one stack).</summary>
public static class InventorySlotCalculator
{
    private static bool HasLimit(Item t) => t.IsStackableItem && t.QtyLimit.HasValue && t.QtyLimit.Value > 0;

    /// <summary>Pack slots one record occupies.</summary>
    public static int SlotsFor(Item template, int quantity)
    {
        if (quantity <= 0) return 0;
        if (template.Size == ItemSize.Bulky) return 2;

        if (template.Size == ItemSize.Regular)
            return HasLimit(template) && quantity > template.QtyLimit!.Value ? 2 : 1;   // over the limit -> Bulky

        // Petty: reaching the limit makes the stack Regular size
        return HasLimit(template) && quantity >= template.QtyLimit!.Value ? 1 : 0;
    }

    /// <summary>The size a record effectively is right now (drives the tile's size label).</summary>
    public static ItemSize EffectiveSize(Item template, int quantity)
    {
        return SlotsFor(template, quantity) switch
        {
            0 => ItemSize.Petty,
            1 => ItemSize.Regular,
            _ => ItemSize.Bulky
        };
    }

    /// <summary>Largest quantity a single record may hold. Beyond this, a new record is created.</summary>
    public static int MaxPerRecord(Item template)
    {
        if (!template.IsStackableItem) return 1;
        if (!HasLimit(template)) return int.MaxValue;

        long limit = template.QtyLimit!.Value;
        long max = template.Size == ItemSize.Regular ? limit * 2 : limit;   // Regular may grow into Bulky first
        return (int)Math.Min(max, int.MaxValue);
    }

    /// <summary>Splits a quantity into legal record-sized chunks.</summary>
    public static List<int> SplitQuantity(Item template, int quantity)
    {
        var chunks = new List<int>();
        int max = MaxPerRecord(template);
        while (quantity > 0)
        {
            int chunk = Math.Min(quantity, max);
            chunks.Add(chunk);
            quantity -= chunk;
        }
        return chunks;
    }

    /// <summary>Pack capacity after fatigue overflow. Never below zero.</summary>
    public static int AvailableSlots(int totalSlots, int fatigueOverflow)
        => Math.Max(0, totalSlots - Math.Max(0, fatigueOverflow));
}