using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class SpellAssetManagerViewModel : INotifyPropertyChanged
{
    private readonly ISpellDataService _spellDataService;
    private readonly IAssetPackService _assetPackService;

    private ObservableCollection<Spell> _customSpells = new();
    public ObservableCollection<InstalledPackEntry> AvailablePacks { get; private set; } = new();

    private List<Spell> _allCustomSpells = new();

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

    public ObservableCollection<Spell> CustomSpells
    {
        get => _customSpells;
        private set { _customSpells = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateNewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleLoadedCommand { get; }

    public event EventHandler? RequestOpenCreator;
    public event EventHandler<Spell>? RequestOpenEditor;

    public SpellAssetManagerViewModel(ISpellDataService spellDataService, IAssetPackService assetPackService)
    {
        _spellDataService = spellDataService;
        _assetPackService = assetPackService;

        RefreshCommand = new Command(async () => await RefreshAsync());
        CreateNewCommand = new Command(() => RequestOpenCreator?.Invoke(this, EventArgs.Empty));
        EditCommand = new Command<Spell>(spell => RequestOpenEditor?.Invoke(this, spell));
        DeleteCommand = new Command<Spell>(async (spell) => await DeleteAsync(spell));
        ToggleLoadedCommand = new Command<Spell>(async (spell) => await ToggleLoadedAsync(spell));

        Task.Run(RefreshAsync);
        Task.Run(RefreshPacksFilterAsync);
    }

    public async Task RefreshAsync()
    {
        var spells = await _spellDataService.GetCustomSpellsAsync();
        _allCustomSpells = spells.Where(s => s.IsLoaded).ToList();
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
            ? _allCustomSpells
            : _allCustomSpells.Where(s => s.SourcePackId == SelectedPackFilter.PackId).ToList();

        CustomSpells = new ObservableCollection<Spell>(filtered.OrderBy(s => s.SpellName));
    }

    private async Task ToggleLoadedAsync(Spell? spell)
    {
        if (spell == null) return;

        bool success = spell.IsLoaded
            ? await _spellDataService.UnloadSpellAsync(spell.Guid)
            : await _spellDataService.LoadSpellAsync(spell.Guid);

        if (success)
        {
            await RefreshAsync();
        }
    }

    private async Task DeleteAsync(Spell? spell)
    {
        if (spell == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Spell",
            $"Permanently delete '{spell.SpellName}'? This cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm) return;

        await _spellDataService.DeleteSpellAsync(spell.Guid);
        await RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}