using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class QuipAssetManagerViewModel : INotifyPropertyChanged
{
    private readonly IQuipDataService _quipDataService;
    private readonly IAssetPackService _assetPackService;

    private ObservableCollection<Quip> _customQuips = new();
    public ObservableCollection<InstalledPackEntry> AvailablePacks { get; private set; } = new();

    private List<Quip> _allCustomQuips = new();

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

    public ObservableCollection<Quip> CustomQuips
    {
        get => _customQuips;
        private set { _customQuips = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateNewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleLoadedCommand { get; }

    public event EventHandler? RequestOpenCreator;
    public event EventHandler<Quip>? RequestOpenEditor;

    public QuipAssetManagerViewModel(IQuipDataService quipDataService, IAssetPackService assetPackService)
    {
        _quipDataService = quipDataService;
        _assetPackService = assetPackService;

        RefreshCommand = new Command(async () => await RefreshAsync());
        CreateNewCommand = new Command(() => RequestOpenCreator?.Invoke(this, EventArgs.Empty));
        EditCommand = new Command<Quip>(quip => RequestOpenEditor?.Invoke(this, quip));
        DeleteCommand = new Command<Quip>(async (quip) => await DeleteAsync(quip));
        ToggleLoadedCommand = new Command<Quip>(async (quip) => await ToggleLoadedAsync(quip));

        Task.Run(RefreshAsync);
        Task.Run(RefreshPacksFilterAsync);
    }

    public async Task RefreshAsync()
    {
        var quips = await _quipDataService.GetCustomQuipsAsync();
        _allCustomQuips = quips.Where(q => q.IsLoaded).ToList();
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
            ? _allCustomQuips
            : _allCustomQuips.Where(q => q.SourcePackId == SelectedPackFilter.PackId).ToList();

        CustomQuips = new ObservableCollection<Quip>(filtered.OrderBy(q => q.QuipName));
    }

    private async Task ToggleLoadedAsync(Quip? quip)
    {
        if (quip == null) return;

        bool success = quip.IsLoaded
            ? await _quipDataService.UnloadQuipAsync(quip.Guid)
            : await _quipDataService.LoadQuipAsync(quip.Guid);

        if (success)
        {
            await RefreshAsync();
        }
    }

    private async Task DeleteAsync(Quip? quip)
    {
        if (quip == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Quip",
            $"Permanently delete '{quip.QuipName}'? This cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        await _quipDataService.DeleteQuipAsync(quip.Guid);
        await RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}