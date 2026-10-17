namespace CharManJur.Services;

// The "body text color": one chosen color for default Labels. With none chosen, body text follows
// the base theme (Black on Light, White on Dark). Only Labels using the default style are affected;
// a Label with its own explicit TextColor keeps it.
public static class BodyTextColor
{
    public const string ResourceKey = "BodyTextColor";
    private const string PreferenceKey = "Skins_BodyTextColor_v1";

    private static Color? _live;      // what is showing right now (may be an unsaved preview)
    private static bool _loaded;

    public static bool HasOverride
    {
        get { EnsureLoaded(); return _live != null; }
    }

    // The color currently in effect (the chosen one, or the theme default)
    public static Color Current
    {
        get { EnsureLoaded(); return _live ?? DefaultColor(); }
    }

    public static bool HasUnsavedChange
    {
        get { EnsureLoaded(); return !Same(_live, LoadSaved()); }
    }

    // Live-previews a color (null = theme default). Not persisted until Save().
    public static void Preview(Color? color)
    {
        EnsureLoaded();
        _live = color;
        Apply();
    }

    public static void Save()
    {
        EnsureLoaded();
        if (_live == null) Preferences.Default.Remove(PreferenceKey);
        else Preferences.Default.Set(PreferenceKey, _live.ToArgbHex());
    }

    public static void Discard()
    {
        EnsureLoaded();
        _live = LoadSaved();
        Apply();
    }

    // Pushes the current value into the app resources (also called when the base theme changes)
    public static void Apply()
    {
        EnsureLoaded();
        if (Application.Current == null) return;
        Application.Current.Resources[ResourceKey] = Current;
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _live = LoadSaved();
        _loaded = true;
    }

    private static Color? LoadSaved()
    {
        string saved = Preferences.Default.Get(PreferenceKey, string.Empty);
        return !string.IsNullOrEmpty(saved) && Color.TryParse(saved, out var color) ? color : null;
    }

    private static Color DefaultColor()
    {
        var theme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
        if (theme == AppTheme.Unspecified) theme = AppInfo.Current.RequestedTheme;
        return theme == AppTheme.Light ? Colors.Black : Colors.White;
    }

    private static bool Same(Color? a, Color? b) =>
        a == null ? b == null : b != null && a.ToArgbHex() == b.ToArgbHex();
}