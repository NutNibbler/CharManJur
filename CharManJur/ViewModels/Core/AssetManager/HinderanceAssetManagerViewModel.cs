using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class HinderanceAssetManagerViewModel : INotifyPropertyChanged
{
    private readonly IHinderanceDataService _hinderanceDataService;
    private readonly IAssetPackService _assetPackService;

    private ObservableCollection<Hinderance> _customHinderances = new();
    public ObservableCollection<InstalledPackEntry> AvailablePacks { get; private set; } = new();

    private List<Hinderance> _allCustomHinderances = new();

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

    public ObservableCollection<Hinderance> CustomHinderances
    {
        get => _customHinderances;
        private set { _customHinderances = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateNewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleLoadedCommand { get; }

    public event EventHandler? RequestOpenCreator;
    public event EventHandler<Hinderance>? RequestOpenEditor;

    public HinderanceAssetManagerViewModel(IHinderanceDataService hinderanceDataService, IAssetPackService assetPackService)
    {
        _hinderanceDataService = hinderanceDataService;
        _assetPackService = assetPackService;

        RefreshCommand = new Command(async () => await RefreshAsync());
        CreateNewCommand = new Command(() => RequestOpenCreator?.Invoke(this, EventArgs.Empty));
        EditCommand = new Command<Hinderance>(hinderance => RequestOpenEditor?.Invoke(this, hinderance));
        DeleteCommand = new Command<Hinderance>(async (hinderance) => await DeleteAsync(hinderance));
        ToggleLoadedCommand = new Command<Hinderance>(async (hinderance) => await ToggleLoadedAsync(hinderance));

        Task.Run(RefreshAsync);
        Task.Run(RefreshPacksFilterAsync);
    }

    public async Task RefreshAsync()
    {
        var hinderances = await _hinderanceDataService.GetCustomHinderancesAsync();
        _allCustomHinderances = hinderances.Where(h => h.IsLoaded).ToList();
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
            ? _allCustomHinderances
            : _allCustomHinderances.Where(h => h.SourcePackId == SelectedPackFilter.PackId).ToList();

        CustomHinderances = new ObservableCollection<Hinderance>(filtered.OrderBy(h => h.Name));
    }

    private async Task ToggleLoadedAsync(Hinderance? hinderance)
    {
        if (hinderance == null) return;

        bool success = hinderance.IsLoaded
            ? await _hinderanceDataService.UnloadHinderanceAsync(hinderance.Guid)
            : await _hinderanceDataService.LoadHinderanceAsync(hinderance.Guid);

        if (success)
        {
            await RefreshAsync();
        }
    }

    private async Task DeleteAsync(Hinderance? hinderance)
    {
        if (hinderance == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Hinderance",
            $"Permanently delete '{hinderance.Name}'? This cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        await _hinderanceDataService.DeleteHinderanceAsync(hinderance.Guid);
        await RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}