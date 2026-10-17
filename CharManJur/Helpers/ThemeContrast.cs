using Microsoft.Maui.Handlers;

namespace CharManJur.Helpers;

// Keeps button text readable on player-chosen theme colors: white text, unless the button is light
// enough that white would be hard to read, then black. Applies only to buttons whose background is
// one of the theme's button colors.
public static class ThemeContrast
{
    // White text needs at least this contrast against the button (WCAG's bar for large text/UI is 3:1).
    private const double MinWhiteContrast = 3.0;

    private static readonly string[] ButtonThemeKeys =
    {
        "ThemePrimaryButtonColor", "ThemeNeutralButtonColor", "ThemeDangerButtonColor",
        "ThemeWarningButtonColor", "ThemeCreateButtonColor", "ThemeConfirmButtonColor",
        "ThemeSummaryButtonColor"
    };

    // Call once at startup, before the app builds.
    public static void Register()
    {
        // Re-evaluate whenever either input changes, in whichever order they arrive
        ButtonHandler.Mapper.AppendToMapping(nameof(IView.Background), (handler, view) => Apply(view));
        ButtonHandler.Mapper.AppendToMapping(nameof(ITextStyle.TextColor), (handler, view) => Apply(view));
    }

    private static void Apply(IButton view)
    {
        if (view is not Button button) return;

        Color? background = button.BackgroundColor ?? (button.Background as SolidColorBrush)?.Color;
        if (background == null || !IsThemeButtonColor(background)) return;

        Color desired = TextColorFor(background);
        if (button.TextColor != desired) button.TextColor = desired;   // equal -> stops the re-entrant mapping call
    }

    private static bool IsThemeButtonColor(Color color)
    {
        var resources = Application.Current?.Resources;
        if (resources == null) return false;

        foreach (string key in ButtonThemeKeys)
        {
            if (resources.TryGetValue(key, out var value) && value is Color themeColor && themeColor == color)
                return true;
        }
        return false;
    }

    public static Color TextColorFor(Color background)
    {
        double whiteContrast = 1.05 / (Luminance(background) + 0.05);
        return whiteContrast >= MinWhiteContrast ? Colors.White : Colors.Black;
    }

    private static double Luminance(Color c)
    {
        static double Linear(float v) => v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        return 0.2126 * Linear(c.Red) + 0.7152 * Linear(c.Green) + 0.0722 * Linear(c.Blue);
    }
}