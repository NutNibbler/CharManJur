using CharManJur.Models;

namespace CharManJur.Services;

// What a purchase hands over and gets back. Coins are spent from the pack; a Note, if chosen,
// is spent whole (its entire written value), with any shortfall made up from coins.
public sealed class PaymentPlan
{
    public long Price { get; init; }
    public CharacterItem? Note { get; init; }
    public long NoteValue { get; init; }
    public int Marks { get; init; }
    public int Rupees { get; init; }
    public int Chips { get; init; }

    public long CoinChips => Marks * (long)CurrencyHelper.ChipsPerMark
                           + Rupees * (long)CurrencyHelper.ChipsPerRupee
                           + Chips;
    public long Tendered => NoteValue + CoinChips;
    public long Change => Tendered - Price;

    // Change comes back in the largest denominations possible (never as a Note).
    public int ChangeMarks => (int)(Change / CurrencyHelper.ChipsPerMark);
    public int ChangeRupees => (int)(Change % CurrencyHelper.ChipsPerMark / CurrencyHelper.ChipsPerRupee);
    public int ChangeChips => (int)(Change % CurrencyHelper.ChipsPerRupee);
}

public sealed class ShopResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class ShopTransactions
{
    private readonly ICharAttribDataService _svc;
    private readonly IItemDataService _items;
    private readonly IPlayerActionLogService _log;

    public ShopTransactions(ICharAttribDataService svc, IItemDataService items, IPlayerActionLogService log)
    {
        _svc = svc;
        _items = items;
        _log = log;
    }

    // ===== WHAT THE CHARACTER CAN PAY WITH =====
    private int Count(int foundationId) => CurrencyHelper.Count(_svc.Inventory, foundationId);

    public long CoinChips() =>
        Count(CurrencyHelper.MarkId) * (long)CurrencyHelper.ChipsPerMark
        + Count(CurrencyHelper.RupeeId) * (long)CurrencyHelper.ChipsPerRupee
        + Count(CurrencyHelper.ChipId);

    private IEnumerable<CharacterItem> CurrencyRecords(int foundationId) =>
        _svc.Inventory.Where(i => !i.IsDropped && CurrencyHelper.IsCurrency(i)
                                  && !i.TemplateGuid.HasValue && i.TemplateId == foundationId);

    public List<CharacterItem> Notes() =>
        CurrencyRecords(CurrencyHelper.NoteId).OrderBy(n => CurrencyHelper.ValueEach(n)).ToList();

    // ===== PLANNING =====
    // Null when the price can't be covered. With a Note chosen, the Note is spent whole first and
    // coins cover any shortfall. Without one, coins are chosen to give the least change, then the
    // fewest coins.
    public PaymentPlan? PlanPayment(long price, int? noteItemId)
    {
        CharacterItem? note = null;
        long noteValue = 0;
        if (noteItemId.HasValue)
        {
            note = Notes().FirstOrDefault(n => n.Id == noteItemId.Value);
            if (note == null) return null;
            noteValue = CurrencyHelper.ValueEach(note);
        }

        var coins = PlanCoins(Math.Max(0, price - noteValue),
            Count(CurrencyHelper.MarkId), Count(CurrencyHelper.RupeeId), Count(CurrencyHelper.ChipId));
        if (coins == null) return null;

        return new PaymentPlan
        {
            Price = price,
            Note = note,
            NoteValue = noteValue,
            Marks = coins.Value.Marks,
            Rupees = coins.Value.Rupees,
            Chips = coins.Value.Chips
        };
    }

    private static (int Marks, int Rupees, int Chips)? PlanCoins(long need, int marks, int rupees, int chips)
    {
        if (need <= 0) return (0, 0, 0);
        if (marks * 60L + rupees * 5L + chips < need) return null;

        (int Marks, int Rupees, int Chips)? best = null;
        long bestOver = long.MaxValue;
        int bestCount = int.MaxValue;

        void Try(int m, long r)
        {
            long remaining = Math.Max(0, need - m * 60L);
            long c = Math.Max(0, remaining - r * 5);
            if (c > chips) return;

            long over = m * 60L + r * 5 + c - need;
            int count = m + (int)r + (int)c;
            if (over < bestOver || (over == bestOver && count < bestCount))
            {
                best = (m, (int)r, (int)c);
                bestOver = over;
                bestCount = count;
            }
        }

        long maxMarks = Math.Min(marks, need / 60 + 1);
        for (int m = 0; m <= maxMarks; m++)
        {
            long remaining = Math.Max(0, need - m * 60L);
            long exact = Math.Min(rupees, remaining / 5);            // rupees that fit without overpaying
            long over = Math.Min(rupees, (remaining + 4) / 5);       // one more rupee, if that's the only way
            Try(m, exact);
            if (over != exact) Try(m, over);
        }
        return best;
    }

    // ===== TEXT =====
    public static string Coins(int marks, int rupees, int chips)
    {
        var parts = new List<string>();
        if (marks > 0) parts.Add($"{marks:N0} {(marks == 1 ? "Mark" : "Marks")}");
        if (rupees > 0) parts.Add($"{rupees:N0} {(rupees == 1 ? "Rupee" : "Rupees")}");
        if (chips > 0) parts.Add($"{chips:N0} {(chips == 1 ? "Chip" : "Chips")}");
        return parts.Count == 0 ? "nothing" : string.Join(", ", parts);
    }

    public static string PayText(PaymentPlan p)
    {
        if (p.Note == null) return Coins(p.Marks, p.Rupees, p.Chips);
        string note = $"a Note written for {p.NoteValue / CurrencyHelper.ChipsPerMark:N0} Marks";
        return p.CoinChips > 0 ? $"{note} + {Coins(p.Marks, p.Rupees, p.Chips)}" : note;
    }

    public static string ChangeText(PaymentPlan p) =>
        p.Change > 0 ? Coins(p.ChangeMarks, p.ChangeRupees, p.ChangeChips) : "none";

    // ===== PURCHASE =====
    public async Task<ShopResult> PurchaseAsync(Item template, int quantity, long unitPrice, bool listedPrice, int? noteItemId)
    {
        if (template == null || quantity < 1 || unitPrice < 0) return Fail("Nothing to buy.");
        if (_svc.IsPackDropped) return Fail("Pick up your pack before shopping.");

        long total = unitPrice * quantity;
        var plan = PlanPayment(total, noteItemId);
        if (plan == null) return Fail("You can't cover that price with the money you chose to pay with.");

        // Look up everything needed for change BEFORE touching the pack, so a failure changes nothing.
        var markT = plan.ChangeMarks > 0 ? await _items.GetItemByIdAsync(CurrencyHelper.MarkId) : null;
        var rupeeT = plan.ChangeRupees > 0 ? await _items.GetItemByIdAsync(CurrencyHelper.RupeeId) : null;
        var chipT = plan.ChangeChips > 0 ? await _items.GetItemByIdAsync(CurrencyHelper.ChipId) : null;
        if ((plan.ChangeMarks > 0 && markT == null) ||
            (plan.ChangeRupees > 0 && rupeeT == null) ||
            (plan.ChangeChips > 0 && chipT == null))
            return Fail("The currency items needed to make change aren't loaded.");

        // Pay
        RemoveCoins(CurrencyHelper.MarkId, plan.Marks);
        RemoveCoins(CurrencyHelper.RupeeId, plan.Rupees);
        RemoveCoins(CurrencyHelper.ChipId, plan.Chips);
        if (plan.Note != null) _svc.RemoveItemFromInventory(plan.Note.Id);

        // Change, then the goods
        if (markT != null) _svc.AddItemToInventory(markT, plan.ChangeMarks);
        if (rupeeT != null) _svc.AddItemToInventory(rupeeT, plan.ChangeRupees);
        if (chipT != null) _svc.AddItemToInventory(chipT, plan.ChangeChips);
        _svc.AddItemToInventory(template, quantity);
        _svc.UpdateEncumbranceState();

        string suffix = !listedPrice
            ? " [price set at purchase]"
            : ShopSettings.PurchaseInflation != 0m
                ? $" [prices {(ShopSettings.PurchaseInflation * 100m):+0.##;-0.##}%]"
                : string.Empty;

        await _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ItemPurchased,
            PlayerName = _svc.PlayerName,
            PlayerId = _svc.PlayerId,
            CharacterName = _svc.CharacterName,
            CharacterGuid = _svc.CharacterGuid,
            ItemName = template.Name,
            ItemId = template.Id,
            Summary = $"Purchased {quantity}x {template.Name} for {CurrencyHelper.FormatAmount(total, CurrencyDisplayUnit.Chips)} " +
                      $"(paid: {PayText(plan)}; change: {ChangeText(plan)}){suffix}"
        });

        await _svc.SaveNowAsync();
        return new ShopResult { Success = true };
    }

    private void RemoveCoins(int foundationId, int count)
    {
        if (count <= 0) return;

        // Unequipped, smaller stacks first
        var records = CurrencyRecords(foundationId).OrderBy(i => i.IsEquipped).ThenBy(i => i.Quantity).ToList();
        foreach (var record in records)
        {
            if (count <= 0) break;
            int take = Math.Min(record.Quantity, count);
            if (take == record.Quantity)
            {
                _svc.RemoveItemFromInventory(record.Id);
            }
            else
            {
                record.Quantity -= take;
                record.LastModified = DateTime.Now;
            }
            count -= take;
        }
    }

    private static ShopResult Fail(string message) => new() { Success = false, Message = message };
}