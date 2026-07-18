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
    private int _maxQuantity = 99;

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

    public BestowItemPage(IItemDataService itemDataService, ICharAttribDataService charDataService, Action? onItemsBestowed = null)
    {
        InitializeComponent();
        _itemDataService = itemDataService;
        _charDataService = charDataService;
        _onItemsBestowed = onItemsBestowed;
        BindingContext = this;

        // Load items immediately
        LoadItems();
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

            if (selectedItem.IsStackableItem)
            {
                _maxQuantity = 99;
            }
            else
            {
                _maxQuantity = 1;
            }
        }
    }

    private void OnQuantityIncrement(object sender, EventArgs e)
    {
        if (_selectedQuantity < _maxQuantity)
        {
            _selectedQuantity++;
            QuantityEntry.Text = _selectedQuantity.ToString();
        }
    }

    private void OnQuantityDecrement(object sender, EventArgs e)
    {
        if (_selectedQuantity > 1)
        {
            _selectedQuantity--;
            QuantityEntry.Text = _selectedQuantity.ToString();
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

        bool confirm = await DisplayAlertAsync(
            "Confirm Bestow",
            $"Bestow {_selectedQuantity}x '{SelectedItem.Name}' to character?",
            "Yes",
            "Cancel");

        if (!confirm) return;

        if (SelectedItem.IsStackableItem)
        {
            _charDataService.AddItemToInventory(SelectedItem, _selectedQuantity);
        }
        else
        {
            for (int i = 0; i < _selectedQuantity; i++)
            {
                _charDataService.AddItemToInventory(SelectedItem, 1);
            }
        }

        _onItemsBestowed?.Invoke();

        await DisplayAlertAsync("Success!",
            $"Granted {_selectedQuantity}x {SelectedItem.Name} to character.",
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
}