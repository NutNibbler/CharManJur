using CharManJur.Models;
using CharManJur.Services;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if WINDOWS
using Microsoft.Maui.Handlers;
#endif

namespace CharManJur.Controls;

// Paints the "page_background" skin image behind every ContentPage (Windows only).
//
//   * Pages with a transparent background (see-through popups / overlays) are left alone.
//   * A single page can opt out with:  skin:SkinBackground.Enabled="False"
//   * With no page_background image in any enabled pack, the page keeps its normal theme color.
//
// Call SkinBackground.Register(skinService) once at startup (App.xaml.cs).
public static class SkinBackground
{
    public static readonly BindableProperty EnabledProperty = BindableProperty.CreateAttached(
        "Enabled", typeof(bool), typeof(SkinBackground), true,
        propertyChanged: OnEnabledChanged);

    public static bool GetEnabled(BindableObject page) => (bool)page.GetValue(EnabledProperty);
    public static void SetEnabled(BindableObject page, bool value) => page.SetValue(EnabledProperty, value);

    private static ISkinService? _skins;
    private static bool _registered;

    public static void Register(ISkinService skins)
    {
        if (_registered) return;
        _registered = true;
        _skins = skins;

#if WINDOWS
        _skins.SkinsChanged += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshAll);

        // Runs after MAUI's own background mapping, so our image wins; also re-runs when the theme recolors the page.
        PageHandler.Mapper.AppendToMapping(nameof(IView.Background), (handler, view) => ApplyTo(handler));
        PageHandler.Mapper.AppendToMapping("SkinBackground", (handler, view) => ApplyTo(handler));
#endif
    }

    private static void OnEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
#if WINDOWS
        if (bindable is ContentPage page && page.Handler != null) ApplyTo(page.Handler);
#endif
    }

    // A page counts as "transparent" (popup / overlay) when its own color has no opacity
    private static bool IsTransparent(ContentPage page)
    {
        if (page.BackgroundColor != null) return page.BackgroundColor.Alpha <= 0f;
        return page.Background is SolidColorBrush solid && solid.Color.Alpha <= 0f;
    }

#if WINDOWS
    private static readonly List<WeakReference<ContentPage>> Pages = new();
    private static readonly ConditionalWeakTable<IElementHandler, object> Applied = new();
    private static readonly object Marker = new();

    private static readonly Dictionary<string,
        (DateTime Stamp, Microsoft.UI.Xaml.Media.Imaging.BitmapImage Bitmap, Stream Source)> BitmapCache =
        new(StringComparer.OrdinalIgnoreCase);

    private static void Track(ContentPage page)
    {
        for (int i = Pages.Count - 1; i >= 0; i--)
        {
            if (!Pages[i].TryGetTarget(out var existing)) { Pages.RemoveAt(i); continue; }
            if (ReferenceEquals(existing, page)) return;
        }
        Pages.Add(new WeakReference<ContentPage>(page));
    }

    private static void RefreshAll()
    {
        for (int i = Pages.Count - 1; i >= 0; i--)
        {
            if (!Pages[i].TryGetTarget(out var page)) { Pages.RemoveAt(i); continue; }
            if (page.Handler != null) ApplyTo(page.Handler);
        }
    }

    private static void ApplyTo(IElementHandler handler)
    {
        if (handler.VirtualView is not ContentPage page) return;
        if (handler.PlatformView is not Microsoft.Maui.Platform.ContentPanel panel) return;

        Track(page);

        Microsoft.UI.Xaml.Media.ImageBrush? brush = null;
        if (GetEnabled(page) && !IsTransparent(page)) brush = CreateBrush();

        if (brush != null)
        {
            panel.Background = brush;
            Applied.AddOrUpdate(handler, Marker);
        }
        else if (Applied.Remove(handler))
        {
            // Skin removed: let MAUI repaint the normal theme background
            handler.UpdateValue(nameof(IView.Background));
        }
    }

    private static Microsoft.UI.Xaml.Media.ImageBrush? CreateBrush()
    {
        var resolved = _skins?.Resolve(SkinKeys.PageBackground);
        if (resolved == null) return null;

        var bitmap = LoadBitmap(resolved.FilePath);
        if (bitmap == null) return null;

        return new Microsoft.UI.Xaml.Media.ImageBrush
        {
            ImageSource = bitmap,
            Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill,   // "cover": fills the window, never distorts
            AlignmentX = Microsoft.UI.Xaml.Media.AlignmentX.Center,
            AlignmentY = Microsoft.UI.Xaml.Media.AlignmentY.Center
        };
    }

    private static Microsoft.UI.Xaml.Media.Imaging.BitmapImage? LoadBitmap(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;

            var stamp = File.GetLastWriteTimeUtc(path);
            if (BitmapCache.TryGetValue(path, out var cached) && cached.Stamp == stamp) return cached.Bitmap;

            var source = new MemoryStream(File.ReadAllBytes(path));
            var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
            bitmap.SetSource(source.AsRandomAccessStream());

            BitmapCache[path] = (stamp, bitmap, source);   // keep the stream alive with the bitmap
            return bitmap;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== SkinBackground: could not load '{path}': {ex.Message} ===");
            return null;
        }
    }
#endif
}