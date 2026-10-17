using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class TraitAssetManagerViewModel : INotifyPropertyChanged
{
    private readonly ITraitDataService _traitDataService;
    private readonly IAssetPackService _assetPackService;

    private ObservableCollection<CustomTrait> _customTraits = new();
    public ObservableCollection<InstalledPackEntry> AvailablePacks { get; private set; } = new();

    private List<CustomTrait> _allCustomTraits = new();
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

    public ObservableCollection<CustomTrait> CustomTraits
    {
        get => _customTraits;
        private set { _customTraits = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateNewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleLoadedCommand { get; }

    public event EventHandler? RequestOpenCreator;
    public event EventHandler<CustomTrait>? RequestOpenEditor;

    public TraitAssetManagerViewModel(ITraitDataService traitDataService, IAssetPackService assetPackService)
    {
        _traitDataService = traitDataService;
        _assetPackService = assetPackService;

        RefreshCommand = new Command(async () => await RefreshAsync());
        CreateNewCommand = new Command(() => RequestOpenCreator?.Invoke(this, EventArgs.Empty));
        EditCommand = new Command<CustomTrait>(trait => RequestOpenEditor?.Invoke(this, trait));
        DeleteCommand = new Command<CustomTrait>(async (trait) => await DeleteAsync(trait));
        ToggleLoadedCommand = new Command<CustomTrait>(async (trait) => await ToggleLoadedAsync(trait));

        Task.Run(RefreshAsync);
        Task.Run(RefreshPacksFilterAsync);
    }

    public async Task RefreshAsync()
    {
        var traits = await _traitDataService.GetCustomTraitsAsync();
        _allCustomTraits = traits.Where(t => t.IsLoaded).ToList();
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
            ? _allCustomTraits
            : _allCustomTraits.Where(t => t.SourcePackId == SelectedPackFilter.PackId).ToList();

        CustomTraits = new ObservableCollection<CustomTrait>(filtered.OrderBy(t => t.PD).ThenBy(t => t.TraitName));
    }

    private async Task ToggleLoadedAsync(CustomTrait? trait)
    {
        if (trait == null) return;

        bool success = trait.IsLoaded
            ? await _traitDataService.UnloadTraitAsync(trait.Guid)
            : await _traitDataService.LoadTraitAsync(trait.Guid);

        if (success)
        {
            await RefreshAsync();
        }
    }

    private async Task DeleteAsync(CustomTrait? trait)
    {
        if (trait == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Trait",
            $"Permanently delete '{trait.TraitName}'? This cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        await _traitDataService.DeleteTraitAsync(trait.Guid);
        await RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}