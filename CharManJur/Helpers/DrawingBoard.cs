using CharManJur.Models;
using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace CharManJur.Helpers;

public enum DrawTool { Hand, Pen, Line, Rectangle, Circle, Text, Eraser }

// The drawing surface's logic and painting: tools, grid, zoom, pan, undo. Shapes live in a list shared with the page's data.
public class DrawingBoard : IDrawable
{
    public const float CanvasSize = 2000f;
    public const float GridStep = 32f;
    private const float SnapStep = GridStep / 2;   // corners, cell centers and edge midpoints
    private const float FineGridZoom = 1.5f;   // from 150% up, the half-cell lines appear
    private const float MinZoom = 0.25f, MaxZoom = 4f;
    private const float StrokeWidth = 2.5f, TextSize = 20f, TextHeight = 26f;
    private const int MaxUndo = 50;

    private List<DrawShape> _shapes = new();
    private readonly List<List<DrawShape>> _undo = new();
    private readonly List<List<DrawShape>> _redo = new();

    private DrawShape? _active;
    private bool _panning, _pinching, _erased;
    private PointF _lastScreen, _lastMid;
    private float _lastDist;
    private PointF? _hover;
    private DrawPoint? _hoverSnapped;
    private bool _ignore, _midPanning;
    private PointF _midLast;
    private IImage? _background;

    public DrawTool Tool { get; set; } = DrawTool.Pen;
    public string ColorName { get; set; } = "Black";
    public bool SnapToGrid { get; set; }
    public bool ShowGrid { get; set; } = true;

    public float Zoom { get; private set; } = 1f;
    public float OffsetX { get; private set; }
    public float OffsetY { get; private set; }
    public float ViewWidth { get; private set; }
    public float ViewHeight { get; private set; }

    public List<DrawShape> ShapeList => _shapes;
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public event Action? Redraw;                 // the picture or the toolbar state changed
    public event Action? Changed;                // the shapes changed (save them)
    public event Action<DrawPoint>? TextRequested;

    public void Refresh() => Redraw?.Invoke();

    // The page's map or picture, drawn under the grid and shapes (null = none).
    public void SetBackground(IImage? image)
    {
        _background = image;
        Redraw?.Invoke();
    }

    // The mouse moved over the surface without drawing (null = it left). Redraws only when the snap point changes.
    public void Hover(PointF? p)
    {
        _hover = p;

        bool wanted = SnapToGrid && Tool != DrawTool.Hand && Tool != DrawTool.Eraser && p.HasValue;
        var snapped = wanted ? ToWorld(p!.Value, true) : null;

        if (snapped?.X == _hoverSnapped?.X && snapped?.Y == _hoverSnapped?.Y) return;
        _hoverSnapped = snapped;
        Redraw?.Invoke();
    }

    // Wheel zoom toward the cursor (desktop).
    public void ZoomAtScreen(PointF center, float factor)
    {
        ZoomAt(center, factor);
        Redraw?.Invoke();
    }

    // Right-click while drawing: the shape in progress is dropped, and the rest of that drag does nothing.
    public void CancelDraw()
    {
        _active = null;
        _panning = false;
        _ignore = true;
        Redraw?.Invoke();
    }

    // Middle mouse panning (desktop).
    public void BeginMiddlePan(PointF p)
    {
        CancelDraw();
        _midPanning = true;
        _midLast = p;
    }

    public void MiddlePanTo(PointF p)
    {
        if (!_midPanning) return;
        Pan(p.X - _midLast.X, p.Y - _midLast.Y);
        _midLast = p;
        Redraw?.Invoke();
    }

    public void EndMiddlePan() => _midPanning = false;

    // Shows a page's shapes (the list is edited in place, so the page saves what is drawn).
    public void Load(List<DrawShape> shapes)
    {
        _shapes = shapes;
        _undo.Clear();
        _redo.Clear();
        _active = null;
        _panning = _pinching = false;
        Zoom = 1f;
        OffsetX = OffsetY = 0;
        Redraw?.Invoke();
    }

    // ===== INPUT (screen points from the view) =====
    public void Begin(PointF[] touches)
    {
        if (touches.Length == 0) return;
        if (touches.Length >= 2) { StartPinch(touches); return; }
        _ignore = false;

        var p = touches[0];
        _hover = p;
        if (Tool == DrawTool.Hand) { _panning = true; _lastScreen = p; return; }

        switch (Tool)
        {
            case DrawTool.Pen:
                var pen = ToWorld(p, true);
                _active = New(ShapeKind.Pen);
                _active.Points = new List<DrawPoint> { pen };
                break;
            case DrawTool.Line:
            case DrawTool.Rectangle:
            case DrawTool.Circle:
                var a = ToWorld(p, true);
                _active = New(Tool == DrawTool.Line ? ShapeKind.Line : Tool == DrawTool.Rectangle ? ShapeKind.Rectangle : ShapeKind.Circle);
                _active.X1 = _active.X2 = a.X;
                _active.Y1 = _active.Y2 = a.Y;
                break;
            case DrawTool.Text:
                TextRequested?.Invoke(ToWorld(p, true));
                break;
            case DrawTool.Eraser:
                _erased = false;
                EraseAt(ToWorld(p, false));
                break;
        }
        Redraw?.Invoke();
    }

    public void Drag(PointF[] touches)
    {
        if (touches.Length >= 2)
        {
            if (!_pinching) StartPinch(touches); else UpdatePinch(touches);
            return;
        }
        if (_pinching || touches.Length == 0) return;
        if (_ignore) return;

        var p = touches[0];
        _hover = p;
        if (_panning)
        {
            Pan(p.X - _lastScreen.X, p.Y - _lastScreen.Y);
            _lastScreen = p;
            Redraw?.Invoke();
            return;
        }

        if (Tool == DrawTool.Eraser) { EraseAt(ToWorld(p, false)); return; }
        if (_active == null) return;

        var w = ToWorld(p, true);
        if (_active.Kind == ShapeKind.Pen)
        {
            var last = _active.Points![^1];
            if (MathF.Abs(w.X - last.X) + MathF.Abs(w.Y - last.Y) < 2f / Zoom) return;   // skip points that add nothing
            _active.Points.Add(w);
        }
        else
        {
            _active.X2 = w.X;
            _active.Y2 = w.Y;
        }
        Redraw?.Invoke();
    }

    public void End()
    {
        if (_ignore)
        {
            _ignore = false;
            _active = null;
            _panning = _pinching = false;
            Redraw?.Invoke();
            return;
        }
        if (_active != null)
        {
            if (IsMeaningful(_active))
            {
                PushUndo();
                _shapes.Add(_active);
                _redo.Clear();
                Changed?.Invoke();
            }
            _active = null;
        }
        _panning = false;
        _pinching = false;
        Redraw?.Invoke();
    }

    public void AddText(DrawPoint at, string text)
    {
        var shape = New(ShapeKind.Text);
        shape.X1 = at.X;
        shape.Y1 = at.Y;
        shape.Text = text;

        PushUndo();
        _shapes.Add(shape);
        _redo.Clear();
        Changed?.Invoke();
        Redraw?.Invoke();
    }

    // ===== UNDO =====
    public void Undo()
    {
        if (_undo.Count == 0) return;
        _redo.Add(new List<DrawShape>(_shapes));
        Restore(_undo[^1]);
        _undo.RemoveAt(_undo.Count - 1);
        Changed?.Invoke();
        Redraw?.Invoke();
    }

    public void RedoLast()
    {
        if (_redo.Count == 0) return;
        _undo.Add(new List<DrawShape>(_shapes));
        Restore(_redo[^1]);
        _redo.RemoveAt(_redo.Count - 1);
        Changed?.Invoke();
        Redraw?.Invoke();
    }

    private void PushUndo()
    {
        _undo.Add(new List<DrawShape>(_shapes));
        if (_undo.Count > MaxUndo) _undo.RemoveAt(0);
    }

    private void Restore(List<DrawShape> snapshot)
    {
        _shapes.Clear();
        _shapes.AddRange(snapshot);
    }

    // ===== VIEW =====
    public void ZoomBy(float factor)
    {
        ZoomAt(new PointF(ViewWidth / 2, ViewHeight / 2), factor);
        Redraw?.Invoke();
    }

    public void ResetView()
    {
        Zoom = 1f;
        OffsetX = OffsetY = 0;
        Redraw?.Invoke();
    }

    private void ZoomAt(PointF center, float factor)
    {
        float wx = center.X / Zoom + OffsetX, wy = center.Y / Zoom + OffsetY;
        Zoom = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);
        OffsetX = wx - center.X / Zoom;
        OffsetY = wy - center.Y / Zoom;
        ClampOffset();
    }

    private void Pan(float screenDx, float screenDy)
    {
        OffsetX -= screenDx / Zoom;
        OffsetY -= screenDy / Zoom;
        ClampOffset();
    }

    private void ClampOffset()
    {
        float maxX = Math.Max(-50f, CanvasSize - ViewWidth / Zoom + 50f);
        float maxY = Math.Max(-50f, CanvasSize - ViewHeight / Zoom + 50f);
        OffsetX = Math.Clamp(OffsetX, -50f, maxX);
        OffsetY = Math.Clamp(OffsetY, -50f, maxY);
    }

    private void StartPinch(PointF[] t)
    {
        _hover = null;
        _pinching = true;
        _active = null;
        _panning = false;
        _lastDist = Distance(t[0], t[1]);
        _lastMid = Mid(t[0], t[1]);
        Redraw?.Invoke();
    }

    private void UpdatePinch(PointF[] t)
    {
        float dist = Distance(t[0], t[1]);
        var mid = Mid(t[0], t[1]);
        if (_lastDist > 1f && dist > 1f) ZoomAt(mid, dist / _lastDist);
        Pan(mid.X - _lastMid.X, mid.Y - _lastMid.Y);
        _lastDist = dist;
        _lastMid = mid;
        Redraw?.Invoke();
    }

    private static float Distance(PointF a, PointF b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    private static PointF Mid(PointF a, PointF b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2);

    private DrawPoint ToWorld(PointF screen, bool snap)
    {
        float x = Math.Clamp(screen.X / Zoom + OffsetX, 0, CanvasSize);
        float y = Math.Clamp(screen.Y / Zoom + OffsetY, 0, CanvasSize);
        if (snap && SnapToGrid)
        {
            x = MathF.Round(x / SnapStep) * SnapStep;
            y = MathF.Round(y / SnapStep) * SnapStep;
        }
        return new DrawPoint { X = MathF.Round(x, 1), Y = MathF.Round(y, 1) };
    }

    private DrawShape New(ShapeKind kind) => new() { Kind = kind, Color = ColorName };

    private static bool IsMeaningful(DrawShape s) => s.Kind switch
    {
        ShapeKind.Pen => s.Points is { Count: > 0 },
        _ => MathF.Abs(s.X2 - s.X1) + MathF.Abs(s.Y2 - s.Y1) >= 2f
    };

    // ===== ERASER =====
    private void EraseAt(DrawPoint p)
    {
        float tolerance = 8f / Zoom;
        if (!_shapes.Any(s => Hit(s, p, tolerance))) return;

        if (!_erased) { PushUndo(); _redo.Clear(); _erased = true; }   // one undo step per stroke
        _shapes.RemoveAll(s => Hit(s, p, tolerance));
        Changed?.Invoke();
        Redraw?.Invoke();
    }

    private static bool Hit(DrawShape s, DrawPoint p, float tol)
    {
        switch (s.Kind)
        {
            case ShapeKind.Pen:
                var pts = s.Points;
                if (pts == null || pts.Count == 0) return false;
                if (pts.Count == 1) return MathF.Abs(pts[0].X - p.X) <= tol && MathF.Abs(pts[0].Y - p.Y) <= tol;
                for (int i = 1; i < pts.Count; i++)
                    if (DistSeg(p.X, p.Y, pts[i - 1].X, pts[i - 1].Y, pts[i].X, pts[i].Y) <= tol) return true;
                return false;

            case ShapeKind.Line:
                return DistSeg(p.X, p.Y, s.X1, s.Y1, s.X2, s.Y2) <= tol;

            case ShapeKind.Rectangle:
                float l = Math.Min(s.X1, s.X2), r = Math.Max(s.X1, s.X2), t = Math.Min(s.Y1, s.Y2), b = Math.Max(s.Y1, s.Y2);
                return DistSeg(p.X, p.Y, l, t, r, t) <= tol || DistSeg(p.X, p.Y, r, t, r, b) <= tol
                    || DistSeg(p.X, p.Y, r, b, l, b) <= tol || DistSeg(p.X, p.Y, l, b, l, t) <= tol;

            case ShapeKind.Circle:
                float radius = MathF.Sqrt((s.X2 - s.X1) * (s.X2 - s.X1) + (s.Y2 - s.Y1) * (s.Y2 - s.Y1));
                float away = MathF.Sqrt((p.X - s.X1) * (p.X - s.X1) + (p.Y - s.Y1) * (p.Y - s.Y1));
                return MathF.Abs(away - radius) <= tol;

            case ShapeKind.Text:
                return p.X >= s.X1 - tol && p.X <= s.X1 + TextWidth(s) + tol && p.Y >= s.Y1 - tol && p.Y <= s.Y1 + TextHeight + tol;
        }
        return false;
    }

    private static float DistSeg(float px, float py, float ax, float ay, float bx, float by)
    {
        float dx = bx - ax, dy = by - ay, len2 = dx * dx + dy * dy;
        float t = len2 <= 0 ? 0 : Math.Clamp(((px - ax) * dx + (py - ay) * dy) / len2, 0, 1);
        float cx = ax + t * dx, cy = ay + t * dy;
        return MathF.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
    }

    private static float TextWidth(DrawShape s) => Math.Max(20f, (s.Text?.Length ?? 0) * 11f);

    // ===== PAINTING =====
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        ViewWidth = dirtyRect.Width;
        ViewHeight = dirtyRect.Height;

        canvas.SaveState();
        canvas.Scale(Zoom, Zoom);
        canvas.Translate(-OffsetX, -OffsetY);

        canvas.FillColor = Color.FromArgb("#F7F4EC");
        canvas.FillRectangle(0, 0, CanvasSize, CanvasSize);

        if (_background != null)
        {
            // Fitted into the canvas from the top-left corner, keeping its proportions
            float fit = Math.Min(CanvasSize / _background.Width, CanvasSize / _background.Height);
            canvas.DrawImage(_background, 0, 0, _background.Width * fit, _background.Height * fit);
        }

        if (ShowGrid) DrawGrid(canvas);

        canvas.StrokeColor = Color.FromArgb("#888888");
        canvas.StrokeSize = 2f / Zoom;
        canvas.DrawRectangle(0, 0, CanvasSize, CanvasSize);

        foreach (var shape in _shapes) DrawShape(canvas, shape);
        if (_active != null) DrawShape(canvas, _active);

        if (SnapToGrid && _hover.HasValue && Tool != DrawTool.Hand && Tool != DrawTool.Eraser)
        {
            var at = ToWorld(_hover.Value, true);
            var ink = ColorOf(ColorName);
            canvas.StrokeColor = ink;
            canvas.StrokeSize = 1.5f / Zoom;
            canvas.DrawCircle(at.X, at.Y, 7f / Zoom);
            canvas.FillColor = ink;
            canvas.FillCircle(at.X, at.Y, 2.5f / Zoom);
        }

        canvas.RestoreState();
    }

    private void DrawGrid(ICanvas canvas)
    {
        float step = Zoom < 0.5f ? GridStep * 2 : GridStep;   // keep the lines from crowding when zoomed out

        float right = Math.Min(CanvasSize, OffsetX + ViewWidth / Zoom);
        float bottom = Math.Min(CanvasSize, OffsetY + ViewHeight / Zoom);

        // Half-cell lines first, so the main grid draws over them. They sit where snapping lands.
        if (Zoom >= FineGridZoom)
        {
            canvas.StrokeColor = Color.FromArgb("#D2CBB8");
            canvas.StrokeSize = 0.6f / Zoom;

            float fine = GridStep / 2;
            float fx = MathF.Max(0, MathF.Floor(OffsetX / fine) * fine);
            float fy = MathF.Max(0, MathF.Floor(OffsetY / fine) * fine);

            for (float x = fx; x <= right; x += fine)
                if (x % GridStep != 0) canvas.DrawLine(x, 0, x, CanvasSize);
            for (float y = fy; y <= bottom; y += fine)
                if (y % GridStep != 0) canvas.DrawLine(0, y, CanvasSize, y);
        }

        canvas.StrokeColor = Color.FromArgb("#C2BAA4");
        canvas.StrokeSize = 0.8f / Zoom;

        float startX = MathF.Max(0, MathF.Floor(OffsetX / step) * step);
        float startY = MathF.Max(0, MathF.Floor(OffsetY / step) * step);

        for (float x = startX; x <= right; x += step) canvas.DrawLine(x, 0, x, CanvasSize);
        for (float y = startY; y <= bottom; y += step) canvas.DrawLine(0, y, CanvasSize, y);
    }

    private static Color ColorOf(string name) => name switch
    {
        "Red" => Color.FromArgb("#d62828"),
        "Blue" => Color.FromArgb("#1d4ed8"),
        _ => Color.FromArgb("#1a1a1a")
    };

    private static void DrawShape(ICanvas canvas, DrawShape s)
    {
        var color = ColorOf(s.Color);
        canvas.StrokeColor = color;
        canvas.FillColor = color;
        canvas.StrokeSize = StrokeWidth;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        switch (s.Kind)
        {
            case ShapeKind.Pen:
                var pts = s.Points;
                if (pts == null || pts.Count == 0) return;
                if (pts.Count == 1) { canvas.FillCircle(pts[0].X, pts[0].Y, StrokeWidth); return; }
                var path = new PathF();
                path.MoveTo(pts[0].X, pts[0].Y);
                for (int i = 1; i < pts.Count; i++) path.LineTo(pts[i].X, pts[i].Y);
                canvas.DrawPath(path);
                break;

            case ShapeKind.Line:
                canvas.DrawLine(s.X1, s.Y1, s.X2, s.Y2);
                break;

            case ShapeKind.Rectangle:
                canvas.DrawRectangle(Math.Min(s.X1, s.X2), Math.Min(s.Y1, s.Y2), MathF.Abs(s.X2 - s.X1), MathF.Abs(s.Y2 - s.Y1));
                break;

            case ShapeKind.Circle:
                float radius = MathF.Sqrt((s.X2 - s.X1) * (s.X2 - s.X1) + (s.Y2 - s.Y1) * (s.Y2 - s.Y1));
                canvas.DrawCircle(s.X1, s.Y1, radius);
                break;

            case ShapeKind.Text:
                canvas.FontColor = color;
                canvas.FontSize = TextSize;
                canvas.DrawString(s.Text ?? string.Empty, s.X1, s.Y1, TextWidth(s), TextHeight,
                    HorizontalAlignment.Left, VerticalAlignment.Top, TextFlow.OverflowBounds);
                break;
        }
    }
}