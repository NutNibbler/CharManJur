using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class BestowItemPage : ContentPage, INotifyPropertyChanged
{
    private readonly IItemDataService _itemDataService;
    private readonly ICharAttribDataService _charDataService;
    private readonly Action? _onItemsBestowed;
    private ObservableCollection<Item> _allItems = new();
    private ObservableCollection<Item> _filteredItems = new();
    private Item? _selectedItem;
    private int _selectedQuantity = 1;
    private int _maxQuantity = 999999;
    private readonly List<Familiar?> _recipients = new();   // index 0 = the character itself

    public ObservableCollection<Item> FilteredItems
    {
        get => _filteredItems;
        set
        {
            _filteredItems = value;
            OnPropertyChanged();
            // Update debug label
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

    private readonly IPlayerActionLogService _actionLogService;
    public BestowItemPage(IItemDataService itemDataService, ICharAttribDataService charDataService,
        IPlayerActionLogService actionLogService, Action? onItemsBestowed = null)
    {
        InitializeComponent();
        _itemDataService = itemDataService;
        _charDataService = charDataService;
        _actionLogService = actionLogService;
        _onItemsBestowed = onItemsBestowed;
        BindingContext = this;

        LoadItems();
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
            System.Diagnostics.Debug.WriteLine("=== Loading items for BestowItemPage ===");
            var items = await _itemDataService.GetAllItemsAsync();
            System.Diagnostics.Debug.WriteLine($"Loaded {items.Count} items from ItemDataService");

            _allItems = new ObservableCollection<Item>(items);
            FilteredItems = new ObservableCollection<Item>(items);

            System.Diagnostics.Debug.WriteLine($"FilteredItems count: {FilteredItems.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            await DisplayAlertAsync("Error", $"Failed to load items: {ex.Message}", "OK");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        FilterItems(e.NewTextValue);
    }

    private void OnSearchClicked(object sender, EventArgs e)
    {
        FilterItems(SearchEntry.Text);
    }

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
        System.Diagnostics.Debug.WriteLine($"Filtered items: {FilteredItems.Count} (search: '{searchTerm}')");
    }

    private void OnItemSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Selection changed. Current: {e.CurrentSelection?.Count ?? 0}");
        if (e.CurrentSelection.FirstOrDefault() is Item selectedItem)
        {
            System.Diagnostics.Debug.WriteLine($"Selected item: {selectedItem.Name} (ID: {selectedItem.Id})");
            SelectedItem = selectedItem;
            _selectedQuantity = 1;
            QuantityEntry.Text = "1";
            NoteValueRow.IsVisible = selectedItem.IsValueEditable;
            if (selectedItem.IsValueEditable) NoteValueEntry.Text = CurrencyHelper.MinNoteMarks.ToString();

            if (selectedItem.IsStackableItem)
            {
                _maxQuantity = 999999;
            }
            else
            {
                _maxQuantity = 1;
            }
            ConfigureStartingUses(selectedItem);
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
        }
        else if (!string.IsNullOrEmpty(e.NewTextValue))
        {
            // Revert invalid input
            QuantityEntry.Text = _selectedQuantity.ToString();
        }
    }

    private async void OnCreateCustomItemClicked(object sender, EventArgs e)
    {
        var creatorViewModel = new CustomItemCreatorViewModel(_itemDataService, (item) =>
        {
            // Add the custom item to the list
            _allItems.Add(item);
            FilterItems(SearchEntry.Text);
        });
        var creatorPage = new CustomItemCreatorPage(creatorViewModel);
        await Navigation.PushModalAsync(creatorPage);
    }

    private async void OnBestowClicked(object sender, EventArgs e)
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
        var familiar = index > 0 && index < _recipients.Count ? _recipients[index] : null;
        string recipientName = familiar != null ? familiar.DisplayName : "character";

        // Starting uses (only offered for items with uses; null = full)
        int? startingUses = StartingUsesControl.IsVisible ? StartingUsesControl.Uses : null;
        string usesNote = startingUses.HasValue && SelectedItem.Uses.HasValue && startingUses.Value < SelectedItem.Uses.Value
            ? $" (starting with {startingUses}/{SelectedItem.Uses} uses)"
            : string.Empty;

        bool confirm = await DisplayAlertAsync(
            "Confirm Bestow",
            $"Bestow {_selectedQuantity}x '{SelectedItem.Name}'{usesNote} to {recipientName}?",
            "Yes",
            "Cancel");

        if (!confirm) return;

        if (familiar != null)
        {
            if (!_charDataService.AddItemToFamiliarInventory(familiar.InstanceId, SelectedItem, _selectedQuantity, writtenValue, startingUses))
            {
                await DisplayAlertAsync("Could Not Bestow", $"{recipientName} can't receive items right now.", "OK");
                return;
            }
        }
        else
        {
            CharacterItem? bestowedItem;

            if (SelectedItem.IsStackableItem)
            {
                bestowedItem = _charDataService.AddItemToInventory(SelectedItem, _selectedQuantity, null, startingUses);
            }
            else
            {
                bestowedItem = null;
                for (int i = 0; i < _selectedQuantity; i++)
                {
                    bestowedItem = _charDataService.AddItemToInventory(SelectedItem, 1, writtenValue, startingUses);
                }
            }

            if (bestowedItem != null)
            {
                _ = _actionLogService.LogItemBestowedAsync(
                    bestowedItem,
                    _charDataService.PlayerName,
                    _charDataService.PlayerId,
                    _charDataService.CharacterName,
                    _charDataService.CharacterGuid);
            }
        }

        _onItemsBestowed?.Invoke();

        await DisplayAlertAsync("Success!",
            $"Granted {_selectedQuantity}x {SelectedItem.Name} to {recipientName}.",
            "OK");

        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
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

    // Offered for items with uses. A stack of more than one always starts full.
    private void UpdateStartingUsesVisibility()
    {
        StartingUsesControl.IsVisible =
            SelectedItem is { HasUses: true } item && (!item.IsStackableItem || _selectedQuantity == 1);
    }
}