using CharManJur.Models;
using CharManJur.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;

namespace CharManJur.ViewModels;

public class ThemeColorEntry : INotifyPropertyChanged
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    private string _hexValue = string.Empty;
    public string HexValue
    {
        get => _hexValue;
        set
        {
            _hexValue = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewColor));
        }
    }

    public Color PreviewColor => Color.TryParse(HexValue, out var c) ? c : Colors.Transparent;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IThemeService _themeService;
    private readonly IAssetPackService _assetPackService;
    private readonly IItemDataService _itemDataService;
    private readonly IBackgroundDataService _backgroundDataService;
    private readonly IFamiliarDataService _familiarDataService;
    private readonly IRaceDataService _raceDataService;
    private readonly IHinderanceDataService _hinderanceDataService;
    private readonly IBlueprintDataService _blueprintDataService;
    private readonly ISpellDataService _spellDataService;
    private readonly IQuipDataService _quipDataService;
    private readonly ITechniqueDataService _techniqueDataService;
    private readonly ITraitDataService _traitDataService;

    private static readonly Dictionary<string, string> FriendlyNames = new()
    {
        { "ThemeSectionBorderColor", "Section Borders" },
        { "ThemeDetailBorderColor1", "Detail Panel Border (Primary)" },
        { "ThemeDetailBorderColor2", "Detail Panel Border (Secondary)" },
        { "ThemePrimaryButtonColor", "Primary Action Buttons" },
        { "ThemeNeutralButtonColor", "Back / Neutral Buttons" },
        { "ThemeDangerButtonColor", "Cancel / Danger Buttons" },
        { "ThemeWarningButtonColor", "Save For Later Buttons" },
        { "ThemeCreateButtonColor", "Create Custom Buttons" },
        { "ThemeConfirmButtonColor", "Confirm Buttons" },
        { "ThemeSummaryButtonColor", "Summary Buttons" },
        { "ThemePanelBackgroundColor", "Panel Backgrounds" },
        { "ThemePanelHeaderBackgroundColor", "Panel Header Backgrounds" },
        { "ThemePageBackgroundColor", "Page Backgrounds" },
    };

    public ObservableCollection<ThemeColorEntry> ThemeColors { get; } = new();
    public ObservableCollection<string> SavedThemeNames { get; } = new();
    public ObservableCollection<InstalledPackEntry> InstalledPacks { get; private set; } = new();

    // SKINS: the Skins tab's view model (load order, previews). Its edits are saved with Save Changes.
    public SkinSettingsViewModel Skins { get; }

    private string _newThemeName = string.Empty;
    public string NewThemeName
    {
        get => _newThemeName;
        set { _newThemeName = value; OnPropertyChanged(); }
    }

    private bool _hasUnsavedChanges;
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set { _hasUnsavedChanges = value; OnPropertyChanged(); }
    }

    // ===== Currency display preference (applied only on Save Changes) =====
    private static readonly CurrencyDisplayUnit[] CurrencyUnits =
        { CurrencyDisplayUnit.Chips, CurrencyDisplayUnit.Rupees, CurrencyDisplayUnit.Marks };

    public IReadOnlyList<string> CurrencyUnitOptions { get; } = CurrencyUnits
        .Select(u => $"{CurrencyHelper.UnitIcon(u)} {CurrencyHelper.PluralName(u)}")
        .ToList();

    private CurrencyDisplayUnit _pendingCurrencyUnit = CurrencyDisplaySettings.Unit;

    public int PendingCurrencyIndex
    {
        get => Array.IndexOf(CurrencyUnits, _pendingCurrencyUnit);
        set
        {
            if (value < 0 || value >= CurrencyUnits.Length) return;   // the picker briefly reports -1 while loading
            if (CurrencyUnits[value] == _pendingCurrencyUnit) return;
            _pendingCurrencyUnit = CurrencyUnits[value];
            OnPropertyChanged();
            RefreshUnsavedState();
        }
    }

    // ===== Tab selection =====
    private string _selectedTab = "General";
    public string SelectedTab
    {
        get => _selectedTab;
        set
        {
            _selectedTab = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsGeneralTabSelected));
            OnPropertyChanged(nameof(IsThemeTabSelected));
            OnPropertyChanged(nameof(IsSkinsTabSelected));   // SKINS
            OnPropertyChanged(nameof(IsAssetManagerTabSelected));
        }
    }

    public bool IsGeneralTabSelected => SelectedTab == "General";
    public bool IsThemeTabSelected => SelectedTab == "Theme";
    public bool IsSkinsTabSelected => SelectedTab == "Skins";   // SKINS
    public bool IsAssetManagerTabSelected => SelectedTab == "AssetManager";

    // THEME MODE (Dark / Light base theme)
    public bool IsDarkThemeSelected => _themeService.Mode == ThemeMode.Dark;
    public bool IsLightThemeSelected => _themeService.Mode == ThemeMode.Light;
    public ICommand SelectDarkThemeCommand { get; }
    public ICommand SelectLightThemeCommand { get; }

    private void NotifyThemeModeChanged()
    {
        OnPropertyChanged(nameof(IsDarkThemeSelected));
        OnPropertyChanged(nameof(IsLightThemeSelected));
    }

    private void ChangeThemeMode(ThemeMode mode)
    {
        _themeService.PreviewMode(mode);
        foreach (var entry in ThemeColors)
        {
            entry.HexValue = _themeService.GetThemeColor(entry.Key).ToArgbHex();
        }
        NotifyThemeModeChanged();
        RefreshUnsavedState();
    }

    public ICommand SelectGeneralTabCommand { get; }
    public ICommand SelectThemeTabCommand { get; }
    public ICommand SelectSkinsTabCommand { get; }   // SKINS
    public ICommand SelectAssetManagerTabCommand { get; }

    public ICommand EditColorCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand SaveChangesCommand { get; }
    public ICommand SaveCurrentAsThemeCommand { get; }
    public ICommand LoadThemeCommand { get; }
    public ICommand DeleteThemeCommand { get; }

    //ASSET MANAGER COMMANDS
    public ICommand OpenItemManagerCommand { get; }
    public ICommand OpenBackgroundManagerCommand { get; }
    public ICommand OpenFamiliarManagerCommand { get; }
    public ICommand OpenRaceManagerCommand { get; }
    public ICommand OpenHinderanceManagerCommand { get; }
    public ICommand OpenBlueprintManagerCommand { get; }
    public ICommand OpenSpellManagerCommand { get; }
    public ICommand OpenQuipManagerCommand { get; }
    public ICommand OpenTechniqueManagerCommand { get; }
    public ICommand OpenTraitManagerCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand TogglePackLoadedCommand { get; }
    public ICommand DeletePackCommand { get; }
    public ICommand CopyToCommand { get; }
    public ICommand UpdatePackCommand { get; }
    public ICommand ToggleExpandedCommand { get; }

    public SettingsViewModel(
        IThemeService themeService, IAssetPackService assetPackService, IItemDataService itemDataService,
        IBackgroundDataService backgroundDataService, IFamiliarDataService familiarDataService, IRaceDataService raceDataService,
        IHinderanceDataService hinderanceDataService, IBlueprintDataService blueprintDataService, ISpellDataService spellDataService,
        IQuipDataService quipDataService, ITechniqueDataService techniqueDataService,
        ISkinService skinService,   // SKINS
        ITraitDataService traitDataService)
    {
        _themeService = themeService;
        _assetPackService = assetPackService;
        _itemDataService = itemDataService;
        _backgroundDataService = backgroundDataService;
        _familiarDataService = familiarDataService;
        _raceDataService = raceDataService;
        _hinderanceDataService = hinderanceDataService;
        _blueprintDataService = blueprintDataService;
        _spellDataService = spellDataService;
        _quipDataService = quipDataService;
        _techniqueDataService = techniqueDataService;
        _traitDataService = traitDataService;

        // SKINS: must exist before RefreshUnsavedState runs
        Skins = new SkinSettingsViewModel(skinService, RefreshUnsavedState);

        foreach (var key in _themeService.ThemeableKeys)
        {
            ThemeColors.Add(new ThemeColorEntry
            {
                Key = key,
                DisplayName = FriendlyNames.TryGetValue(key, out var name) ? name : key,
                HexValue = _themeService.GetThemeColor(key).ToArgbHex()
            });
        }

        RefreshSavedThemeNames();
        RefreshUnsavedState();

        SelectGeneralTabCommand = new Command(() => SelectedTab = "General");
        SelectThemeTabCommand = new Command(() => SelectedTab = "Theme");
        SelectSkinsTabCommand = new Command(() => SelectedTab = "Skins");   // SKINS
        SelectDarkThemeCommand = new Command(() => ChangeThemeMode(ThemeMode.Dark));
        SelectLightThemeCommand = new Command(() => ChangeThemeMode(ThemeMode.Light));
        SelectAssetManagerTabCommand = new Command(() => SelectedTab = "AssetManager");
        OpenItemManagerCommand = new Command(async () => await Shell.Current.GoToAsync("///ItemAssetManagerPage"));
        OpenBackgroundManagerCommand = new Command(async () => await Shell.Current.GoToAsync("///BackgroundAssetManagerPage"));
        OpenFamiliarManagerCommand = new Command(async () => await Shell.Current.GoToAsync("///FamiliarAssetManagerPage"));
        OpenRaceManagerCommand = new Command(async () => await Shell.Current.GoToAsync("///RaceAssetManagerPage"));
        OpenHinderanceManagerCommand = new Command(async () => await Shell.Current.GoToAsync("///HinderanceAssetManagerPage"));
        OpenBlueprintManagerCommand = new Command(async () => await Shell.Current.GoToAsync("///BlueprintAssetManagerPage"));
        OpenSpellManagerCommand = new Command(async () => await Shell.Current.GoToAsync("///SpellAssetManagerPage"));
        OpenQuipManagerCommand = new Command(async () => await Shell.Current.GoToAsync("///QuipAssetManagerPage"));
        OpenTechniqueManagerCommand = new Command(async () => await Shell.Current.GoToAsync("///TechniqueAssetManagerPage"));
        OpenTraitManagerCommand = new Command(async () => await Shell.Current.GoToAsync("///TraitAssetManagerPage"));
        ExportCommand = new Command(async () => await ExportAsync());
        ImportCommand = new Command(async () => await ImportAsync());
        TogglePackLoadedCommand = new Command<InstalledPackEntry>(async (pack) => await TogglePackAsync(pack));
        DeletePackCommand = new Command<InstalledPackEntry>(async (pack) => await DeletePackAsync(pack));
        CopyToCommand = new Command(async () => await CopyToAsync());
        UpdatePackCommand = new Command<InstalledPackEntry>(async (pack) => await UpdatePackAsync(pack));
        ToggleExpandedCommand = new Command<InstalledPackEntry>(pack =>
        {
            if (pack != null) pack.IsExpanded = !pack.IsExpanded;
        });

        Task.Run(RefreshPacksAsync);

        EditColorCommand = new Command<ThemeColorEntry>(async (entry) =>
        {
            if (entry == null) return;

            var popup = new Views.ColorPickerPopup(entry.DisplayName, entry.PreviewColor, (newColor) =>
            {
                entry.HexValue = newColor.ToArgbHex();
                _themeService.PreviewThemeColor(entry.Key, newColor);
                RefreshUnsavedState();
            });

            await Application.Current!.MainPage!.Navigation.PushModalAsync(popup);
        });

        ResetToDefaultsCommand = new Command(() =>
        {
            _themeService.ResetToDefaults();
            foreach (var entry in ThemeColors)
            {
                entry.HexValue = _themeService.GetThemeColor(entry.Key).ToArgbHex();
            }
            RefreshUnsavedState();
        });

        SaveChangesCommand = new Command(SaveChanges);

        SaveCurrentAsThemeCommand = new Command(async () =>
        {
            if (string.IsNullOrWhiteSpace(NewThemeName))
            {
                await Application.Current!.MainPage!.DisplayAlertAsync(
                    "Name Required", "Enter a name for this theme before saving.", "OK");
                return;
            }

            _themeService.SaveNamedTheme(NewThemeName.Trim());
            NewThemeName = string.Empty;
            RefreshSavedThemeNames();
        });

        LoadThemeCommand = new Command<string>(themeName =>
        {
            if (string.IsNullOrEmpty(themeName)) return;
            _themeService.LoadNamedTheme(themeName);
            NotifyThemeModeChanged();   // a saved theme can switch Dark / Light
            foreach (var entry in ThemeColors)
            {
                entry.HexValue = _themeService.GetThemeColor(entry.Key).ToArgbHex();
            }
            RefreshUnsavedState();
        });

        DeleteThemeCommand = new Command<string>(async (themeName) =>
        {
            if (string.IsNullOrEmpty(themeName)) return;
            bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
                "Delete Theme", $"Delete the saved theme '{themeName}'?", "Yes, Delete", "Cancel");
            if (confirm)
            {
                _themeService.DeleteNamedTheme(themeName);
                RefreshSavedThemeNames();
            }
        });
    }

    public async Task RefreshPacksAsync()
    {
        var packs = await _assetPackService.GetInstalledPacksAsync();
        var allCustomItems = await _itemDataService.GetCustomItemsAsync();
        var allCustomBackgrounds = await _backgroundDataService.GetCustomBackgroundsAsync();
        var allCustomFamiliars = await _familiarDataService.GetCustomFamiliarsAsync();
        var allCustomRaces = await _raceDataService.GetCustomRacesAsync();
        var allCustomHinderances = await _hinderanceDataService.GetCustomHinderancesAsync();
        var allCustomBlueprints = await _blueprintDataService.GetCustomBlueprintsAsync();
        var allCustomSpells = await _spellDataService.GetCustomSpellsAsync();
        var allCustomQuips = await _quipDataService.GetCustomQuipsAsync();
        var allCustomTechniques = await _techniqueDataService.GetCustomTechniquesAsync();
        var allCustomTraits = await _traitDataService.GetCustomTraitsAsync();

        foreach (var pack in packs)
        {
            pack.ItemCount = allCustomItems.Count(i => i.SourcePackId == pack.PackId);
            pack.BackgroundCount = allCustomBackgrounds.Count(b => b.SourcePackId == pack.PackId);
            pack.FamiliarCount = allCustomFamiliars.Count(f => f.SourcePackId == pack.PackId);
            pack.RaceCount = allCustomRaces.Count(r => r.SourcePackId == pack.PackId);
            pack.HinderanceCount = allCustomHinderances.Count(h => h.SourcePackId == pack.PackId);
            pack.BlueprintCount = allCustomBlueprints.Count(bp => bp.SourcePackId == pack.PackId);
            pack.SpellCount = allCustomSpells.Count(s => s.SourcePackId == pack.PackId);
            pack.QuipCount = allCustomQuips.Count(q => q.SourcePackId == pack.PackId);
            pack.TechniqueCount = allCustomTechniques.Count(t => t.SourcePackId == pack.PackId);
            pack.TraitCount = allCustomTraits.Count(tr => tr.SourcePackId == pack.PackId);
        }

        InstalledPacks = new ObservableCollection<InstalledPackEntry>(packs);
        OnPropertyChanged(nameof(InstalledPacks));
    }

    private async Task ExportAsync()
    {
        var loadedLocalItems = (await _itemDataService.GetCustomItemsAsync())
            .Where(i => i.IsLoaded && i.SourcePackId == "Local").ToList();
        var loadedLocalBackgrounds = (await _backgroundDataService.GetCustomBackgroundsAsync())
            .Where(b => b.IsLoaded && b.SourcePackId == "Local").ToList();
        var loadedLocalFamiliars = (await _familiarDataService.GetCustomFamiliarsAsync())
            .Where(f => f.IsLoaded && f.SourcePackId == "Local").ToList();
        var loadedLocalRaces = (await _raceDataService.GetCustomRacesAsync())
            .Where(r => r.IsLoaded && r.SourcePackId == "Local").ToList();
        var loadedLocalHinderances = (await _hinderanceDataService.GetCustomHinderancesAsync())
            .Where(h => h.IsLoaded && h.SourcePackId == "Local").ToList();
        var loadedLocalBlueprints = (await _blueprintDataService.GetCustomBlueprintsAsync())
            .Where(bp => bp.IsLoaded && bp.SourcePackId == "Local").ToList();
        var loadedLocalSpells = (await _spellDataService.GetCustomSpellsAsync())
            .Where(s => s.IsLoaded && s.SourcePackId == "Local").ToList();
        var loadedLocalQuips = (await _quipDataService.GetCustomQuipsAsync())
            .Where(q => q.IsLoaded && q.SourcePackId == "Local").ToList();
        var loadedLocalTechniques = (await _techniqueDataService.GetCustomTechniquesAsync())
            .Where(t => t.IsLoaded && t.SourcePackId == "Local").ToList();
        var loadedLocalTraits = (await _traitDataService.GetCustomTraitsAsync())
            .Where(tr => tr.IsLoaded && tr.SourcePackId == "Local").ToList();

        if (loadedLocalItems.Count == 0 && loadedLocalBackgrounds.Count == 0 && loadedLocalFamiliars.Count == 0 &&
            loadedLocalRaces.Count == 0 && loadedLocalHinderances.Count == 0 && loadedLocalBlueprints.Count == 0 &&
            loadedLocalSpells.Count == 0 && loadedLocalQuips.Count == 0 && loadedLocalTechniques.Count == 0 &&
            loadedLocalTraits.Count == 0)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Nothing to Save", "No loaded Local assets to save into a pack.", "OK");
            return;
        }

        string? packName = await Application.Current!.MainPage!.DisplayPromptAsync(
            "Save as Asset Pack", "Name this Asset Pack:", "Continue", "Cancel", initialValue: "My Pack");

        if (string.IsNullOrWhiteSpace(packName)) return;

        int totalCount = loadedLocalItems.Count + loadedLocalBackgrounds.Count + loadedLocalFamiliars.Count +
            loadedLocalRaces.Count + loadedLocalHinderances.Count + loadedLocalBlueprints.Count +
            loadedLocalSpells.Count + loadedLocalQuips.Count + loadedLocalTechniques.Count + loadedLocalTraits.Count;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Confirm Save",
            $"{totalCount} loaded Local asset(s) will be moved into '{packName}' and unloaded. Load '{packName}' from the pack list when you want them active again.",
            "Save", "Cancel");

        if (!confirm) return;

        var itemGuids = loadedLocalItems.Select(i => i.Guid).ToList();
        var backgroundGuids = loadedLocalBackgrounds.Select(b => b.Guid).ToList();
        var familiarGuids = loadedLocalFamiliars.Select(f => f.Guid).ToList();
        var raceGuids = loadedLocalRaces.Select(r => r.Guid).ToList();
        var hinderanceGuids = loadedLocalHinderances.Select(h => h.Guid).ToList();
        var blueprintGuids = loadedLocalBlueprints.Select(bp => bp.Guid).ToList();
        var spellGuids = loadedLocalSpells.Select(s => s.Guid).ToList();
        var quipGuids = loadedLocalQuips.Select(q => q.Guid).ToList();
        var techniqueGuids = loadedLocalTechniques.Select(t => t.Guid).ToList();
        var traitGuids = loadedLocalTraits.Select(tr => tr.Guid).ToList();

        string filePath = await _assetPackService.ExportPackAsync(
            packName, null, itemGuids, backgroundGuids, familiarGuids, raceGuids, hinderanceGuids,
            blueprintGuids, spellGuids, quipGuids, techniqueGuids, moveItems: true, traitGuidsToInclude: traitGuids);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = $"Share '{packName}' Asset Pack",
            File = new ShareFile(filePath)
        });

        await RefreshPacksAsync();
    }

    private async Task ImportAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select an Asset Pack file" });
            if (result == null) return;

            var (success, message) = await _assetPackService.ImportPackAsync(result.FullPath);
            await Application.Current!.MainPage!.DisplayAlertAsync(success ? "Import Complete" : "Import Failed", message, "OK");

            if (success)
            {
                await RefreshPacksAsync();
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Error", $"Failed to import: {ex.Message}", "OK");
        }
    }

    private async Task TogglePackAsync(InstalledPackEntry? pack)
    {
        if (pack == null) return;
        await _assetPackService.SetPackLoadedAsync(pack.PackId, !pack.IsLoaded);
        await RefreshPacksAsync();
    }

    private async Task DeletePackAsync(InstalledPackEntry? pack)
    {
        if (pack == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Pack", $"Permanently delete '{pack.Name}' and all its items? This cannot be undone.",
            "Yes, Delete", "Cancel");

        if (!confirm) return;

        var (success, message) = await _assetPackService.DeletePackAsync(pack.PackId);
        if (!success)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Cannot Delete", message, "OK");
            return;
        }

        await RefreshPacksAsync();
    }

    private async Task CopyToAsync()
    {
        var loadedItems = (await _itemDataService.GetCustomItemsAsync()).Where(i => i.IsLoaded).ToList();
        var loadedBackgrounds = (await _backgroundDataService.GetCustomBackgroundsAsync()).Where(b => b.IsLoaded).ToList();
        var loadedFamiliars = (await _familiarDataService.GetCustomFamiliarsAsync()).Where(f => f.IsLoaded).ToList();
        var loadedRaces = (await _raceDataService.GetCustomRacesAsync()).Where(r => r.IsLoaded).ToList();
        var loadedHinderances = (await _hinderanceDataService.GetCustomHinderancesAsync()).Where(h => h.IsLoaded).ToList();
        var loadedBlueprints = (await _blueprintDataService.GetCustomBlueprintsAsync()).Where(bp => bp.IsLoaded).ToList();
        var loadedSpells = (await _spellDataService.GetCustomSpellsAsync()).Where(s => s.IsLoaded).ToList();
        var loadedQuips = (await _quipDataService.GetCustomQuipsAsync()).Where(q => q.IsLoaded).ToList();
        var loadedTechniques = (await _techniqueDataService.GetCustomTechniquesAsync()).Where(t => t.IsLoaded).ToList();
        var loadedTraits = (await _traitDataService.GetCustomTraitsAsync()).Where(tr => tr.IsLoaded).ToList();

        if (loadedItems.Count == 0 && loadedBackgrounds.Count == 0 && loadedFamiliars.Count == 0 && loadedRaces.Count == 0 &&
            loadedHinderances.Count == 0 && loadedBlueprints.Count == 0 && loadedSpells.Count == 0 && loadedQuips.Count == 0 &&
            loadedTechniques.Count == 0 && loadedTraits.Count == 0)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Nothing to Copy", "No loaded assets to copy.", "OK");
            return;
        }

        var targetChoices = InstalledPacks.Where(p => p.PackId != "Local").ToList();
        if (targetChoices.Count == 0)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("No Packs Available", "Save a pack first before you can copy items into one.", "OK");
            return;
        }

        string[] packNames = targetChoices.Select(p => p.Name).ToArray();
        string? chosen = await Application.Current!.MainPage!.DisplayActionSheetAsync(
            "Copy loaded assets to which pack?", "Cancel", null, packNames);

        if (string.IsNullOrEmpty(chosen) || chosen == "Cancel") return;

        var targetPack = targetChoices.First(p => p.Name == chosen);
        var itemGuids = loadedItems.Select(i => i.Guid).ToList();
        var backgroundGuids = loadedBackgrounds.Select(b => b.Guid).ToList();
        var familiarGuids = loadedFamiliars.Select(f => f.Guid).ToList();
        var raceGuids = loadedRaces.Select(r => r.Guid).ToList();
        var hinderanceGuids = loadedHinderances.Select(h => h.Guid).ToList();
        var blueprintGuids = loadedBlueprints.Select(bp => bp.Guid).ToList();
        var spellGuids = loadedSpells.Select(s => s.Guid).ToList();
        var quipGuids = loadedQuips.Select(q => q.Guid).ToList();
        var techniqueGuids = loadedTechniques.Select(t => t.Guid).ToList();
        var traitGuids = loadedTraits.Select(tr => tr.Guid).ToList();

        var (success, message) = await _assetPackService.CopyItemsToPackAsync(
            targetPack.PackId, itemGuids, backgroundGuids, familiarGuids, raceGuids, hinderanceGuids,
            blueprintGuids, spellGuids, quipGuids, techniqueGuids, traitGuidsToCopy: traitGuids);
        await Application.Current!.MainPage!.DisplayAlertAsync(success ? "Copied" : "Error", message, "OK");

        if (success)
        {
            await RefreshPacksAsync();
        }
    }

    private async Task UpdatePackAsync(InstalledPackEntry? pack)
    {
        if (pack == null) return;

        var popup = new Views.PackEditPopup(pack.Name, pack.Description, pack.Details, async (name, description, details, syncMode) =>
        {
            var (success, message) = await _assetPackService.UpdatePackAsync(pack.PackId, name, description, details, syncMode);
            await Application.Current!.MainPage!.DisplayAlertAsync(success ? "Pack Updated" : "Error", message, "OK");

            if (success)
            {
                await RefreshPacksAsync();
            }
        });

        await Application.Current!.MainPage!.Navigation.PushModalAsync(popup);
    }

    public void SaveChanges()
    {
        _themeService.SaveChanges();
        CurrencyDisplaySettings.Unit = _pendingCurrencyUnit;   // fires Changed, so the dashboard refreshes
        Skins.Save();   // SKINS
        RefreshUnsavedState();
    }

    public void DiscardChanges()
    {
        _themeService.DiscardChanges();
        NotifyThemeModeChanged();
        foreach (var entry in ThemeColors)
        {
            entry.HexValue = _themeService.GetThemeColor(entry.Key).ToArgbHex();
        }
        _pendingCurrencyUnit = CurrencyDisplaySettings.Unit;
        OnPropertyChanged(nameof(PendingCurrencyIndex));   // snaps the picker back
        Skins.Discard();   // SKINS
        RefreshUnsavedState();
    }

    private void RefreshUnsavedState()
    {
        HasUnsavedChanges = _themeService.HasUnsavedChanges
            || _pendingCurrencyUnit != CurrencyDisplaySettings.Unit
            || Skins.HasPendingChanges;   // SKINS
    }

    private void RefreshSavedThemeNames()
    {
        SavedThemeNames.Clear();
        foreach (var name in _themeService.GetSavedThemeNames())
        {
            SavedThemeNames.Add(name);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}