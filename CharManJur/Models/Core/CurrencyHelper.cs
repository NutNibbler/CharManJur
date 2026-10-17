using System;
using System.Collections.Generic;
using System.Linq;

namespace CharManJur.Models;

// All currency math lives here so the dashboard and the shop count money the same way.
public static class CurrencyHelper
{
    public const int ChipId = 1;
    public const int RupeeId = 2;
    public const int MarkId = 3;
    public const int NoteId = 4;

    public const int ChipsPerMark = 60;
    public const int MinNoteMarks = 10;
    public const int MaxNoteMarks = 1_000_000;

    public const int ChipsPerRupee = 5;

    public static int ChipsPerUnit(CurrencyDisplayUnit unit) => unit switch
    {
        CurrencyDisplayUnit.Rupees => ChipsPerRupee,
        CurrencyDisplayUnit.Marks => ChipsPerMark,
        _ => 1
    };

    public static string PluralName(CurrencyDisplayUnit unit) => unit switch
    {
        CurrencyDisplayUnit.Rupees => "Rupees",
        CurrencyDisplayUnit.Marks => "Marks",
        _ => "Chips"
    };

    public static string SingularName(CurrencyDisplayUnit unit) => unit switch
    {
        CurrencyDisplayUnit.Rupees => "Rupee",
        CurrencyDisplayUnit.Marks => "Mark",
        _ => "Chip"
    };

    public static string UnitIcon(CurrencyDisplayUnit unit) => unit switch
    {
        CurrencyDisplayUnit.Rupees => "▲",
        CurrencyDisplayUnit.Marks => "▮",
        _ => "●"
    };

    public static string UnitColorHex(CurrencyDisplayUnit unit) => unit switch
    {
        CurrencyDisplayUnit.Rupees => "#c0c0c0",   // silver
        CurrencyDisplayUnit.Marks => "#e8e0c0",    // stamped paper
        _ => "#b87333"                              // copper
    };

    // "1.67 Marks", "20 Rupees", "1 Mark", "0 Chips": up to two decimals, trailing zeros trimmed.
    // Two decimals never round up to a false whole number (the smallest fraction is 1/60).
    public static string FormatAmount(long chips, CurrencyDisplayUnit unit)
    {
        decimal amount = Math.Round(chips / (decimal)ChipsPerUnit(unit), 2, MidpointRounding.AwayFromZero);
        string name = amount == 1m ? SingularName(unit) : PluralName(unit);
        return $"{amount.ToString("#,0.##")} {name}";
    }

    // Shop price: the base price in chips raised by the DM's inflation modifier
    // (0 = none, 0.2 = +20%), rounded down. Decimal math, so 0.2 never drifts. Apply per item, in chips.
    public static long ApplyInflation(long baseChips, decimal inflation)
        => Math.Max(0L, (long)Math.Floor(baseChips * (1m + inflation)));

    public static bool IsCurrency(CharacterItem item) => item.Template?.Category == ItemCategory.Currency;

    // One item's worth in chips. A Note's written value overrides its template value.
    public static int ValueEach(CharacterItem item)
    {
        if (item.Template == null) return 0;
        if (item.Template.IsValueEditable)
            return item.WrittenValueInChips ?? item.Template.ValueInChips ?? 0;
        return item.Template.ValueInChips ?? 0;
    }

    // Counts everything in the pack, equipped currency included. Familiar packs and the
    // recoverables table aren't part of the list passed in, so they never count.
    public static long TotalChips(IEnumerable<CharacterItem> items)
        => items.Where(i => !i.IsDropped && IsCurrency(i)).Sum(i => (long)ValueEach(i) * i.Quantity);

    // How many of one built-in denomination (by its foundation item id).
    public static int Count(IEnumerable<CharacterItem> items, int foundationItemId)
        => items.Where(i => !i.IsDropped && IsCurrency(i) && !i.TemplateGuid.HasValue && i.TemplateId == foundationItemId)
                .Sum(i => i.Quantity);

    // Parses a Note value typed in Marks. False if it's not a whole number in range.
    public static bool TryParseNoteMarks(string? text, out int chips)
    {
        chips = 0;
        if (!int.TryParse(text?.Trim(), out int marks) || marks < MinNoteMarks || marks > MaxNoteMarks)
            return false;
        chips = marks * ChipsPerMark;
        return true;
    }
}