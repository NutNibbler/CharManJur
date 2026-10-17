namespace CharManJur.Services;

public enum ThemeMode
{
    Dark,
    Light
}

public interface IThemeService
{
    void RegisterDefaults(ResourceDictionary resources);
    void ApplyTheme();

    // Live-updates the resource so the UI reflects it immediately, but does
    // NOT persist to Preferences. Marks HasUnsavedChanges = true.
    void PreviewThemeColor(string resourceKey, Color color);

    // Persists the current mode and every live themeable color to Preferences.
    void SaveChanges();

    // Reverts the mode and every themeable color back to whatever's last saved in
    // Preferences (or the default, if never saved).
    void DiscardChanges();

    Color GetThemeColor(string resourceKey);
    void ResetToDefaults();
    IReadOnlyList<string> ThemeableKeys { get; }
    bool HasUnsavedChanges { get; }

    Color? ClipboardColor { get; set; }

    // The base theme currently showing (may be an unsaved preview).
    ThemeMode Mode { get; }

    // Switches the base theme (Dark / Light) live. Not persisted until SaveChanges.
    void PreviewMode(ThemeMode mode);

    void SaveNamedTheme(string themeName);
    void LoadNamedTheme(string themeName);
    void DeleteNamedTheme(string themeName);
    IReadOnlyList<string> GetSavedThemeNames();
}

public class ThemeService : IThemeService
{
    // Colors saved before Dark/Light modes existed. They are treated as Dark-mode edits.
    private const string LegacyPreferenceKeyPrefix = "Theme_";
    private const string ModePreferenceKey = "Theme_Mode_v2";
    private const string SavedThemesPreferenceKey = "SavedThemes_v1";
    private const string ModeSnapshotKey = "__Mode";

    // Dark palette (the original default).
    private static readonly Dictionary<string, string> DarkColors = new()
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
        { "ThemePageBackgroundColor", "#1a1a2e" },
    };

    // Light palette. Same keys as Dark; tune any of these in Settings > Theme.
    private static readonly Dictionary<string, string> LightColors = new()
    {
        { "ThemeSectionBorderColor", "#008080" },
        { "ThemeDetailBorderColor1", "#008080" },
        { "ThemeDetailBorderColor2", "#008080" },
        { "ThemePrimaryButtonColor", "#2F8F3A" },
        { "ThemeNeutralButtonColor", "#6B7280" },
        { "ThemeDangerButtonColor",  "#D93636" },
        { "ThemeWarningButtonColor", "#C9560A" },
        { "ThemeCreateButtonColor",  "#0A9BC4" },
        { "ThemeConfirmButtonColor", "#22A745" },
        { "ThemeSummaryButtonColor", "#6A6AE6" },
        { "ThemePanelBackgroundColor", "#E8EBF3" },
        { "ThemePanelHeaderBackgroundColor", "#D3D9E8" },
        { "ThemePageBackgroundColor", "#F4F5FA" },
    };

    private ThemeMode _savedMode = ThemeMode.Dark;
    private ThemeMode _mode = ThemeMode.Dark;
    private bool _modeLoaded;

    public IReadOnlyList<string> ThemeableKeys => DarkColors.Keys.ToList();
    public bool HasUnsavedChanges { get; private set; }
    public Color? ClipboardColor { get; set; }

    public ThemeMode Mode
    {
        get
        {
            EnsureMode();
            return _mode;
        }
    }

    // ===== Mode helpers =====

    private static Dictionary<string, string> DefaultsFor(ThemeMode mode) =>
        mode == ThemeMode.Light ? LightColors : DarkColors;

    private static string ModeKey(ThemeMode mode, string key) => $"Theme_{mode}_{key}";

    // First run only: follow the OS once and remember it. After that the OS setting is ignored.
    private void EnsureMode()
    {
        if (_modeLoaded) return;

        string saved = Preferences.Default.Get(ModePreferenceKey, string.Empty);
        if (!Enum.TryParse<ThemeMode>(saved, out var mode) || !Enum.IsDefined(mode))
        {
            mode = AppInfo.Current.RequestedTheme == AppTheme.Light ? ThemeMode.Light : ThemeMode.Dark;
            Preferences.Default.Set(ModePreferenceKey, mode.ToString());
        }

        _savedMode = mode;
        _mode = mode;
        _modeLoaded = true;
    }

    // The saved color for this mode, or that mode's default.
    private static Color ResolveColor(ThemeMode mode, string key)
    {
        string saved = Preferences.Default.Get(ModeKey(mode, key), string.Empty);

        if (string.IsNullOrEmpty(saved) && mode == ThemeMode.Dark)
        {
            saved = Preferences.Default.Get(LegacyPreferenceKeyPrefix + key, string.Empty);
        }

        if (!string.IsNullOrEmpty(saved) && Color.TryParse(saved, out var color)) return color;

        return Color.TryParse(DefaultsFor(mode)[key], out var fallback) ? fallback : Colors.Gray;
    }

    // Applies a mode's colors to the live resources and forces AppThemeBinding to match it.
    private static void ApplyMode(ThemeMode mode)
    {
        var resources = Application.Current!.Resources;
        foreach (var key in DarkColors.Keys)
        {
            resources[key] = ResolveColor(mode, key);
        }

        Application.Current.UserAppTheme = mode == ThemeMode.Light ? AppTheme.Light : AppTheme.Dark;
        BodyTextColor.Apply();   // body text default follows Dark / Light unless a color was chosen
    }

    // ===== Theme =====

    public void RegisterDefaults(ResourceDictionary resources)
    {
        EnsureMode();

        foreach (var kvp in DefaultsFor(_mode))
        {
            if (Color.TryParse(kvp.Value, out var color))
            {
                resources[kvp.Key] = color;
            }
        }
    }

    public void ApplyTheme()
    {
        EnsureMode();
        ApplyMode(_mode);
    }

    public void PreviewMode(ThemeMode mode)
    {
        EnsureMode();

        _mode = mode;
        ApplyMode(mode);
        HasUnsavedChanges = true;
    }

    public void PreviewThemeColor(string resourceKey, Color color)
    {
        if (!DarkColors.ContainsKey(resourceKey))
        {
            System.Diagnostics.Debug.WriteLine($"=== ThemeService: '{resourceKey}' is not a recognized themeable key ===");
            return;
        }

        Application.Current!.Resources[resourceKey] = color;
        HasUnsavedChanges = true;
    }

    public void SaveChanges()
    {
        EnsureMode();

        Preferences.Default.Set(ModePreferenceKey, _mode.ToString());

        foreach (var key in DarkColors.Keys)
        {
            string hex = GetThemeColor(key).ToArgbHex();

            // Only edits are stored; a color equal to the mode's default is cleared so it follows the default
            bool isDefault = Color.TryParse(DefaultsFor(_mode)[key], out var def) && def.ToArgbHex() == hex;
            if (isDefault) Preferences.Default.Remove(ModeKey(_mode, key));
            else Preferences.Default.Set(ModeKey(_mode, key), hex);

            // The pre-modes value has now been migrated into the Dark entry
            if (_mode == ThemeMode.Dark) Preferences.Default.Remove(LegacyPreferenceKeyPrefix + key);
        }

        _savedMode = _mode;
        HasUnsavedChanges = false;
    }

    public void DiscardChanges()
    {
        EnsureMode();

        _mode = _savedMode;
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
        EnsureMode();

        foreach (var kvp in DefaultsFor(_mode))
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
        EnsureMode();

        var themes = LoadSavedThemesRaw();
        var snapshot = new Dictionary<string, string>
        {
            [ModeSnapshotKey] = _mode.ToString()   // so loading it also restores Dark / Light
        };

        foreach (var key in DarkColors.Keys)
        {
            snapshot[key] = GetThemeColor(key).ToArgbHex();
        }

        themes[themeName] = snapshot;
        SaveSavedThemesRaw(themes);
    }

    public void LoadNamedTheme(string themeName)
    {
        EnsureMode();

        var themes = LoadSavedThemesRaw();
        if (!themes.TryGetValue(themeName, out var snapshot)) return;

        // Mode first (it resets the palette), then the saved colors on top. Older themes have no mode entry.
        if (snapshot.TryGetValue(ModeSnapshotKey, out var modeText)
            && Enum.TryParse<ThemeMode>(modeText, out var mode)
            && Enum.IsDefined(mode)
            && mode != _mode)
        {
            PreviewMode(mode);
        }

        foreach (var kvp in snapshot)
        {
            if (kvp.Key == ModeSnapshotKey) continue;

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