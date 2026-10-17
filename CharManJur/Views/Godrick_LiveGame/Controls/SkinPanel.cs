using CharManJur.Models;
using CharManJur.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CharManJur.Controls;

// Draws either the skin's "panel" image (nine-slice) or the plain card look (optional fill + rounded stroke)
// behind its content. Use it in place of a Border:
//
//   <skin:SkinPanel StrokeColor="{DynamicResource ThemeSectionBorderColor}" StrokeThickness="1"
//                   CornerRadius="8" ContentPadding="8">
//       <VerticalStackLayout> ... </VerticalStackLayout>
//   </skin:SkinPanel>
//
// Put ONE child inside, and set the padding with ContentPadding (not Padding, and no Margin on the child).
internal sealed class PanelDrawable : IDrawable
{
    public SkinDrawable Skin { get; } = new();
    public bool UseSkin { get; set; }

    public Color? Fill { get; set; }
    public Color? Stroke { get; set; }
    public float StrokeThickness { get; set; } = 1;
    public float CornerRadius { get; set; } = 8;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (UseSkin)
        {
            Skin.Draw(canvas, dirtyRect);
            return;
        }

        float t = Math.Max(0, StrokeThickness);
        float half = t / 2;
        var rect = new RectF(half, half, dirtyRect.Width - t, dirtyRect.Height - t);
        if (rect.Width <= 0 || rect.Height <= 0) return;

        float radius = Math.Max(0, Math.Min(CornerRadius, Math.Min(rect.Width, rect.Height) / 2));

        if (Fill != null && Fill.Alpha > 0)
        {
            canvas.FillColor = Fill;
            canvas.FillRoundedRectangle(rect, radius);
        }

        if (Stroke != null && t > 0)
        {
            canvas.StrokeColor = Stroke;
            canvas.StrokeSize = t;
            canvas.DrawRoundedRectangle(rect, radius);
        }
    }
}

public class SkinPanel : Grid
{
    public static readonly BindableProperty SkinKeyProperty = BindableProperty.Create(
        nameof(SkinKey), typeof(string), typeof(SkinPanel), SkinKeys.Panel,
        propertyChanged: (b, o, n) => ((SkinPanel)b).Refresh());

    public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
        nameof(FillColor), typeof(Color), typeof(SkinPanel), null,
        propertyChanged: (b, o, n) => ((SkinPanel)b).Refresh());

    public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(
        nameof(StrokeColor), typeof(Color), typeof(SkinPanel), null,
        propertyChanged: (b, o, n) => ((SkinPanel)b).Refresh());

    public static readonly BindableProperty StrokeThicknessProperty = BindableProperty.Create(
        nameof(StrokeThickness), typeof(double), typeof(SkinPanel), 1.0,
        propertyChanged: (b, o, n) => ((SkinPanel)b).Refresh());

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(SkinPanel), 8.0,
        propertyChanged: (b, o, n) => ((SkinPanel)b).Refresh());

    public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
        nameof(ContentPadding), typeof(Thickness), typeof(SkinPanel), new Thickness(0),
        propertyChanged: (b, o, n) => ((SkinPanel)b).ApplyContentPadding());

    // Skin image key to use (default "panel")
    public string SkinKey
    {
        get => (string)GetValue(SkinKeyProperty);
        set => SetValue(SkinKeyProperty, value);
    }

    // Plain look (no skin image): optional fill and stroke
    public Color? FillColor
    {
        get => (Color?)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public Color? StrokeColor
    {
        get => (Color?)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    // Space between the panel's edge and its content
    public Thickness ContentPadding
    {
        get => (Thickness)GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    private readonly PanelDrawable _drawable = new();
    private readonly GraphicsView _canvasView;
    private ISkinService? _skins;
    private bool _attached;

    public SkinPanel()
    {
        _canvasView = new GraphicsView { Drawable = _drawable, InputTransparent = true };
        Children.Insert(0, _canvasView);   // index 0 = drawn underneath the content

        ChildAdded += (_, e) =>
        {
            if (e.Element is View view && !ReferenceEquals(view, _canvasView)) view.Margin = ContentPadding;
        };

        HandlerChanged += OnHostHandlerChanged;
        Refresh();
    }

    private void OnHostHandlerChanged(object? sender, EventArgs e)
    {
        if (Handler != null) Attach();
        else Detach();
    }

    private void Attach()
    {
        if (_attached) return;

        _skins = IPlatformApplication.Current?.Services.GetService<ISkinService>();
        if (_skins != null)
        {
            _skins.SkinsChanged += OnSkinsChanged;
            _attached = true;
        }

        Refresh();
    }

    private void Detach()
    {
        if (!_attached) return;
        if (_skins != null) _skins.SkinsChanged -= OnSkinsChanged;
        _attached = false;
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

    private void ApplyContentPadding()
    {
        foreach (var child in Children)
        {
            if (child is View view && !ReferenceEquals(view, _canvasView)) view.Margin = ContentPadding;
        }
    }

    private void Refresh()
    {
        _drawable.Fill = FillColor;
        _drawable.Stroke = StrokeColor;
        _drawable.StrokeThickness = (float)StrokeThickness;
        _drawable.CornerRadius = (float)CornerRadius;
        _drawable.Skin.CornerRadius = (float)CornerRadius;

        var image = default(Microsoft.Maui.Graphics.IImage);
        NineSliceMargins margins = NineSliceMargins.Uniform(SkinPack.DefaultNineSlice);

        string key = SkinKey?.Trim() ?? string.Empty;
        if (_skins != null && key.Length > 0)
        {
            var resolved = _skins.Resolve(key);
            if (resolved != null)
            {
                image = SkinImageCache.Load(resolved.FilePath);
                margins = resolved.Margins;
            }
        }

        _drawable.UseSkin = image != null;
        _drawable.Skin.Image = image;
        _drawable.Skin.Margins = margins;

        _canvasView.Invalidate();
    }
}