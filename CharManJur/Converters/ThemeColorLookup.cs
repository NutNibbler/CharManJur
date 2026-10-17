namespace CharManJur.Converters;

// Converters can't use {DynamicResource}, so they read the live theme resource here.
internal static class ThemeColorLookup
{
    public static Color Get(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
        {
            return color;
        }

        return fallback;
    }
}