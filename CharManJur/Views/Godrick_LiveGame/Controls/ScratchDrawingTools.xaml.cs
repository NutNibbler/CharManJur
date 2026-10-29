using CharManJur.Helpers;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame.Controls;

public partial class ScratchDrawingTools : ContentView
{
    private DrawingBoard? _board;

    public ScratchDrawingTools()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_board != null) _board.Redraw -= UpdateButtons;
        _board = (BindingContext as ScratchPadViewModel)?.Board;
        if (_board != null) _board.Redraw += UpdateButtons;
        UpdateButtons();
    }

    private void OnToolClicked(object? sender, EventArgs e)
    {
        if (_board != null && sender is Button b && Enum.TryParse<DrawTool>(b.StyleId, out var tool)) _board.Tool = tool;
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
        Mark(BtnHand, _board.Tool == DrawTool.Hand);
        Mark(BtnPen, _board.Tool == DrawTool.Pen);
        Mark(BtnLine, _board.Tool == DrawTool.Line);
        Mark(BtnRect, _board.Tool == DrawTool.Rectangle);
        Mark(BtnCircle, _board.Tool == DrawTool.Circle);
        Mark(BtnText, _board.Tool == DrawTool.Text);
        Mark(BtnErase, _board.Tool == DrawTool.Eraser);

        LblBlack.Text = _board.ColorName == "Black" ? "✓" : string.Empty;
        LblRed.Text = _board.ColorName == "Red" ? "✓" : string.Empty;
        LblBlue.Text = _board.ColorName == "Blue" ? "✓" : string.Empty;

        BtnSnap.Text = _board.SnapToGrid ? "🧲 Snap: On" : "🧲 Snap: Off";
        BtnGrid.Text = _board.ShowGrid ? "▦ Grid: On" : "▦ Grid: Off";
        BtnUndo.IsEnabled = _board.CanUndo;
        BtnRedo.IsEnabled = _board.CanRedo;
        ZoomLabel.Text = $"{_board.Zoom * 100:0}%";
    }
}