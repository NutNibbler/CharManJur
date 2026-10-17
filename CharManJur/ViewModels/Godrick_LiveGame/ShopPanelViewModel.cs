using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class ShopItemRow
{
    public Item Item { get; init; } = null!;
    public string Name => Item.Name;
    public string CategoryText { get; init; } = string.Empty;
    public bool HasListedPrice { get; init; }
    public long UnitPriceChips { get; init; }      // after the purchase inflation modifier
    public string PriceText { get; init; } = string.Empty;
}

public class PayOption
{
    public string Label { get; init; } = string.Empty;
    public int? NoteItemId { get; init; }          // null = pay with coins
}

public class ShopPanelViewModel : INotifyPropertyChanged
{
    private const int MaxQuantity = 999;
    private const long MaxCustomPriceChips = 100_000_000;
    private const string AllCategories = "All categories";

    // Never sold here: money itself, and Story items (given at creation or bestowed by the GM).
    private static readonly HashSet<ItemCategory> HiddenCategories = new()
    {
        ItemCategory.Currency,
        ItemCategory.Story
    };

    private readonly ICharAttribDataService _svc;
    private readonly IItemDataService _itemData;
    private readonly ShopTransactions _shop;

    private List<Item> _allItems = new();
    private ShopItemRow? _selected;
    private PayOption? _payOption;
    private PaymentPlan? _plan;
    private string _search = string.Empty;
    private string _category = AllCategories;
    private string _quantityText = "1";
    private string _customPriceText = string.Empty;
    private string _resultText = string.Empty;
    private bool _rebuilding;
    private bool _busy;

    public ObservableCollection<ShopItemRow> Rows { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<PayOption> PayOptions { get; } = new();

    public ICommand BuyCommand { get; }
    public ICommand QuantityUpCommand { get; }
    public ICommand QuantityDownCommand { get; }

    // ===== BUY / SELL MODE =====
    private bool _isSellMode;
    public SellPanelViewModel Sell { get; }
    public ICommand ShowBuyCommand { get; }
    public ICommand ShowSellCommand { get; }
    public bool IsSellMode => _isSellMode;
    public bool IsBuyMode => !_isSellMode;

    // Shown beside the toggle and in the panel title, so nobody mistakes one side for the other.
    public string ModeBannerText => _isSellMode ? "💰 SELLING YOUR ITEMS" : "🛒 BUYING FROM THE SHOP";
    public string ModeTitle => _isSellMode ? "🏪 Shop · Selling" : "🏪 Shop · Buying";
    public event Action? ModeChanged;

    // The home page reloads its dashboard (currency, pack slots) when this fires.
    public event Action? CharacterInventoryChanged;

    public ShopPanelViewModel(ICharAttribDataService svc, IItemDataService itemData, IPlayerActionLogService log)
    {
        _svc = svc;
        _itemData = itemData;
        _shop = new ShopTransactions(svc, itemData, log);

        BuyCommand = new Command(async () => await BuyAsync());
        QuantityUpCommand = new Command(() => SetQuantity(Quantity + 1));
        QuantityDownCommand = new Command(() => SetQuantity(Quantity - 1));

        Sell = new SellPanelViewModel(svc, itemData, log);
        Sell.CharacterInventoryChanged += () => CharacterInventoryChanged?.Invoke();
        ShowBuyCommand = new Command(() => SetMode(false));
        ShowSellCommand = new Command(() => SetMode(true));

        // A preference or GM change to prices, units or categories refreshes the list right away.
        CurrencyDisplaySettings.Changed += OnSettingsChanged;
        ShopSettings.Changed += OnSettingsChanged;
    }

    private void OnSettingsChanged()
    {
        RebuildCategories();
        RebuildRows();
    }

    private void SetMode(bool sell)
    {
        if (_isSellMode == sell) return;
        _isSellMode = sell;

        if (sell)
        {
            Sell.Refresh();
        }
        else
        {
            // Sales change your coins and Notes, so refresh what the Buy side offers to pay with.
            RebuildPayOptions();
            RecomputePreview();
        }
        OnPropertyChanged(string.Empty);
        ModeChanged?.Invoke();
    }

    // Called each time the panel opens.
    public async Task RefreshAsync()
    {
        var all = await _itemData.GetAllItemsAsync();
        _allItems = all.Where(i => !(i.Category is { } c && HiddenCategories.Contains(c)))
                       .OrderBy(i => i.Category.HasValue ? (int)i.Category.Value : int.MaxValue)
                       .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                       .ToList();

        _quantityText = "1";
        _customPriceText = string.Empty;
        _resultText = string.Empty;
        _isSellMode = false;   // the panel always opens on Buy
        RebuildPayOptions();
        RebuildCategories();
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

    private void RebuildCategories()
    {
        var names = _allItems.Where(IsAllowed).Select(CategoryName).Distinct().OrderBy(n => n).ToList();

        _rebuilding = true;
        try
        {
            Categories.Clear();
            Categories.Add(AllCategories);
            foreach (var name in names) Categories.Add(name);
        }
        finally { _rebuilding = false; }

        if (!Categories.Contains(_category)) _category = AllCategories;
    }

    private void RebuildRows()
    {
        var keep = _selected?.Item;

        _rebuilding = true;
        try
        {
            Rows.Clear();
            foreach (var item in _allItems)
            {
                if (!IsAllowed(item)) continue;

                string category = CategoryName(item);
                if (_category != AllCategories && category != _category) continue;
                if (_search.Length > 0 && item.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;

                bool listed = item.ValueInChips is > 0;
                long unit = listed ? CurrencyHelper.ApplyInflation(item.ValueInChips!.Value, ShopSettings.PurchaseInflation) : 0;

                Rows.Add(new ShopItemRow
                {
                    Item = item,
                    CategoryText = category,
                    HasListedPrice = listed,
                    UnitPriceChips = unit,
                    PriceText = listed ? CurrencyHelper.FormatAmount(unit, CurrencyDisplaySettings.Unit) : "No set price"
                });
            }
            _selected = keep == null ? null : Rows.FirstOrDefault(r => ReferenceEquals(r.Item, keep));
        }
        finally { _rebuilding = false; }

        RecomputePreview();
        OnPropertyChanged(string.Empty);
    }

    private void RebuildPayOptions()
    {
        int? keep = _payOption?.NoteItemId;

        _rebuilding = true;
        try
        {
            PayOptions.Clear();
            PayOptions.Add(new PayOption { Label = "Coins" });
            foreach (var note in _shop.Notes())
            {
                PayOptions.Add(new PayOption
                {
                    Label = $"Note ({CurrencyHelper.FormatAmount(CurrencyHelper.ValueEach(note), CurrencyDisplayUnit.Marks)})",
                    NoteItemId = note.Id
                });
            }
            _payOption = PayOptions.FirstOrDefault(o => o.NoteItemId == keep) ?? PayOptions[0];
        }
        finally { _rebuilding = false; }
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

    public string SelectedCategory
    {
        get => _category;
        set
        {
            if (_rebuilding || value == null || value == _category) return;
            _category = value;
            RebuildRows();
        }
    }

    public ShopItemRow? SelectedRow
    {
        get => _selected;
        set
        {
            if (_rebuilding || ReferenceEquals(_selected, value)) return;
            _selected = value;
            _quantityText = "1";
            _customPriceText = string.Empty;
            _resultText = string.Empty;
            RecomputePreview();
            OnPropertyChanged(string.Empty);
        }
    }

    public PayOption? SelectedPayOption
    {
        get => _payOption;
        set
        {
            if (_rebuilding || value == null || ReferenceEquals(_payOption, value)) return;
            _payOption = value;
            RecomputePreview();
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
            RecomputePreview();
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
            RecomputePreview();
            OnPropertyChanged(string.Empty);
        }
    }

    private int Quantity => int.TryParse(_quantityText, out int q) ? Math.Clamp(q, 1, MaxQuantity) : 1;

    private void SetQuantity(int quantity)
    {
        _quantityText = Math.Clamp(quantity, 1, MaxQuantity).ToString();
        RecomputePreview();
        OnPropertyChanged(string.Empty);
    }

    // The price each, in Chips, that a purchase would use right now. Null when there isn't a usable one.
    private long? EffectiveUnitPrice
    {
        get
        {
            if (_selected == null) return null;
            if (_selected.HasListedPrice) return _selected.UnitPriceChips;
            return long.TryParse(_customPriceText.Trim(), out long chips) && chips >= 1 && chips <= MaxCustomPriceChips
                ? chips
                : null;
        }
    }

    private void RecomputePreview()
    {
        long? unit = EffectiveUnitPrice;
        _plan = unit.HasValue
            ? _shop.PlanPayment(unit.Value * Quantity, _payOption?.NoteItemId)
            : null;
    }

    // ===== BUY =====
    private async Task BuyAsync()
    {
        if (_busy || _selected == null) return;
        var page = Application.Current!.MainPage!;

        if (_svc.IsPackDropped)
        {
            await page.DisplayAlertAsync("Pack Dropped", "Pick up your pack before shopping.", "OK");
            return;
        }

        long? price = EffectiveUnitPrice;
        if (price == null)
        {
            await page.DisplayAlertAsync("Set a Price", "Enter a whole number of Chips (1 or more).", "OK");
            return;
        }

        _busy = true;
        try
        {
            var row = _selected;
            int quantity = Quantity;
            bool listed = row.HasListedPrice;
            long unit = price.Value;

            long total = unit * quantity;
            var plan = _shop.PlanPayment(total, _payOption?.NoteItemId);
            if (plan == null)
            {
                await page.DisplayAlertAsync("Can't Afford It",
                    "That price isn't covered by what you chose to pay with.", "OK");
                return;
            }

            string message =
                $"Buy {quantity}x {row.Name} for {CurrencyHelper.FormatAmount(total, CurrencyDisplaySettings.Unit)}?\n\n" +
                $"You pay: {ShopTransactions.PayText(plan)}\nChange back: {ShopTransactions.ChangeText(plan)}";
            if (WouldEncumber(quantity))
                message += "\n\n⚠ This will put you over your pack capacity. You'll be encumbered and your Max HP drops to 0.";

            if (!await page.DisplayAlertAsync("Confirm Purchase", message, "Buy", "Cancel")) return;

            var result = await _shop.PurchaseAsync(row.Item, quantity, unit, listed, _payOption?.NoteItemId);
            if (!result.Success)
            {
                await page.DisplayAlertAsync("Purchase Failed", result.Message, "OK");
                return;
            }

            _quantityText = "1";
            _customPriceText = string.Empty;
            _resultText = $"Bought {quantity}x {row.Name} for {CurrencyHelper.FormatAmount(total, CurrencyDisplaySettings.Unit)}.";
            RebuildPayOptions();
            RecomputePreview();
            OnPropertyChanged(string.Empty);
            CharacterInventoryChanged?.Invoke();
        }
        finally { _busy = false; }
    }

    // Pack slots a quantity of this item takes, split into records the way the pack really stores them.
    private static int PackSlotsFor(Item t, int quantity)
        => InventorySlotCalculator.SplitQuantity(t, quantity).Sum(c => InventorySlotCalculator.SlotsFor(t, c));

    // Estimate of whether the new item pushes the pack past capacity (change coins are ignored).
    private bool WouldEncumber(int quantity)
    {
        if (_selected == null) return false;
        var t = _selected.Item;

        // Only stackable items merge into what the character already carries.
        int have = t.IsStackableItem
            ? _svc.Inventory
                .Where(i => !i.IsDropped && !i.IsEquipped && !i.IsEmpty &&
                            (t.IsPlayerCreated ? i.TemplateGuid == t.Guid : i.TemplateId == t.Id))
                .Sum(i => i.Quantity)
            : 0;

        int added = PackSlotsFor(t, have + quantity) - PackSlotsFor(t, have);
        return _svc.InventorySlotsUsed + added > _svc.InventorySlotsAvailable;
    }

    // ===== DISPLAY =====
    public bool HasNoRows => Rows.Count == 0;
    public bool HasSelection => _selected != null;
    public bool HasNoSelection => _selected == null;

    public string SelectedName => _selected?.Name ?? string.Empty;
    public string SelectedDescription => _selected?.Item.BaseDescription ?? string.Empty;
    public bool HasSelectedDescription => !string.IsNullOrWhiteSpace(SelectedDescription);

    public string SelectedTraits
    {
        get
        {
            if (_selected == null) return string.Empty;
            var t = _selected.Item;
            var parts = new List<string> { _selected.CategoryText, EnumDisplayHelper.GetName(t.Size) };
            if (t.IsStackableItem) parts.Add("Stackable");
            if (t.HasUses) parts.Add($"{t.Uses} uses");
            return string.Join(" · ", parts);
        }
    }

    public string CoinsText => $"Coins: {CurrencyHelper.FormatAmount(_shop.CoinChips(), CurrencyDisplaySettings.Unit)}";

    public bool HasNoListedPrice => _selected is { HasListedPrice: false };
    public string UnitPriceText => _selected?.PriceText ?? string.Empty;

    // "= 1 Mark, 2 Rupees, 3 Chips each": the typed Chips in largest denominations (never a Note).
    public string PriceConversionText
    {
        get
        {
            if (_selected == null || _selected.HasListedPrice) return string.Empty;
            long? price = EffectiveUnitPrice;
            if (price == null) return string.Empty;

            long chips = price.Value;
            int marks = (int)(chips / CurrencyHelper.ChipsPerMark);
            int rupees = (int)(chips % CurrencyHelper.ChipsPerMark / CurrencyHelper.ChipsPerRupee);
            int rest = (int)(chips % CurrencyHelper.ChipsPerRupee);
            return $"= {ShopTransactions.Coins(marks, rupees, rest)} each";
        }
    }
    public bool HasPriceConversion => PriceConversionText.Length > 0;

    public string TotalText
    {
        get
        {
            long? unit = EffectiveUnitPrice;
            return unit.HasValue
                ? CurrencyHelper.FormatAmount(unit.Value * Quantity, CurrencyDisplaySettings.Unit)
                : "Enter a price";
        }
    }

    public bool HasNotes => PayOptions.Count > 1;

    public bool HasPlan => _plan != null;
    public string PlanText => _plan == null ? string.Empty
        : $"You pay: {ShopTransactions.PayText(_plan)}\nChange back: {ShopTransactions.ChangeText(_plan)}";

    public string StatusText
    {
        get
        {
            if (_selected == null) return string.Empty;
            if (_svc.IsPackDropped) return "Pick up your pack before shopping.";
            if (EffectiveUnitPrice == null) return "Enter a price in Chips to buy this.";
            if (_plan == null)
                return _payOption?.NoteItemId != null ? "That Note and your coins can't cover this."
                     : HasNotes ? "Not enough coins. Choose a Note to pay with."
                     : "Not enough coins.";
            return string.Empty;
        }
    }
    public bool HasStatus => StatusText.Length > 0;

    public string WarningText => _selected != null && WouldEncumber(Quantity)
        ? "⚠ This would put you over your pack capacity (encumbered, Max HP 0)."
        : string.Empty;
    public bool HasWarning => WarningText.Length > 0;

    public string ResultText => _resultText;
    public bool HasResult => _resultText.Length > 0;

    public bool CanBuy => _selected != null && !_svc.IsPackDropped && _plan != null;

    // ===== INotifyPropertyChanged =====
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}