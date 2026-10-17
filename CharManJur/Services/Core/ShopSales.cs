using CharManJur.Models;

namespace CharManJur.Services;

// Selling: the item leaves the pack and its price comes back as coins in the largest denominations.
public class ShopSales
{
    private readonly ICharAttribDataService _svc;
    private readonly IItemDataService _items;
    private readonly IPlayerActionLogService _log;

    public ShopSales(ICharAttribDataService svc, IItemDataService items, IPlayerActionLogService log)
    {
        _svc = svc;
        _items = items;
        _log = log;
    }

    // A Note is currency whose value is written on the item itself.
    public static bool IsNote(CharacterItem item)
        => CurrencyHelper.IsCurrency(item) && item.Template?.IsValueEditable == true;

    // Payout in the largest denominations; never a Note.
    public static (int Marks, int Rupees, int Chips) Payout(long chips)
    {
        if (chips < 0) chips = 0;
        return ((int)(chips / CurrencyHelper.ChipsPerMark),
                (int)(chips % CurrencyHelper.ChipsPerMark / CurrencyHelper.ChipsPerRupee),
                (int)(chips % CurrencyHelper.ChipsPerRupee));
    }

    // ===== LISTED PRICES (items that have a value) =====

    // A partly used item: it has uses and some are gone.
    public static bool IsOpened(CharacterItem item)
        => item.Template is { HasUses: true } t && item.RemainingUses < t.Uses!.Value;

    // One full-use item: the sale modifier applied to its value. A Note pays its exact written value.
    public static long ListedUnit(CharacterItem item)
    {
        int baseChips = CurrencyHelper.ValueEach(item);
        if (baseChips <= 0) return 0;
        return IsNote(item) ? baseChips : CurrencyHelper.ApplyInflation(baseChips, ShopSettings.SaleInflation);
    }

    // The partly used item itself: its full price scaled by remaining uses, rounded down.
    public static long OpenedUnit(CharacterItem item)
    {
        long full = ListedUnit(item);
        if (!IsOpened(item)) return full;
        long maxUses = item.Template!.Uses!.Value;
        return full * Math.Max(0, item.RemainingUses) / maxUses;
    }

    // Total for selling some of a record. Only one item in a stack is ever opened, and it stays
    // with the stack unless the whole stack is sold.
    public static long ListedTotal(CharacterItem item, int quantity)
    {
        quantity = Math.Clamp(quantity, 1, Math.Max(1, item.Quantity));
        long full = ListedUnit(item);
        if (!IsOpened(item)) return full * quantity;
        return quantity >= item.Quantity ? full * (quantity - 1) + OpenedUnit(item) : full * quantity;
    }

    // customUnitPrice is only used for items with no value. It is exactly what the player typed:
    // no sale modifier, no scaling by uses.
    public async Task<ShopResult> SellAsync(int characterItemId, int quantity, long customUnitPrice)
    {
        if (quantity < 1) return Fail("Nothing to sell.");
        if (_svc.IsPackDropped) return Fail("Pick up your pack before shopping.");

        var item = _svc.Inventory.FirstOrDefault(i => i.Id == characterItemId);
        if (item?.Template == null || item.IsDropped) return Fail("That item isn't in your pack any more.");
        if (item.IsEquipped) return Fail("Unequip it before selling.");
        if (item.IsEmpty) return Fail("A used-up item can't be sold.");
        if (quantity > item.Quantity) return Fail("You don't have that many.");

        bool note = IsNote(item);
        if (CurrencyHelper.IsCurrency(item) && !note) return Fail("Coins can't be sold.");

        bool listed = CurrencyHelper.ValueEach(item) > 0;
        bool includesOpened = listed && !note && IsOpened(item) && quantity >= item.Quantity;

        long total;
        if (listed)
        {
            total = ListedTotal(item, quantity);
            if (total < 1) return Fail("The shop won't pay anything for it as it is.");
        }
        else
        {
            if (note) return Fail("This Note has no written value.");
            if (customUnitPrice < 1) return Fail("There's no price to sell it for.");
            total = customUnitPrice * quantity;
        }

        var (marks, rupees, chips) = Payout(total);

        // Look up every coin template BEFORE touching the pack, so a failure changes nothing.
        var markT = marks > 0 ? await _items.GetItemByIdAsync(CurrencyHelper.MarkId) : null;
        var rupeeT = rupees > 0 ? await _items.GetItemByIdAsync(CurrencyHelper.RupeeId) : null;
        var chipT = chips > 0 ? await _items.GetItemByIdAsync(CurrencyHelper.ChipId) : null;
        if ((marks > 0 && markT == null) || (rupees > 0 && rupeeT == null) || (chips > 0 && chipT == null))
            return Fail("The currency items needed to pay you aren't loaded.");

        string name = item.DisplayName;
        int templateId = item.TemplateId;

        // Hand over the goods. Selling part of a stack keeps the rest (and any opened item) on the record.
        if (quantity >= item.Quantity)
        {
            _svc.RemoveItemFromInventory(item.Id);
        }
        else
        {
            item.Quantity -= quantity;
            item.LastModified = DateTime.Now;
        }

        // Get paid
        if (markT != null) _svc.AddItemToInventory(markT, marks);
        if (rupeeT != null) _svc.AddItemToInventory(rupeeT, rupees);
        if (chipT != null) _svc.AddItemToInventory(chipT, chips);
        _svc.UpdateEncumbranceState();

        var notes = new List<string>();
        if (note) notes.Add("Note: exact written value");
        else if (!listed) notes.Add("price set at sale");
        else
        {
            if (ShopSettings.SaleInflation != 0m)
                notes.Add($"sale prices {(ShopSettings.SaleInflation * 100m):+0.##;-0.##}%");
            if (includesOpened) notes.Add("partly used item priced by remaining uses");
        }
        string suffix = notes.Count > 0 ? $" [{string.Join("; ", notes)}]" : string.Empty;

        await _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ItemSold,
            PlayerName = _svc.PlayerName,
            PlayerId = _svc.PlayerId,
            CharacterName = _svc.CharacterName,
            CharacterGuid = _svc.CharacterGuid,
            ItemName = name,
            ItemId = templateId,
            Summary = $"Sold {quantity}x {name} for {CurrencyHelper.FormatAmount(total, CurrencyDisplayUnit.Chips)} " +
                      $"(received: {ShopTransactions.Coins(marks, rupees, chips)}){suffix}"
        });

        await _svc.SaveNowAsync();
        return new ShopResult { Success = true };
    }

    private static ShopResult Fail(string message) => new() { Success = false, Message = message };
}