using CharManJur.Models;
using CharManJur.Services;
using Microsoft.Extensions.DependencyInjection;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace CharManJur.Controls;

// Wraps ONE Button. If a loaded skin pack has an image for SkinKey, the image is drawn behind the button
// (nine-slice) and the button turns transparent, so Text / Command / Clicked / IsEnabled keep working.
// With no image, the button is a plain theme-colored Button, exactly as before.
//
// Usage: put layout properties (Grid.Row/Column, HorizontalOptions, VerticalOptions) on the SkinHost,
// the button's color on FallbackColor, and everything else on the Button. IsVisible stays on the Button;
// the host follows it.
public class SkinHost : Grid
{
    private const double DimmedOpacity = 0.45;
    private const double PressedScale = 0.95;

    public static readonly BindableProperty SkinKeyProperty = BindableProperty.Create(
        nameof(SkinKey), typeof(string), typeof(SkinHost), string.Empty,
        propertyChanged: (b, o, n) => ((SkinHost)b).Refresh());

    public static readonly BindableProperty FallbackColorProperty = BindableProperty.Create(
        nameof(FallbackColor), typeof(Color), typeof(SkinHost), null,
        propertyChanged: (b, o, n) => ((SkinHost)b).Refresh());

    public static readonly BindableProperty SkinTextColorProperty = BindableProperty.Create(
        nameof(SkinTextColor), typeof(Color), typeof(SkinHost), Colors.White,
        propertyChanged: (b, o, n) => ((SkinHost)b).Refresh());

    // Skin image to use: primary, neutral, danger, warning, create, confirm, summary, generic, generic_small
    public string SkinKey
    {
        get => (string)GetValue(SkinKeyProperty);
        set => SetValue(SkinKeyProperty, value);
    }

    // The button's color when no skin image applies (usually a theme color)
    public Color? FallbackColor
    {
        get => (Color?)GetValue(FallbackColorProperty);
        set => SetValue(FallbackColorProperty, value);
    }

    // Button text color while a skin image is showing
    public Color SkinTextColor
    {
        get => (Color)GetValue(SkinTextColorProperty);
        set => SetValue(SkinTextColorProperty, value);
    }

    private readonly SkinDrawable _drawable = new();
    private GraphicsView? _canvasView;
    private Button? _button;
    private ISkinService? _skins;

    private bool _attached;
    private bool _skinned;
    private bool _pressed;
    private bool _vsmSuppressed;
    private Color? _originalTextColor;

    public SkinHost()
    {
        HandlerChanged += OnHostHandlerChanged;
    }

    private void OnHostHandlerChanged(object? sender, EventArgs e)
    {
        if (Handler != null) Attach();
        else Detach();
    }

    // ===== Attach / detach =====

    private void Attach()
    {
        if (_attached) return;

        _button = Children.OfType<Button>().FirstOrDefault();
        _skins = IPlatformApplication.Current?.Services.GetService<ISkinService>();
        if (_button == null || _skins == null) return;

        if (_canvasView == null)
        {
            _canvasView = new GraphicsView { Drawable = _drawable, InputTransparent = true, IsVisible = false };
            Children.Insert(0, _canvasView);   // index 0 = drawn underneath the button
        }

        _button.PropertyChanged += OnButtonPropertyChanged;
        _button.Pressed += OnButtonPressed;
        _button.Released += OnButtonReleased;
        _button.Clicked += OnButtonClicked;
        _skins.SkinsChanged += OnSkinsChanged;
        _attached = true;

        IsVisible = _button.IsVisible;
        Refresh();
    }

    private void Detach()
    {
        if (!_attached) return;

        if (_button != null)
        {
            _button.PropertyChanged -= OnButtonPropertyChanged;
            _button.Pressed -= OnButtonPressed;
            _button.Released -= OnButtonReleased;
            _button.Clicked -= OnButtonClicked;
        }

        if (_skins != null) _skins.SkinsChanged -= OnSkinsChanged;
        _attached = false;
        _pressed = false;
    }

    // ===== Button events =====

    private void OnButtonPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(VisualElement.IsVisible):
                if (_button != null) IsVisible = _button.IsVisible;
                break;
            case nameof(VisualElement.IsEnabled):
            case nameof(Button.CornerRadius):
                Refresh();
                break;
        }
    }

    private void OnButtonPressed(object? sender, EventArgs e)
    {
        _pressed = true;
        Refresh();
    }

    private void OnButtonReleased(object? sender, EventArgs e)
    {
        _pressed = false;
        Refresh();
    }

    private void OnButtonClicked(object? sender, EventArgs e)
    {
        if (!_pressed) return;
        _pressed = false;
        Refresh();
    }

    private void OnSkinsChanged(object? sender, EventArgs e)
    {
        if (Handler == null)
        {
            Detach();
            return;
        }

        MainThread.BeginInvokeOnMainThread(Refresh);
    }

    // ===== Applying the skin =====

    private void Refresh()
    {
        if (!_attached || _button == null || _skins == null || _canvasView == null) return;

        string key = SkinKey?.Trim() ?? string.Empty;
        var resolved = key.Length == 0 ? null : _skins.Resolve(key);
        if (resolved == null)
        {
            ShowPlain();
            return;
        }

        // Optional state art: <key>_disabled and <key>_pressed
        ResolvedSkinImage? stateArt = null;
        if (!_button.IsEnabled) stateArt = _skins.Resolve(key + "_disabled");
        else if (_pressed) stateArt = _skins.Resolve(key + "_pressed");

        var art = stateArt ?? resolved;
        var image = SkinImageCache.Load(art.FilePath);
        if (image == null)
        {
            ShowPlain();
            return;
        }

        ShowSkinned(
            image,
            art.Margins,
            dim: !_button.IsEnabled && stateArt == null,
            shrink: _button.IsEnabled && _pressed && stateArt == null);
    }

    private void ShowSkinned(IImage image, NineSliceMargins margins, bool dim, bool shrink)
    {
        if (_button == null || _canvasView == null) return;

        _originalTextColor ??= _button.TextColor;
        _skinned = true;

        SuppressVisualStates();
        _button.BackgroundColor = Colors.Transparent;
        _button.TextColor = SkinTextColor;

        _drawable.Image = image;
        _drawable.Margins = margins;
        _drawable.CornerRadius = _button.CornerRadius >= 0 ? _button.CornerRadius : 8;

        _canvasView.IsVisible = true;
        _canvasView.Invalidate();

        Opacity = dim ? DimmedOpacity : 1;
        Scale = shrink ? PressedScale : 1;
    }

    private void ShowPlain()
    {
        if (_button == null || _canvasView == null) return;

        _canvasView.IsVisible = false;

        if (_skinned)
        {
            _skinned = false;
            Opacity = 1;
            Scale = 1;
            RestoreVisualStates();

            // Text color first, then the background: the background change re-runs the theme contrast logic
            if (_originalTextColor != null) _button.TextColor = _originalTextColor;
            _originalTextColor = null;
        }

        var fallback = FallbackColor;
        if (fallback != null && _button.BackgroundColor != fallback)
        {
            _button.BackgroundColor = fallback;
        }
    }

    // The app-wide Button style turns a disabled button gray. While skinned, the image does the talking,
    // so that visual state is switched off (the host dims instead).
    private void SuppressVisualStates()
    {
        if (_vsmSuppressed || _button == null) return;

        var common = new VisualStateGroup { Name = "CommonStates" };
        common.States.Add(new VisualState { Name = "Normal" });
        common.States.Add(new VisualState { Name = "Disabled" });
        common.States.Add(new VisualState { Name = "PointerOver" });

        var groups = new VisualStateGroupList { common };
        VisualStateManager.SetVisualStateGroups(_button, groups);
        _vsmSuppressed = true;
    }

    private void RestoreVisualStates()
    {
        if (!_vsmSuppressed || _button == null) return;

        _button.ClearValue(VisualStateManager.VisualStateGroupsProperty);
        _vsmSuppressed = false;
    }
}