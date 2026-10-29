using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CharManJur.Views.Godrick_LiveGame;

// One line of the basket: an item with the options chosen for it.
public class BasketLine
{
    public Item Item { get; init; } = null!;
    public int Quantity { get; set; }
    public int? StartingUses { get; init; }
    public int? WrittenValue { get; init; }
    public int? UsageDieSides { get; init; }
    public Familiar? Recipient { get; init; }          // null = the character

    public string Name => Item.Name;
    public string RecipientName => Recipient?.DisplayName ?? string.Empty;
    public bool HasRecipient => Recipient != null;

    public string Details
    {
        get
        {
            var parts = new List<string>();
            if (StartingUses.HasValue && Item.Uses.HasValue && StartingUses.Value < Item.Uses.Value)
                parts.Add($"{StartingUses}/{Item.Uses} uses");
            if (WrittenValue.HasValue) parts.Add($"{WrittenValue.Value / CurrencyHelper.ChipsPerMark} Marks written");
            if (UsageDieSides.HasValue) parts.Add($"{UsageDie.Label(UsageDieSides.Value)} die");
            return string.Join(" · ", parts);
        }
    }

    public bool HasDetails => Details.Length > 0;
}

public partial class BestowItemPage : ContentPage, INotifyPropertyChanged
{
    private readonly IItemDataService _itemDataService;
    private readonly ICharAttribDataService _charDataService;
    private readonly IPlayerActionLogService _actionLogService;
    private readonly Action? _onItemsBestowed;
    private ObservableCollection<Item> _allItems = new();
    private ObservableCollection<Item> _filteredItems = new();
    private Item? _selectedItem;
    private int _selectedQuantity = 1;
    private int _maxQuantity = 999999;
    private readonly List<Familiar?> _recipients = new();   // index 0 = the character itself

    public ObservableCollection<BasketLine> Basket { get; } = new();

    public ObservableCollection<Item> FilteredItems
    {
        get => _filteredItems;
        set
        {
            _filteredItems = value;
            OnPropertyChanged();
            DebugItemCountLabel.Text = $"Items: {_filteredItems.Count}";
        }
    }

    public Item? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedItem));
            OnPropertyChanged(nameof(SelectedItemName));
        }
    }

    public bool HasSelectedItem => SelectedItem != null;
    public string SelectedItemName => SelectedItem?.Name ?? string.Empty;

    public bool HasBasket => Basket.Count > 0;
    public string BasketSummary => Basket.Count == 0
        ? "Nothing added yet."
        : $"{Basket.Sum(b => b.Quantity)} item(s) in {Basket.Count} line(s)";
    public string BestowButtonText => Basket.Count == 0 ? "✅ Bestow" : $"✅ Bestow All ({Basket.Sum(b => b.Quantity)})";

    public BestowItemPage(IItemDataService itemDataService, ICharAttribDataService charDataService,
        IPlayerActionLogService actionLogService, Action? onItemsBestowed = null)
    {
        InitializeComponent();
        _itemDataService = itemDataService;
        _charDataService = charDataService;
        _actionLogService = actionLogService;
        _onItemsBestowed = onItemsBestowed;
        BindingContext = this;

        Basket.CollectionChanged += (_, _) => RefreshBasketBindings();

        BuildRecipients();
        UsageDiePicker.ItemsSource = UsageDie.Sizes.Select(UsageDie.Label).ToList();
        LoadItems();
    }

    private void RefreshBasketBindings()
    {
        OnPropertyChanged(nameof(HasBasket));
        OnPropertyChanged(nameof(BasketSummary));
        OnPropertyChanged(nameof(BestowButtonText));
    }

    private void BuildRecipients()
    {
        var names = new List<string> { $"{_charDataService.CharacterName} (character)" };
        _recipients.Add(null);

        foreach (var f in _charDataService.AcquiredFamiliars.Where(f => !f.IsDead))
        {
            _recipients.Add(f);
            names.Add($"{(f.FmlrIntelligence == FmlrIntelligences.Inanimate ? "⚙️" : "🐾")} {f.DisplayName}");
        }

        RecipientPicker.ItemsSource = names;
        RecipientPicker.SelectedIndex = 0;
        RecipientRow.IsVisible = _recipients.Count > 1;   // hidden when there's nobody else to give to
    }

    private async void LoadItems()
    {
        try
        {
            var items = await _itemDataService.GetAllItemsAsync();
            _allItems = new ObservableCollection<Item>(items);
            FilteredItems = new ObservableCollection<Item>(items);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
            await DisplayAlertAsync("Error", $"Failed to load items: {ex.Message}", "OK");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) => FilterItems(e.NewTextValue);

    private void OnSearchClicked(object sender, EventArgs e) => FilterItems(SearchEntry.Text);

    private void FilterItems(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            FilteredItems = new ObservableCollection<Item>(_allItems);
            return;
        }

        var filtered = _allItems.Where(i =>
            i.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            (i.BaseDescription != null && i.BaseDescription.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
            (i.Category != null && i.Category.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        FilteredItems = new ObservableCollection<Item>(filtered);
    }

    private void OnItemSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Item selectedItem)
        {
            SelectedItem = selectedItem;
            _selectedQuantity = 1;
            QuantityEntry.Text = "1";
            NoteValueRow.IsVisible = selectedItem.IsValueEditable;
            if (selectedItem.IsValueEditable) NoteValueEntry.Text = CurrencyHelper.MinNoteMarks.ToString();

            // Stackable items bestow as one stack. A non-stackable item can be bestowed several times: each one is its own record.
            _maxQuantity = 999999;
            ConfigureStartingUses(selectedItem);
            ConfigureUsageDie(selectedItem);
        }
    }

    private void OnQuantityIncrement(object sender, EventArgs e)
    {
        if (_selectedQuantity < _maxQuantity)
        {
            _selectedQuantity++;
            QuantityEntry.Text = _selectedQuantity.ToString();
            UpdateStartingUsesVisibility();
        }
    }

    private void OnQuantityDecrement(object sender, EventArgs e)
    {
        if (_selectedQuantity > 1)
        {
            _selectedQuantity--;
            QuantityEntry.Text = _selectedQuantity.ToString();
            UpdateStartingUsesVisibility();
        }
    }

    private void OnQuantityTextChanged(object sender, TextChangedEventArgs e)
    {
        if (int.TryParse(e.NewTextValue, out int value))
        {
            if (value < 1) value = 1;
            if (value > _maxQuantity) value = _maxQuantity;
            _selectedQuantity = value;
            UpdateStartingUsesVisibility();
        }
        else if (!string.IsNullOrEmpty(e.NewTextValue))
        {
            QuantityEntry.Text = _selectedQuantity.ToString();   // revert invalid input
        }
    }

    private async void OnCreateCustomItemClicked(object sender, EventArgs e)
    {
        var creatorViewModel = new CustomItemCreatorViewModel(_itemDataService, (item) =>
        {
            _allItems.Add(item);
            FilterItems(SearchEntry.Text);
        });
        var creatorPage = new CustomItemCreatorPage(creatorViewModel);
        await Navigation.PushModalAsync(creatorPage);
    }

    // Opens the creator pre-filled from the selected item, to make a NEW custom item from it.
    private async void OnCustomCopyClicked(object sender, EventArgs e)
    {
        if (SelectedItem == null)
        {
            await DisplayAlertAsync("Custom Copy", "Select the item you want to base your custom item on.", "OK");
            return;
        }

        var creatorViewModel = new CustomItemCreatorViewModel(
            _itemDataService,
            onItemCreated: item =>
            {
                _allItems.Add(item);
                FilterItems(SearchEntry.Text);
                SelectedItem = item;        // ready to set a quantity and add to the basket
            },
            copyFrom: SelectedItem);

        await Navigation.PushModalAsync(new CustomItemCreatorPage(creatorViewModel));
    }

    // ===== BASKET =====
    private async void OnAddToBasketClicked(object sender, EventArgs e)
    {
        if (SelectedItem == null) return;

        // A Note carries a written value, entered in Marks (default 10)
        int? writtenValue = null;
        if (SelectedItem.IsValueEditable)
        {
            if (!CurrencyHelper.TryParseNoteMarks(NoteValueEntry.Text, out int chips))
            {
                await DisplayAlertAsync("Invalid Value",
                    $"Enter a whole number of Marks from {CurrencyHelper.MinNoteMarks} to {CurrencyHelper.MaxNoteMarks:N0}.", "OK");
                return;
            }
            writtenValue = chips;
        }

        int index = RecipientPicker.SelectedIndex;
        var recipient = index > 0 && index < _recipients.Count ? _recipients[index] : null;

        int? startingUses = StartingUsesControl.IsVisible ? StartingUsesControl.Uses : null;
        int? usageDie = UsageDieRow.IsVisible && UsageDiePicker.SelectedIndex >= 0
            ? UsageDie.Sizes[UsageDiePicker.SelectedIndex]
            : null;

        Basket.Add(new BasketLine
        {
            Item = SelectedItem,
            Quantity = _selectedQuantity,
            StartingUses = startingUses,
            WrittenValue = writtenValue,
            UsageDieSides = usageDie,
            Recipient = recipient
        });

        // Ready for the next item
        ItemsCollectionView.SelectedItem = null;
        SelectedItem = null;
        _selectedQuantity = 1;
        QuantityEntry.Text = "1";
        NoteValueRow.IsVisible = false;
        StartingUsesControl.IsVisible = false;
        UsageDieRow.IsVisible = false;
    }

    private void OnRemoveBasketLine(object sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is BasketLine line) Basket.Remove(line);
    }

    // The standard bestow entry doesn't name the die, so die items get one extra line saying which die was chosen.
    private void LogUsageDieBestowed(BasketLine line)
    {
        if (!line.UsageDieSides.HasValue) return;

        string to = line.Recipient != null ? $" to {line.RecipientName}" : string.Empty;
        _ = _actionLogService.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ItemBestowed,
            PlayerName = _charDataService.PlayerName,
            PlayerId = _charDataService.PlayerId,
            CharacterName = _charDataService.CharacterName,
            CharacterGuid = _charDataService.CharacterGuid,
            ItemName = line.Name,
            ItemId = line.Item.Id,
            Summary = $"Bestowed {line.Quantity}x {line.Name}{to} with a {UsageDie.Label(line.UsageDieSides.Value)} usage die"
        });
    }

    private void OnClearBasketClicked(object sender, EventArgs e) => Basket.Clear();

    // Bestows the basket. A selected item that hasn't been added yet is added first, so a single item still takes one press.
    private async void OnBestowClicked(object sender, EventArgs e)
    {
        if (Basket.Count == 0 && SelectedItem != null)
        {
            OnAddToBasketClicked(sender, e);
            if (Basket.Count == 0) return;   // a Note's written value was invalid
        }
        if (Basket.Count == 0) return;

        var lines = Basket.Select(b =>
        {
            string who = b.Recipient != null ? $" → {b.Recipient.DisplayName}" : string.Empty;
            string extra = b.HasDetails ? $" ({b.Details})" : string.Empty;
            return $"{b.Quantity}× {b.Name}{extra}{who}";
        }).ToList();

        bool confirm = await DisplayAlertAsync(
            "Confirm Bestow",
            $"Bestow to {_charDataService.CharacterName}:\n\n{string.Join("\n", lines)}",
            "Bestow", "Cancel");
        if (!confirm) return;

        var failed = new List<string>();

        foreach (var line in Basket.ToList())
        {
            if (line.Recipient != null)
            {
                if (!_charDataService.AddItemToFamiliarInventory(
                        line.Recipient.InstanceId, line.Item, line.Quantity, line.WrittenValue, line.StartingUses, line.UsageDieSides))
                    failed.Add($"{line.Name} (to {line.RecipientName})");
                else
                    LogUsageDieBestowed(line);
                continue;
            }

            CharacterItem? last = null;
            if (line.Item.IsStackableItem)
            {
                last = _charDataService.AddItemToInventory(line.Item, line.Quantity, null, line.StartingUses, line.UsageDieSides);
            }
            else
            {
                // Each non-stackable item is its own record.
                for (int i = 0; i < line.Quantity; i++)
                    last = _charDataService.AddItemToInventory(line.Item, 1, line.WrittenValue, line.StartingUses, line.UsageDieSides);
            }

            if (last != null)
            {
                _ = _actionLogService.LogItemBestowedAsync(
                    last,
                    _charDataService.PlayerName,
                    _charDataService.PlayerId,
                    _charDataService.CharacterName,
                    _charDataService.CharacterGuid);
                LogUsageDieBestowed(line);
            }
        }

        _onItemsBestowed?.Invoke();

        if (failed.Count > 0)
        {
            await DisplayAlertAsync("Some items weren't bestowed",
                "These couldn't be given right now:\n\n" + string.Join("\n", failed), "OK");
        }

        Basket.Clear();
        await Navigation.PopModalAsync();   // the dashboard announces what arrived
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        if (Basket.Count > 0)
        {
            bool leave = await DisplayAlertAsync("Leave without bestowing?",
                "Your basket will be emptied.", "Leave", "Stay");
            if (!leave) return;
        }
        await Navigation.PopModalAsync();
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // A new item starts full; the player can lower it (an empty flask, a half-used torch).
    private void ConfigureStartingUses(Item item)
    {
        StartingUsesControl.MaximumUses = item.HasUses ? item.Uses!.Value : 1;
        StartingUsesControl.Uses = StartingUsesControl.MaximumUses;
        UpdateStartingUsesVisibility();
    }

    // Items with a usage die can be bestowed at any size; the picker starts at the item's own die.
    private void ConfigureUsageDie(Item item)
    {
        UsageDieRow.IsVisible = item.HasUsageDie;
        if (item.HasUsageDie)
            UsageDiePicker.SelectedIndex = Math.Max(0, Array.IndexOf(UsageDie.Sizes, item.DefaultUsageDieSides));
    }


    // Offered for items with uses. A stack of more than one always starts full.
    private void UpdateStartingUsesVisibility()
    {
        StartingUsesControl.IsVisible =
            SelectedItem is { HasUses: true } item && (!item.IsStackableItem || _selectedQuantity == 1);
    }
}