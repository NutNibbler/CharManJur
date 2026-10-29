using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class SellRow
{
    public int CharacterItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryText { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public bool IsNote { get; init; }
    public bool IsOpened { get; init; }            // a partly used item
    public bool HasListedPrice { get; init; }
    public string PriceText { get; init; } = string.Empty;
    public string BlockReason { get; init; } = string.Empty;   // empty = can be sold
    public bool CanSell => BlockReason.Length == 0;
    public bool IsBlocked => BlockReason.Length > 0;
    public bool ShowQuantity => Quantity > 1;
    public string QuantityText => $"×{Quantity}";
}

public class SellPanelViewModel : INotifyPropertyChanged
{
    private const long MaxCustomPriceChips = 100_000_000;

    private readonly ICharAttribDataService _svc;
    private readonly ShopSales _sales;

    private SellRow? _selected;
    private string _search = string.Empty;
    private string _quantityText = "1";
    private string _customPriceText = string.Empty;
    private string _resultText = string.Empty;
    private bool _rebuilding;
    private bool _busy;

    public ObservableCollection<SellRow> Rows { get; } = new();

    public ICommand SellCommand { get; }
    public ICommand QuantityUpCommand { get; }
    public ICommand QuantityDownCommand { get; }

    // The home page reloads its dashboard (currency, pack slots) when this fires.
    public event Action? CharacterInventoryChanged;

    public SellPanelViewModel(ICharAttribDataService svc, IItemDataService itemData, IPlayerActionLogService log)
    {
        _svc = svc;
        _sales = new ShopSales(svc, itemData, log);

        SellCommand = new Command(async () => await SellAsync());
        QuantityUpCommand = new Command(() => SetQuantity(Quantity + 1));
        QuantityDownCommand = new Command(() => SetQuantity(Quantity - 1));

        CurrencyDisplaySettings.Changed += OnSettingsChanged;
        ShopSettings.Changed += OnSettingsChanged;
    }

    private void OnSettingsChanged() => RebuildRows();

    // Called each time the Sell side is shown.
    public void Refresh()
    {
        _selected = null;
        _quantityText = "1";
        _customPriceText = string.Empty;
        _resultText = string.Empty;
        RebuildRows();
    }

    // ===== LIST =====
    private static string CategoryName(Item item) =>
        item.Category.HasValue ? EnumDisplayHelper.GetName(item.Category.Value) : "Other";

    private static bool IsAllowed(Item item)
    {
        var allowed = ShopSettings.AllowedCategories;
        return allowed == null || (item.Category.HasValue && allowed.Contains(item.Category.Value));
    }

    private static string Format(long chips) => CurrencyHelper.FormatAmount(chips, CurrencyDisplaySettings.Unit);

    private SellRow? BuildRow(CharacterItem item)
    {
        var t = item.Template;
        if (t == null || item.IsDropped) return null;

        bool note = ShopSales.IsNote(item);
        if (CurrencyHelper.IsCurrency(item) && !note) return null;   // loose coins aren't sold

        int baseChips = CurrencyHelper.ValueEach(item);
        bool listed = baseChips > 0;
        long unit = listed ? ShopSales.ListedUnit(item) : 0;
        bool opened = listed && !note && ShopSales.IsOpened(item);

        string reason = string.Empty;
        if (item.IsEquipped) reason = "Unequip it before selling.";
        else if (item.IsEmpty) reason = "A used-up item can't be sold.";
        else if (note && !listed) reason = "This Note has no written value.";
        else if (!note && !IsAllowed(t)) reason = "The shop isn't buying this kind of item right now.";
        else if (listed && unit < 1) reason = "The shop won't pay anything for this.";

        string priceText = !listed ? "No set price"
                         : note ? $"{Format(unit)} (exact value)"
                         : opened ? (item.Quantity == 1
                                        ? $"{Format(ShopSales.OpenedUnit(item))} (partly used)"
                                        : $"{Format(unit)} (1 opened)")
                         : Format(unit);

        return new SellRow
        {
            CharacterItemId = item.Id,
            Name = item.HasUsageDie ? $"{item.DisplayName} ({item.UsageDieLabel})" : item.DisplayName,
            CategoryText = CategoryName(t),
            Quantity = item.Quantity,
            IsNote = note,
            IsOpened = opened,
            HasListedPrice = listed,
            PriceText = priceText,
            BlockReason = reason
        };
    }

    private void RebuildRows()
    {
        int? keep = _selected?.CharacterItemId;

        _rebuilding = true;
        try
        {
            Rows.Clear();
            foreach (var item in _svc.Inventory.OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase).ToList())
            {
                var row = BuildRow(item);
                if (row == null) continue;
                if (_search.Length > 0 && row.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                Rows.Add(row);
            }
            _selected = keep == null ? null : Rows.FirstOrDefault(r => r.CharacterItemId == keep.Value);
        }
        finally { _rebuilding = false; }

        OnPropertyChanged(string.Empty);
    }

    // ===== INPUT =====
    public string SearchText
    {
        get => _search;
        set
        {
            string v = value?.Trim() ?? string.Empty;
            if (v == _search) return;
            _search = v;
            RebuildRows();
        }
    }

    public SellRow? SelectedRow
    {
        get => _selected;
        set
        {
            if (_rebuilding || ReferenceEquals(_selected, value)) return;
            _selected = value;
            _quantityText = "1";
            _customPriceText = string.Empty;
            _resultText = string.Empty;
            OnPropertyChanged(string.Empty);
        }
    }

    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (_quantityText == value) return;
            _quantityText = value ?? string.Empty;
            OnPropertyChanged(string.Empty);
        }
    }

    // Price per item in Chips, typed by the player for items that have no set price.
    public string CustomPriceText
    {
        get => _customPriceText;
        set
        {
            string v = value ?? string.Empty;
            if (_customPriceText == v) return;
            _customPriceText = v;
            OnPropertyChanged(string.Empty);
        }
    }

    private int MaxQuantity => _selected?.Quantity ?? 1;
    private int Quantity => int.TryParse(_quantityText, out int q) ? Math.Clamp(q, 1, MaxQuantity) : 1;

    private void SetQuantity(int quantity)
    {
        _quantityText = Math.Clamp(quantity, 1, MaxQuantity).ToString();
        OnPropertyChanged(string.Empty);
    }

    private CharacterItem? SelectedItem => _selected == null
        ? null
        : _svc.Inventory.FirstOrDefault(i => i.Id == _selected.CharacterItemId);

    // What the player typed for an item with no set price (null for priced items or an unusable entry).
    private long? CustomUnitPrice
    {
        get
        {
            if (_selected == null || _selected.HasListedPrice) return null;
            return long.TryParse(_customPriceText.Trim(), out long chips) && chips >= 1 && chips <= MaxCustomPriceChips
                ? chips
                : (long?)null;
        }
    }

    // What a sale would pay right now, in Chips. Null when the item can't be sold or has no usable price.
    private long? EffectiveTotal
    {
        get
        {
            if (_selected == null || !_selected.CanSell) return null;

            if (_selected.HasListedPrice)
            {
                var item = SelectedItem;
                if (item == null) return null;
                long total = ShopSales.ListedTotal(item, Quantity);
                return total >= 1 ? total : (long?)null;
            }

            long? unit = CustomUnitPrice;
            return unit.HasValue ? unit.Value * Quantity : (long?)null;
        }
    }

    // ===== SELL =====
    private async Task SellAsync()
    {
        if (_busy || _selected == null) return;
        var page = Application.Current!.MainPage!;

        if (_svc.IsPackDropped)
        {
            await page.DisplayAlertAsync("Pack Dropped", "Pick up your pack before shopping.", "OK");
            return;
        }

        long? price = EffectiveTotal;
        if (price == null)
        {
            await page.DisplayAlertAsync("Can't Sell", StatusText.Length > 0 ? StatusText : "There's no price to sell it for.", "OK");
            return;
        }

        _busy = true;
        try
        {
            var row = _selected;
            int quantity = Quantity;
            long total = price.Value;
            var (marks, rupees, chips) = ShopSales.Payout(total);

            string message =
                $"Sell {quantity}x {row.Name} for {Format(total)}?\n\n" +
                $"You receive: {ShopTransactions.Coins(marks, rupees, chips)}";
            if (row.IsNote) message += "\n\nA Note sells for its exact written value.";
            if (row.IsOpened && quantity >= row.Quantity)
                message += "\n\nThe partly used item is priced by its remaining uses.";

            if (!await page.DisplayAlertAsync("Confirm Sale", message, "Sell", "Cancel")) return;

            var result = await _sales.SellAsync(row.CharacterItemId, quantity, CustomUnitPrice ?? 0);
            if (!result.Success)
            {
                await page.DisplayAlertAsync("Sale Failed", result.Message, "OK");
                return;
            }

            _quantityText = "1";
            _customPriceText = string.Empty;
            _resultText = $"Sold {quantity}x {row.Name} for {Format(total)}.";
            RebuildRows();
            CharacterInventoryChanged?.Invoke();
        }
        finally { _busy = false; }
    }

    // ===== DISPLAY =====
    public bool HasNoRows => Rows.Count == 0;
    public bool HasSelection => _selected != null;
    public bool HasNoSelection => _selected == null;

    public string SelectedName => _selected?.Name ?? string.Empty;
    public string SelectedTraits => _selected == null ? string.Empty
        : $"{_selected.CategoryText} · {_selected.Quantity} carried";

    public bool HasNoListedPrice => _selected is { CanSell: true, HasListedPrice: false };
    public string UnitPriceText => _selected?.PriceText ?? string.Empty;

    // "= 1 Mark, 2 Rupees, 3 Chips each": the typed Chips in largest denominations (never a Note).
    public string PriceConversionText
    {
        get
        {
            long? unit = CustomUnitPrice;
            if (!unit.HasValue) return string.Empty;
            var (marks, rupees, chips) = ShopSales.Payout(unit.Value);
            return $"= {ShopTransactions.Coins(marks, rupees, chips)} each";
        }
    }
    public bool HasPriceConversion => PriceConversionText.Length > 0;

    public string TotalText
    {
        get
        {
            long? total = EffectiveTotal;
            if (total.HasValue) return Format(total.Value);
            return _selected is { CanSell: true } ? "Enter a price" : "—";
        }
    }

    public bool HasReceive => EffectiveTotal.HasValue;
    public string ReceiveText
    {
        get
        {
            long? total = EffectiveTotal;
            if (!total.HasValue) return string.Empty;
            var (marks, rupees, chips) = ShopSales.Payout(total.Value);
            return $"You receive: {ShopTransactions.Coins(marks, rupees, chips)}";
        }
    }

    public string StatusText
    {
        get
        {
            if (_selected == null) return string.Empty;
            if (!_selected.CanSell) return _selected.BlockReason;
            if (_svc.IsPackDropped) return "Pick up your pack before shopping.";
            if (EffectiveTotal == null)
                return _selected.HasListedPrice ? "The shop won't pay anything for it in this condition."
                                                : "Enter a price in Chips to sell this.";
            return string.Empty;
        }
    }
    public bool HasStatus => StatusText.Length > 0;

    public string ResultText => _resultText;
    public bool HasResult => _resultText.Length > 0;

    public bool CanSellSelected => _selected != null && !_svc.IsPackDropped && EffectiveTotal.HasValue;

    // ===== INotifyPropertyChanged =====
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}