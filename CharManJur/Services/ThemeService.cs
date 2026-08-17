namespace CharManJur.Services;

public interface IThemeService
{
    void RegisterDefaults(ResourceDictionary resources);
    void ApplyTheme();

    // Live-updates the resource so the UI reflects it immediately, but does
    // NOT persist to Preferences. Marks HasUnsavedChanges = true.
    void PreviewThemeColor(string resourceKey, Color color);

    // Persists every currently-live themeable color to Preferences.
    void SaveChanges();

    // Reverts every themeable color back to whatever's last saved in
    // Preferences (or the default, if never saved).
    void DiscardChanges();

    Color GetThemeColor(string resourceKey);
    void ResetToDefaults();
    IReadOnlyList<string> ThemeableKeys { get; }
    bool HasUnsavedChanges { get; }

    Color? ClipboardColor { get; set; }

    void SaveNamedTheme(string themeName);
    void LoadNamedTheme(string themeName);
    void DeleteNamedTheme(string themeName);
    IReadOnlyList<string> GetSavedThemeNames();
}

public class ThemeService : IThemeService
{
    private const string PreferenceKeyPrefix = "Theme_";
    private const string SavedThemesPreferenceKey = "SavedThemes_v1";

    // Single source of truth for the default palette.
    private static readonly Dictionary<string, string> DefaultThemeColors = new()
    {
        { "ThemeSectionBorderColor", "#008080" },
        { "ThemeDetailBorderColor1", "#008080" },
        { "ThemeDetailBorderColor2", "#008080" },
        { "ThemePrimaryButtonColor", "#43A143" },
        { "ThemeNeutralButtonColor", "#808080" },
        { "ThemeDangerButtonColor",  "#FF4A4A" },
        { "ThemeWarningButtonColor", "#DB600A" },
        { "ThemeCreateButtonColor",  "#00CDFF" },
        { "ThemeConfirmButtonColor", "#00FF00" },
        { "ThemeSummaryButtonColor", "#8080FF" },
        { "ThemePanelBackgroundColor", "#16213e" },
        { "ThemePanelHeaderBackgroundColor", "#2a2a3e" },
    };

    public IReadOnlyList<string> ThemeableKeys => DefaultThemeColors.Keys.ToList();
    public bool HasUnsavedChanges { get; private set; }
    public Color? ClipboardColor { get; set; }

    public void RegisterDefaults(ResourceDictionary resources)
    {
        foreach (var kvp in DefaultThemeColors)
        {
            if (Color.TryParse(kvp.Value, out var color))
            {
                resources[kvp.Key] = color;
            }
        }
    }

    public void ApplyTheme()
    {
        foreach (var key in DefaultThemeColors.Keys)
        {
            string saved = Preferences.Default.Get(PreferenceKeyPrefix + key, string.Empty);
            if (!string.IsNullOrEmpty(saved) && Color.TryParse(saved, out var color))
            {
                Application.Current!.Resources[key] = color;
            }
        }
    }

    public void PreviewThemeColor(string resourceKey, Color color)
    {
        if (!DefaultThemeColors.ContainsKey(resourceKey))
        {
            System.Diagnostics.Debug.WriteLine($"=== ThemeService: '{resourceKey}' is not a recognized themeable key ===");
            return;
        }

        Application.Current!.Resources[resourceKey] = color;
        HasUnsavedChanges = true;
    }

    public void SaveChanges()
    {
        foreach (var key in DefaultThemeColors.Keys)
        {
            var color = GetThemeColor(key);
            Preferences.Default.Set(PreferenceKeyPrefix + key, color.ToArgbHex());
        }

        HasUnsavedChanges = false;
    }

    public void DiscardChanges()
    {
        RegisterDefaults(Application.Current!.Resources);
        ApplyTheme();
        HasUnsavedChanges = false;
    }

    public Color GetThemeColor(string resourceKey)
    {
        if (Application.Current!.Resources.TryGetValue(resourceKey, out var value) && value is Color color)
        {
            return color;
        }

        return Colors.Gray;
    }

    public void ResetToDefaults()
    {
        foreach (var kvp in DefaultThemeColors)
        {
            if (Color.TryParse(kvp.Value, out var defaultColor))
            {
                PreviewThemeColor(kvp.Key, defaultColor);
            }
        }
    }

    // ===== Named theme presets =====

    private Dictionary<string, Dictionary<string, string>> LoadSavedThemesRaw()
    {
        string json = Preferences.Default.Get(SavedThemesPreferenceKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return new();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)
                   ?? new();
        }
        catch
        {
            return new();
        }
    }

    private void SaveSavedThemesRaw(Dictionary<string, Dictionary<string, string>> themes)
    {
        string json = System.Text.Json.JsonSerializer.Serialize(themes);
        Preferences.Default.Set(SavedThemesPreferenceKey, json);
    }

    public void SaveNamedTheme(string themeName)
    {
        var themes = LoadSavedThemesRaw();
        var snapshot = new Dictionary<string, string>();

        foreach (var key in DefaultThemeColors.Keys)
        {
            snapshot[key] = GetThemeColor(key).ToArgbHex();
        }

        themes[themeName] = snapshot;
        SaveSavedThemesRaw(themes);
    }

    public void LoadNamedTheme(string themeName)
    {
        var themes = LoadSavedThemesRaw();
        if (!themes.TryGetValue(themeName, out var snapshot)) return;

        foreach (var kvp in snapshot)
        {
            if (Color.TryParse(kvp.Value, out var color))
            {
                PreviewThemeColor(kvp.Key, color);
            }
        }
    }

    public void DeleteNamedTheme(string themeName)
    {
        var themes = LoadSavedThemesRaw();
        if (themes.Remove(themeName))
        {
            SaveSavedThemesRaw(themes);
        }
    }

    public IReadOnlyList<string> GetSavedThemeNames()
    {
        return LoadSavedThemesRaw().Keys.OrderBy(k => k).ToList();
    }
}