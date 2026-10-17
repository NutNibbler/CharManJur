using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class FamiliarAssetManagerViewModel : INotifyPropertyChanged
{
    private readonly IFamiliarDataService _familiarDataService;
    private readonly IAssetPackService _assetPackService;

    private ObservableCollection<Familiar> _customFamiliars = new();
    public ObservableCollection<InstalledPackEntry> AvailablePacks { get; private set; } = new();

    private List<Familiar> _allCustomFamiliars = new();

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

    public ObservableCollection<Familiar> CustomFamiliars
    {
        get => _customFamiliars;
        private set { _customFamiliars = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateNewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleLoadedCommand { get; }

    public event EventHandler? RequestOpenCreator;
    public event EventHandler<Familiar>? RequestOpenEditor;

    public FamiliarAssetManagerViewModel(IFamiliarDataService familiarDataService, IAssetPackService assetPackService)
    {
        _familiarDataService = familiarDataService;
        _assetPackService = assetPackService;

        RefreshCommand = new Command(async () => await RefreshAsync());
        CreateNewCommand = new Command(() => RequestOpenCreator?.Invoke(this, EventArgs.Empty));
        EditCommand = new Command<Familiar>(familiar => RequestOpenEditor?.Invoke(this, familiar));
        DeleteCommand = new Command<Familiar>(async (familiar) => await DeleteAsync(familiar));
        ToggleLoadedCommand = new Command<Familiar>(async (familiar) => await ToggleLoadedAsync(familiar));

        Task.Run(RefreshAsync);
        Task.Run(RefreshPacksFilterAsync);
    }

    public async Task RefreshAsync()
    {
        var familiars = await _familiarDataService.GetCustomFamiliarsAsync();
        _allCustomFamiliars = familiars.Where(f => f.IsLoaded).ToList();
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
            ? _allCustomFamiliars
            : _allCustomFamiliars.Where(f => f.SourcePackId == SelectedPackFilter.PackId).ToList();

        CustomFamiliars = new ObservableCollection<Familiar>(filtered.OrderBy(f => f.FmlrName));
    }

    private async Task ToggleLoadedAsync(Familiar? familiar)
    {
        if (familiar == null) return;

        bool success = familiar.IsLoaded
            ? await _familiarDataService.UnloadFamiliarAsync(familiar.Guid)
            : await _familiarDataService.LoadFamiliarAsync(familiar.Guid);

        if (success)
        {
            await RefreshAsync();
        }
    }

    private async Task DeleteAsync(Familiar? familiar)
    {
        if (familiar == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Familiar",
            $"Permanently delete '{familiar.FmlrName}'? This cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        await _familiarDataService.DeleteFamiliarAsync(familiar.Guid);
        await RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}