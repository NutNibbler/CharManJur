using CharManJur.Helpers;
using CharManJur.Models;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame.Controls;

public partial class ScratchDrawingView : ContentView
{
    private ScratchPadViewModel? _vm;
    private DrawingBoard? _board;

    public ScratchDrawingView()
    {
        InitializeComponent();
        Surface.HandlerChanged += OnSurfaceHandlerChanged;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_vm != null) _vm.PropertyChanged -= OnVmChanged;
        if (_board != null)
        {
            _board.Redraw -= OnBoardRedraw;
            _board.Changed -= OnBoardChanged;
            _board.TextRequested -= OnTextRequested;
        }

        _vm = BindingContext as ScratchPadViewModel;
        _board = _vm?.Board;

        if (_vm != null) _vm.PropertyChanged += OnVmChanged;
        if (_board != null)
        {
            Surface.Drawable = _board;
            _board.Redraw += OnBoardRedraw;
            _board.Changed += OnBoardChanged;
            _board.TextRequested += OnTextRequested;
        }
        LoadIfNeeded();
    }

    private void OnBoardRedraw() => Surface.Invalidate();
    private void OnBoardChanged() => _vm?.MarkDrawingChanged();
    private async void OnTextRequested(DrawPoint at) => await AskTextAsync(at);

    // "Everything changed": a page opened. Load its shapes if this is a different drawing than the one on screen.
    private void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)) MainThread.BeginInvokeOnMainThread(LoadIfNeeded);
    }

    private string? _shownBackground;

    private void LoadIfNeeded()
    {
        if (_board == null) return;

        if (_vm?.DrawingShapes is { } shapes && !ReferenceEquals(shapes, _board.ShapeList))
        {
            _board.Load(shapes);
            _shownBackground = null;
            _board.SetBackground(null);
        }

        // Reload the picture only when the page's background actually changed
        string? wanted = _vm?.BackgroundImageName;
        if (wanted == _shownBackground) return;
        _shownBackground = wanted;
        _board.SetBackground(_vm?.LoadBackgroundImage());
    }

    // ===== SURFACE =====
    private void OnSurfaceStart(object? sender, TouchEventArgs e) => _board?.Begin(e.Touches);
    private void OnSurfaceDrag(object? sender, TouchEventArgs e) => _board?.Drag(e.Touches);
    private void OnSurfaceEnd(object? sender, TouchEventArgs e) => _board?.End();
    private void OnSurfaceCancel(object? sender, EventArgs e) => _board?.End();

    private void OnSurfaceHover(object? sender, TouchEventArgs e)
        => _board?.Hover(e.Touches.Length > 0 ? e.Touches[0] : null);

    private void OnSurfaceHoverEnd(object? sender, EventArgs e) => _board?.Hover(null);

    private async Task AskTextAsync(DrawPoint at)
    {
        var page = Application.Current?.MainPage;
        if (page == null || _board == null) return;

        string? text = await page.DisplayPromptAsync("Text Label", "Label text:", "Add", "Cancel", maxLength: 80);
        if (!string.IsNullOrWhiteSpace(text)) _board.AddText(at, text.Trim());
    }

    // ===== MOUSE (Windows only; other platforms don't compile this) =====
    private void OnSurfaceHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        if (Surface.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement element) HookPointer(element);
#endif
    }

#if WINDOWS
    private Microsoft.UI.Xaml.Input.PointerEventHandler? _onPressed, _onMoved, _onReleased;

    private void HookPointer(Microsoft.UI.Xaml.UIElement element)
    {
        element.PointerWheelChanged -= OnWheel;
        element.PointerWheelChanged += OnWheel;

        // handledEventsToo: the drawing surface handles these itself, and we still need to see them.
        if (_onPressed != null) element.RemoveHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, _onPressed);
        if (_onMoved != null) element.RemoveHandler(Microsoft.UI.Xaml.UIElement.PointerMovedEvent, _onMoved);
        if (_onReleased != null) element.RemoveHandler(Microsoft.UI.Xaml.UIElement.PointerReleasedEvent, _onReleased);

        _onPressed = new Microsoft.UI.Xaml.Input.PointerEventHandler(OnPointerPressed);
        _onMoved = new Microsoft.UI.Xaml.Input.PointerEventHandler(OnPointerMoved);
        _onReleased = new Microsoft.UI.Xaml.Input.PointerEventHandler(OnPointerReleased);
        element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, _onPressed, true);
        element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerMovedEvent, _onMoved, true);
        element.AddHandler(Microsoft.UI.Xaml.UIElement.PointerReleasedEvent, _onReleased, true);
    }

    private static PointF PositionOf(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e, out Microsoft.UI.Input.PointerPointProperties props)
    {
        var point = e.GetCurrentPoint((Microsoft.UI.Xaml.UIElement)sender);
        props = point.Properties;
        return new PointF((float)point.Position.X, (float)point.Position.Y);
    }

    private void OnPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var p = PositionOf(sender, e, out var props);

        // Queued, so it runs after the surface has handled the same press.
        if (props.IsMiddleButtonPressed) Dispatcher.Dispatch(() => _board?.BeginMiddlePan(p));
        else if (props.IsRightButtonPressed) Dispatcher.Dispatch(() => _board?.CancelDraw());
    }

    private void OnPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var p = PositionOf(sender, e, out var props);

        if (props.IsMiddleButtonPressed) _board?.MiddlePanTo(p);
        else _board?.EndMiddlePan();

        if (props.IsRightButtonPressed) _board?.CancelDraw();   // right button pressed while a drag is under way
    }

    private void OnPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        => _board?.EndMiddlePan();

    private void OnWheel(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var p = PositionOf(sender, e, out var props);
        int delta = props.MouseWheelDelta;
        if (delta == 0) return;

        _board?.ZoomAtScreen(p, delta > 0 ? 1.15f : 1 / 1.15f);
        e.Handled = true;
    }
#endif
}