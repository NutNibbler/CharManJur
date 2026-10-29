using CharManJur.Helpers;
using CharManJur.Models;
using CharManJur.ViewModels;

namespace CharManJur.Views;

public partial class GmMapPanelView : ContentView
{
    private GmMapViewModel? _vm;
    private MapBoard? _board;

    public GmMapPanelView()
    {
        InitializeComponent();
        Surface.HandlerChanged += OnSurfaceHandlerChanged;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_board != null)
        {
            _board.Redraw -= OnBoardRedraw;
            _board.ViewFitted -= OnViewFitted;
            _board.TextRequested -= OnTextRequested;
        }

        _vm = BindingContext as GmMapViewModel;
        _board = _vm?.Board;

        if (_board != null)
        {
            Surface.Drawable = _board;
            _board.Redraw += OnBoardRedraw;
            _board.ViewFitted += OnViewFitted;
            _board.TextRequested += OnTextRequested;
        }
        UpdateButtons();
    }

    private void OnBoardRedraw()
    {
        Surface.Invalidate();
        UpdateButtons();
    }

    // The map was fitted into the view while drawing: the zoom label needs to catch up.
    private void OnViewFitted() => Dispatcher.Dispatch(UpdateButtons);

    private async void OnTextRequested(DrawPoint at) => await AskTextAsync(at);

    private async Task AskTextAsync(DrawPoint at)
    {
        var page = Application.Current?.MainPage;
        if (page == null || _board == null) return;

        string? text = await page.DisplayPromptAsync("Text Label", "Label text:", "Add", "Cancel", maxLength: 80);
        if (!string.IsNullOrWhiteSpace(text)) _board.AddText(at, text.Trim());
    }

    // ===== TOOLS =====
    private void OnToolClicked(object? sender, EventArgs e)
    {
        if (_board != null && sender is Button b && Enum.TryParse<MapTool>(b.StyleId, out var tool)) _board.Tool = tool;
        _board?.Refresh();
    }

    private void OnColorTapped(object? sender, TappedEventArgs e)
    {
        if (_board != null && sender is Border b) _board.ColorName = b.StyleId;
        _board?.Refresh();
    }

    private void OnSnapClicked(object? sender, EventArgs e) { if (_board == null) return; _board.SnapToGrid = !_board.SnapToGrid; _board.Refresh(); }
    private void OnGridClicked(object? sender, EventArgs e) { if (_board == null) return; _board.ShowGrid = !_board.ShowGrid; _board.Refresh(); }
    private void OnUndoClicked(object? sender, EventArgs e) => _board?.Undo();
    private void OnRedoClicked(object? sender, EventArgs e) => _board?.RedoLast();
    private void OnZoomOutClicked(object? sender, EventArgs e) => _board?.ZoomBy(1 / 1.25f);
    private void OnZoomInClicked(object? sender, EventArgs e) => _board?.ZoomBy(1.25f);
    private void OnFitClicked(object? sender, EventArgs e) => _board?.ResetView();

    private void UpdateButtons()
    {
        if (_board == null) return;

        static void Mark(Button b, bool on) => b.Opacity = on ? 1 : 0.55;
        Mark(BtnSelect, _board.Tool == MapTool.Select);
        Mark(BtnHand, _board.Tool == MapTool.Hand);
        Mark(BtnPen, _board.Tool == MapTool.Pen);
        Mark(BtnLine, _board.Tool == MapTool.Line);
        Mark(BtnRect, _board.Tool == MapTool.Rectangle);
        Mark(BtnCircle, _board.Tool == MapTool.Circle);
        Mark(BtnText, _board.Tool == MapTool.Text);
        Mark(BtnErase, _board.Tool == MapTool.Eraser);

        LblBlack.Text = _board.ColorName == "Black" ? "✓" : string.Empty;
        LblRed.Text = _board.ColorName == "Red" ? "✓" : string.Empty;
        LblBlue.Text = _board.ColorName == "Blue" ? "✓" : string.Empty;

        BtnSnap.Text = _board.SnapToGrid ? "🧲 Snap: On" : "🧲 Snap: Off";
        BtnGrid.Text = _board.ShowGrid ? "▦ Grid: On" : "▦ Grid: Off";
        BtnUndo.IsEnabled = _board.CanUndo;
        BtnRedo.IsEnabled = _board.CanRedo;
        ZoomLabel.Text = $"{_board.Zoom * 100:0}%";
    }

    // ===== SURFACE =====
    private void OnSurfaceStart(object? sender, TouchEventArgs e) => _board?.Begin(e.Touches);
    private void OnSurfaceDrag(object? sender, TouchEventArgs e) => _board?.Drag(e.Touches);
    private void OnSurfaceEnd(object? sender, TouchEventArgs e) => _board?.End();
    private void OnSurfaceCancel(object? sender, EventArgs e) => _board?.End();

    private void OnSurfaceHover(object? sender, TouchEventArgs e)
        => _board?.Hover(e.Touches.Length > 0 ? e.Touches[0] : null);

    private void OnSurfaceHoverEnd(object? sender, EventArgs e) => _board?.Hover(null);

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