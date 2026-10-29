namespace CharManJur.Services;

// Global UI scale (percent). Android: density override applied at activity start (restart needed).
// Windows: the window root is scaled and given a proportionally larger/smaller layout size (live).
public static class UiScale
{
    public const string PrefKey = "UiScalePercent";
    private const string PrevKey = "UiScalePreviousPercent";
    private const string PendingKey = "UiScaleUnconfirmed";
    public const int Min = 50, Max = 150, Default = 100, Step = 5;

    public static event Action? Changed;

    public static int Percent => Math.Clamp(Preferences.Default.Get(PrefKey, Default), Min, Max);
    public static bool RestartNeeded => DeviceInfo.Platform == DevicePlatform.Android;
    public static bool IsPending => Preferences.Default.Get(PendingKey, false);

    // Saves a new scale and marks it as awaiting confirmation.
    public static void Set(int percent)
    {
        percent = Math.Clamp(percent, Min, Max);
        if (percent == Percent) return;

        if (!IsPending) Preferences.Default.Set(PrevKey, Percent);   // keep the last CONFIRMED value
        Preferences.Default.Set(PendingKey, true);
        Preferences.Default.Set(PrefKey, percent);
        Changed?.Invoke();
    }

    public static void Keep() => Preferences.Default.Set(PendingKey, false);

    public static void Revert()
    {
        Preferences.Default.Set(PrefKey, Math.Clamp(Preferences.Default.Get(PrevKey, Default), Min, Max));
        Preferences.Default.Set(PendingKey, false);
        Changed?.Invoke();
    }

    // Asks whether to keep an unconfirmed scale. Returns true when kept (or nothing was pending).
    public static async Task<bool> AskKeepAsync(Page? page)
    {
        if (!IsPending || page == null) return true;

        bool keep = await page.DisplayAlertAsync(
            "Keep this UI scale?",
            $"The UI is at {Percent}%. If you can't read it or reach the buttons, choose Revert.",
            "Keep", "Revert");

        if (keep) Keep(); else Revert();
        return keep;
    }

#if WINDOWS
    private static Microsoft.UI.Xaml.Window? _nativeWindow;
    private static bool _hooked;

    public static void Apply(Microsoft.Maui.Controls.Window? window)
    {
        if (window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window native) return;

        _nativeWindow = native;
        if (!_hooked)
        {
            native.SizeChanged += (_, _) => ApplyCurrent();
            _hooked = true;
        }
        ApplyCurrent();
    }

    private static void ApplyCurrent()
    {
        if (_nativeWindow?.Content is not Microsoft.UI.Xaml.FrameworkElement root) return;

        double s = Percent / 100.0;
        if (Math.Abs(s - 1.0) < 0.001)
        {
            root.RenderTransform = null;
            root.Width = double.NaN;
            root.Height = double.NaN;
            root.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
            root.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
            return;
        }

        root.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left;
        root.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top;
        root.RenderTransformOrigin = new Windows.Foundation.Point(0, 0);
        root.RenderTransform = new Microsoft.UI.Xaml.Media.ScaleTransform { ScaleX = s, ScaleY = s };

        var bounds = _nativeWindow.Bounds;
        root.Width = bounds.Width / s;
        root.Height = bounds.Height / s;
    }
#else
    public static void Apply(Microsoft.Maui.Controls.Window? window) { }
#endif
}