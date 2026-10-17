using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class BackgroundAssetManagerViewModel : INotifyPropertyChanged
{
    private readonly IBackgroundDataService _backgroundDataService;
    private readonly IAssetPackService _assetPackService;

    private ObservableCollection<CharacterBackground> _customBackgrounds = new();
    public ObservableCollection<InstalledPackEntry> AvailablePacks { get; private set; } = new();

    private List<CharacterBackground> _allCustomBackgrounds = new();

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

    public ObservableCollection<CharacterBackground> CustomBackgrounds
    {
        get => _customBackgrounds;
        private set { _customBackgrounds = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateNewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleLoadedCommand { get; }

    public event EventHandler? RequestOpenCreator;
    public event EventHandler<CharacterBackground>? RequestOpenEditor;

    public BackgroundAssetManagerViewModel(IBackgroundDataService backgroundDataService, IAssetPackService assetPackService)
    {
        _backgroundDataService = backgroundDataService;
        _assetPackService = assetPackService;

        RefreshCommand = new Command(async () => await RefreshAsync());
        CreateNewCommand = new Command(() => RequestOpenCreator?.Invoke(this, EventArgs.Empty));
        EditCommand = new Command<CharacterBackground>(background => RequestOpenEditor?.Invoke(this, background));
        DeleteCommand = new Command<CharacterBackground>(async (background) => await DeleteAsync(background));
        ToggleLoadedCommand = new Command<CharacterBackground>(async (background) => await ToggleLoadedAsync(background));

        Task.Run(RefreshAsync);
        Task.Run(RefreshPacksFilterAsync);
    }

    public async Task RefreshAsync()
    {
        var backgrounds = await _backgroundDataService.GetCustomBackgroundsAsync();
        _allCustomBackgrounds = backgrounds.Where(b => b.IsLoaded).ToList();
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
            ? _allCustomBackgrounds
            : _allCustomBackgrounds.Where(b => b.SourcePackId == SelectedPackFilter.PackId).ToList();

        CustomBackgrounds = new ObservableCollection<CharacterBackground>(filtered.OrderBy(b => b.Name));
    }

    private async Task ToggleLoadedAsync(CharacterBackground? background)
    {
        if (background == null) return;

        bool success = background.IsLoaded
            ? await _backgroundDataService.UnloadBackgroundAsync(background.Guid)
            : await _backgroundDataService.LoadBackgroundAsync(background.Guid);

        if (success)
        {
            await RefreshAsync();
        }
    }

    private async Task DeleteAsync(CharacterBackground? background)
    {
        if (background == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Background",
            $"Permanently delete '{background.Name}'? This cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        await _backgroundDataService.DeleteBackgroundAsync(background.Guid);
        await RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}