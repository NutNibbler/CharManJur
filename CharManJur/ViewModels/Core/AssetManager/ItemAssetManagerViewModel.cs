using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class ItemAssetManagerViewModel : INotifyPropertyChanged
{
    private readonly IItemDataService _itemDataService;
    private readonly IAssetPackService _assetPackService;

    private ObservableCollection<Item> _customItems = new();
    public ObservableCollection<InstalledPackEntry> AvailablePacks { get; private set; } = new();

    private List<Item> _allCustomItems = new();

    private InstalledPackEntry? _selectedPackFilter;

    public InstalledPackEntry? SelectedPackFilter
    {
        get => _selectedPackFilter;
        set
        {
            _selectedPackFilter = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public ObservableCollection<Item> CustomItems
    {
        get => _customItems;
        private set { _customItems = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateNewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleLoadedCommand { get; }

    public event EventHandler? RequestOpenCreator;
    public event EventHandler<Item>? RequestOpenEditor;

    public ItemAssetManagerViewModel(IItemDataService itemDataService, IAssetPackService assetPackService)
    {
        _itemDataService = itemDataService;
        _assetPackService = assetPackService;

        RefreshCommand = new Command(async () => await RefreshAsync());
        CreateNewCommand = new Command(() => RequestOpenCreator?.Invoke(this, EventArgs.Empty));
        EditCommand = new Command<Item>(item => RequestOpenEditor?.Invoke(this, item));
        DeleteCommand = new Command<Item>(async (item) => await DeleteAsync(item));
        ToggleLoadedCommand = new Command<Item>(async (item) => await ToggleLoadedAsync(item));

        Task.Run(RefreshAsync);
        Task.Run(RefreshPacksFilterAsync);
    }

    public async Task RefreshAsync()
    {
        var items = await _itemDataService.GetCustomItemsAsync();
        _allCustomItems = items.Where(i => i.IsLoaded).ToList();
        ApplyFilter();
    }

    public async Task RefreshPacksFilterAsync()
    {
        var packs = await _assetPackService.GetInstalledPacksAsync();
        var withAll = new List<InstalledPackEntry> { new InstalledPackEntry { PackId = string.Empty, Name = "All Packs" } };
        withAll.AddRange(packs);

        AvailablePacks = new ObservableCollection<InstalledPackEntry>(withAll);
        OnPropertyChanged(nameof(AvailablePacks));

        if (SelectedPackFilter == null)
        {
            SelectedPackFilter = withAll[0];
        }
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrEmpty(SelectedPackFilter?.PackId)
            ? _allCustomItems
            : _allCustomItems.Where(i => i.SourcePackId == SelectedPackFilter.PackId).ToList();

        CustomItems = new ObservableCollection<Item>(filtered.OrderBy(i => i.Name));
    }

    private async Task ToggleLoadedAsync(Item? item)
    {
        if (item == null) return;

        bool success = item.IsLoaded
            ? await _itemDataService.UnloadItemAsync(item.Guid)
            : await _itemDataService.LoadItemAsync(item.Guid);

        if (success)
        {
            await RefreshAsync();
        }
    }

    private async Task DeleteAsync(Item? item)
    {
        if (item == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Item",
            $"Permanently delete '{item.Name}'? This cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        await _itemDataService.DeleteItemAsync(item.Guid);
        await RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}