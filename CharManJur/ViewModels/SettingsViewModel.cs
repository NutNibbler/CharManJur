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
    };

    public ObservableCollection<ThemeColorEntry> ThemeColors { get; } = new();
    public ObservableCollection<string> SavedThemeNames { get; } = new();
    public ObservableCollection<InstalledPackEntry> InstalledPacks { get; private set; } = new();


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
            OnPropertyChanged(nameof(IsAssetManagerTabSelected));
        }
    }

    public bool IsGeneralTabSelected => SelectedTab == "General";
    public bool IsThemeTabSelected => SelectedTab == "Theme";
    public bool IsAssetManagerTabSelected => SelectedTab == "AssetManager";

    public ICommand SelectGeneralTabCommand { get; }
    public ICommand SelectThemeTabCommand { get; }
    public ICommand SelectAssetManagerTabCommand { get; }

    public ICommand EditColorCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand SaveChangesCommand { get; }
    public ICommand SaveCurrentAsThemeCommand { get; }
    public ICommand LoadThemeCommand { get; }
    public ICommand DeleteThemeCommand { get; }

    //ASSET MANAGER COMMANDS
    public ICommand OpenItemManagerCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand TogglePackLoadedCommand { get; }
    public ICommand DeletePackCommand { get; }
    public ICommand CopyToCommand { get; }
    public ICommand UpdatePackCommand { get; }
    public ICommand ToggleExpandedCommand { get; }

    public SettingsViewModel(IThemeService themeService, IAssetPackService assetPackService, IItemDataService itemDataService)
    {
        _themeService = themeService;
        _assetPackService = assetPackService;
        _itemDataService = itemDataService;

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
        SelectAssetManagerTabCommand = new Command(() => SelectedTab = "AssetManager");
        OpenItemManagerCommand = new Command(async () => await Shell.Current.GoToAsync("///ItemAssetManagerPage"));
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

        foreach (var pack in packs)
        {
            pack.ItemCount = allCustomItems.Count(i => i.SourcePackId == pack.PackId);
        }

        InstalledPacks = new ObservableCollection<InstalledPackEntry>(packs);
        OnPropertyChanged(nameof(InstalledPacks));
    }

    private async Task ExportAsync()
    {
        var loadedLocalItems = (await _itemDataService.GetCustomItemsAsync())
            .Where(i => i.IsLoaded && i.SourcePackId == "Local")
            .ToList();

        if (loadedLocalItems.Count == 0)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Nothing to Save", "No loaded Local items to save into a pack.", "OK");
            return;
        }

        string? packName = await Application.Current!.MainPage!.DisplayPromptAsync(
            "Save as Asset Pack", "Name this Asset Pack:", "Continue", "Cancel", initialValue: "My Pack");

        if (string.IsNullOrWhiteSpace(packName)) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Confirm Save",
            $"All {loadedLocalItems.Count} loaded Local item(s) will be moved into '{packName}' and unloaded. Load '{packName}' from the pack list when you want them active again.",
            "Save", "Cancel");

        if (!confirm) return;

        var guids = loadedLocalItems.Select(i => i.Guid).ToList();
        string filePath = await _assetPackService.ExportPackAsync(packName, null, guids, moveItems: true);

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
        if (loadedItems.Count == 0)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Nothing to Copy", "No loaded items to copy.", "OK");
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
            "Copy loaded items to which pack?", "Cancel", null, packNames);

        if (string.IsNullOrEmpty(chosen) || chosen == "Cancel") return;

        var targetPack = targetChoices.First(p => p.Name == chosen);
        var guids = loadedItems.Select(i => i.Guid).ToList();

        var (success, message) = await _assetPackService.CopyItemsToPackAsync(targetPack.PackId, guids);
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
        RefreshUnsavedState();
    }

    public void DiscardChanges()
    {
        _themeService.DiscardChanges();
        foreach (var entry in ThemeColors)
        {
            entry.HexValue = _themeService.GetThemeColor(entry.Key).ToArgbHex();
        }
        RefreshUnsavedState();
    }

    private void RefreshUnsavedState()
    {
        HasUnsavedChanges = _themeService.HasUnsavedChanges;
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