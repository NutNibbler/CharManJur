using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class TechniqueAssetManagerViewModel : INotifyPropertyChanged
{
    private readonly ITechniqueDataService _techniqueDataService;
    private readonly IAssetPackService _assetPackService;

    private ObservableCollection<Technique> _customTechniques = new();
    public ObservableCollection<InstalledPackEntry> AvailablePacks { get; private set; } = new();

    private List<Technique> _allCustomTechniques = new();

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

    public ObservableCollection<Technique> CustomTechniques
    {
        get => _customTechniques;
        private set { _customTechniques = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateNewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleLoadedCommand { get; }

    public event EventHandler? RequestOpenCreator;
    public event EventHandler<Technique>? RequestOpenEditor;

    public TechniqueAssetManagerViewModel(ITechniqueDataService techniqueDataService, IAssetPackService assetPackService)
    {
        _techniqueDataService = techniqueDataService;
        _assetPackService = assetPackService;

        RefreshCommand = new Command(async () => await RefreshAsync());
        CreateNewCommand = new Command(() => RequestOpenCreator?.Invoke(this, EventArgs.Empty));
        EditCommand = new Command<Technique>(technique => RequestOpenEditor?.Invoke(this, technique));
        DeleteCommand = new Command<Technique>(async (technique) => await DeleteAsync(technique));
        ToggleLoadedCommand = new Command<Technique>(async (technique) => await ToggleLoadedAsync(technique));

        Task.Run(RefreshAsync);
        Task.Run(RefreshPacksFilterAsync);
    }

    public async Task RefreshAsync()
    {
        var techniques = await _techniqueDataService.GetCustomTechniquesAsync();
        _allCustomTechniques = techniques.Where(t => t.IsLoaded).ToList();
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
            ? _allCustomTechniques
            : _allCustomTechniques.Where(t => t.SourcePackId == SelectedPackFilter.PackId).ToList();

        CustomTechniques = new ObservableCollection<Technique>(filtered.OrderBy(t => t.TechniqueName));
    }

    private async Task ToggleLoadedAsync(Technique? technique)
    {
        if (technique == null) return;

        bool success = technique.IsLoaded
            ? await _techniqueDataService.UnloadTechniqueAsync(technique.Guid)
            : await _techniqueDataService.LoadTechniqueAsync(technique.Guid);

        if (success)
        {
            await RefreshAsync();
        }
    }

    private async Task DeleteAsync(Technique? technique)
    {
        if (technique == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Technique",
            $"Permanently delete '{technique.TechniqueName}'? This cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        await _techniqueDataService.DeleteTechniqueAsync(technique.Guid);
        await RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}