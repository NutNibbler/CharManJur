using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class RaceAssetManagerViewModel : INotifyPropertyChanged
{
    private readonly IRaceDataService _raceDataService;
    private readonly IAssetPackService _assetPackService;

    private ObservableCollection<Race> _customRaces = new();
    public ObservableCollection<InstalledPackEntry> AvailablePacks { get; private set; } = new();

    private List<Race> _allCustomRaces = new();

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

    public ObservableCollection<Race> CustomRaces
    {
        get => _customRaces;
        private set { _customRaces = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateNewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleLoadedCommand { get; }

    public event EventHandler? RequestOpenCreator;
    public event EventHandler<Race>? RequestOpenEditor;

    public RaceAssetManagerViewModel(IRaceDataService raceDataService, IAssetPackService assetPackService)
    {
        _raceDataService = raceDataService;
        _assetPackService = assetPackService;

        RefreshCommand = new Command(async () => await RefreshAsync());
        CreateNewCommand = new Command(() => RequestOpenCreator?.Invoke(this, EventArgs.Empty));
        EditCommand = new Command<Race>(race => RequestOpenEditor?.Invoke(this, race));
        DeleteCommand = new Command<Race>(async (race) => await DeleteAsync(race));
        ToggleLoadedCommand = new Command<Race>(async (race) => await ToggleLoadedAsync(race));

        Task.Run(RefreshAsync);
        Task.Run(RefreshPacksFilterAsync);
    }

    public async Task RefreshAsync()
    {
        var races = await _raceDataService.GetCustomRacesAsync();
        _allCustomRaces = races.Where(r => r.IsLoaded).ToList();
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
            ? _allCustomRaces
            : _allCustomRaces.Where(r => r.SourcePackId == SelectedPackFilter.PackId).ToList();

        CustomRaces = new ObservableCollection<Race>(filtered.OrderBy(r => r.Name));
    }

    private async Task ToggleLoadedAsync(Race? race)
    {
        if (race == null) return;

        bool success = race.IsLoaded
            ? await _raceDataService.UnloadRaceAsync(race.Guid)
            : await _raceDataService.LoadRaceAsync(race.Guid);

        if (success)
        {
            await RefreshAsync();
        }
    }

    private async Task DeleteAsync(Race? race)
    {
        if (race == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Kin",
            $"Permanently delete '{race.Name}'? This cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        await _raceDataService.DeleteRaceAsync(race.Guid);
        await RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}