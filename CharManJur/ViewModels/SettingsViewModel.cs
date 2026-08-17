using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Services;

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
    private string _selectedTab = "Theme";
    public string SelectedTab
    {
        get => _selectedTab;
        set
        {
            _selectedTab = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsGeneralTabSelected));
            OnPropertyChanged(nameof(IsThemeTabSelected));
        }
    }

    public bool IsGeneralTabSelected => SelectedTab == "General";
    public bool IsThemeTabSelected => SelectedTab == "Theme";

    public ICommand SelectGeneralTabCommand { get; }
    public ICommand SelectThemeTabCommand { get; }

    public ICommand EditColorCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand SaveChangesCommand { get; }
    public ICommand SaveCurrentAsThemeCommand { get; }
    public ICommand LoadThemeCommand { get; }
    public ICommand DeleteThemeCommand { get; }

    public SettingsViewModel(IThemeService themeService)
    {
        _themeService = themeService;

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