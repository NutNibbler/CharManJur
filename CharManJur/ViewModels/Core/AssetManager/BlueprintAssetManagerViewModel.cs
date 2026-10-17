using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class BlueprintAssetManagerViewModel : INotifyPropertyChanged
{
    private readonly IBlueprintDataService _blueprintDataService;
    private readonly IAssetPackService _assetPackService;

    private ObservableCollection<Blueprint> _customBlueprints = new();
    public ObservableCollection<InstalledPackEntry> AvailablePacks { get; private set; } = new();

    private List<Blueprint> _allCustomBlueprints = new();

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

    public ObservableCollection<Blueprint> CustomBlueprints
    {
        get => _customBlueprints;
        private set { _customBlueprints = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateNewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleLoadedCommand { get; }

    public event EventHandler? RequestOpenCreator;
    public event EventHandler<Blueprint>? RequestOpenEditor;

    public BlueprintAssetManagerViewModel(IBlueprintDataService blueprintDataService, IAssetPackService assetPackService)
    {
        _blueprintDataService = blueprintDataService;
        _assetPackService = assetPackService;

        RefreshCommand = new Command(async () => await RefreshAsync());
        CreateNewCommand = new Command(() => RequestOpenCreator?.Invoke(this, EventArgs.Empty));
        EditCommand = new Command<Blueprint>(blueprint => RequestOpenEditor?.Invoke(this, blueprint));
        DeleteCommand = new Command<Blueprint>(async (blueprint) => await DeleteAsync(blueprint));
        ToggleLoadedCommand = new Command<Blueprint>(async (blueprint) => await ToggleLoadedAsync(blueprint));

        Task.Run(RefreshAsync);
        Task.Run(RefreshPacksFilterAsync);
    }

    public async Task RefreshAsync()
    {
        var blueprints = await _blueprintDataService.GetCustomBlueprintsAsync();
        _allCustomBlueprints = blueprints.Where(bp => bp.IsLoaded).ToList();
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
            ? _allCustomBlueprints
            : _allCustomBlueprints.Where(bp => bp.SourcePackId == SelectedPackFilter.PackId).ToList();

        CustomBlueprints = new ObservableCollection<Blueprint>(filtered.OrderBy(bp => bp.BlueprintName));
    }

    private async Task ToggleLoadedAsync(Blueprint? blueprint)
    {
        if (blueprint == null) return;

        bool success = blueprint.IsLoaded
            ? await _blueprintDataService.UnloadBlueprintAsync(blueprint.Guid)
            : await _blueprintDataService.LoadBlueprintAsync(blueprint.Guid);

        if (success)
        {
            await RefreshAsync();
        }
    }

    private async Task DeleteAsync(Blueprint? blueprint)
    {
        if (blueprint == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Blueprint",
            $"Permanently delete '{blueprint.BlueprintName}'? This cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        await _blueprintDataService.DeleteBlueprintAsync(blueprint.Guid);
        await RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}