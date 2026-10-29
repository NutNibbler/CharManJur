using CharManJur.Models;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace CharManJur.Helpers;

public enum MapTool { Hand, Select, Pen, Line, Rectangle, Circle, Text, Eraser }

public static class MapTokenColors
{
    public static readonly string[] Names = { "Red", "Blue", "Green", "Yellow", "Purple", "Orange", "Black", "White" };

    public static Color Get(string? name) => name switch
    {
        "Red" => Color.FromArgb("#d62828"),
        "Blue" => Color.FromArgb("#1d4ed8"),
        "Green" => Color.FromArgb("#2e7d32"),
        "Yellow" => Color.FromArgb("#f2c200"),
        "Purple" => Color.FromArgb("#7b2cbf"),
        "Orange" => Color.FromArgb("#e67e22"),
        "White" => Color.FromArgb("#f5f5f5"),
        _ => Color.FromArgb("#1a1a1a")
    };

    // Dark letters on light tokens, white letters on the rest.
    public static Color TextOn(string? name) => name is "Yellow" or "White" ? Color.FromArgb("#1a1a1a") : Colors.White;
}

// The map's surface: the picture, drawing tools, grid, zoom and pan, and the symbols placed on it.
// The canvas is the size of the picture (3000 x 3000 with none). Shapes and symbols live in lists shared with the map's data.
public class MapBoard : IDrawable
{
    public const float DefaultCanvas = 3000f;
    public const float GridStep = 50f;
    private const float SnapStep = GridStep / 2;
    private const float MinZoom = 0.05f, MaxZoom = 4f;
    private const int MaxUndo = 50;

    private sealed record Snapshot(List<DrawShape> Shapes, List<MapSymbol> Symbols);

    private List<DrawShape> _shapes = new();
    private List<MapSymbol> _symbols = new();
    private readonly Dictionary<Guid, SymbolDef> _defs = new();
    private readonly Dictionary<Guid, IImage?> _images = new();
    private readonly List<Snapshot> _undo = new();
    private readonly List<Snapshot> _redo = new();

    private DrawShape? _active;
    private bool _panning, _pinching, _erased, _ignore, _midPanning, _fitPending = true;
    private PointF _lastScreen, _lastMid, _midLast;
    private float _lastDist;
    private PointF? _hover;
    private DrawPoint? _hoverSnapped;
    private IImage? _background;

    private MapSymbol? _dragSymbol;
    private bool _dragMoved;
    private float _grabDx, _grabDy;
    private Snapshot? _dragSnapshot;

    public MapTool Tool { get; set; } = MapTool.Select;
    public string ColorName { get; set; } = "Black";
    public bool SnapToGrid { get; set; }
    public bool ShowGrid { get; set; }

    public float CanvasWidth { get; private set; } = DefaultCanvas;
    public float CanvasHeight { get; private set; } = DefaultCanvas;
    public float Zoom { get; private set; } = 1f;
    public float OffsetX { get; private set; }
    public float OffsetY { get; private set; }
    public float ViewWidth { get; private set; }
    public float ViewHeight { get; private set; }

    public MapSymbol? Selected { get; private set; }
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public event Action? Redraw;                 // the picture or the toolbar state changed
    public event Action? Changed;                // the map's shapes or symbols changed (save them)
    public event Action? SelectionChanged;
    public event Action? ViewFitted;             // the first draw (or a new picture) fitted the map into the view
    public event Action<DrawPoint>? TextRequested;

    // Strokes and text scale with the map, so they stay readable on a 3000 px canvas.
    private float K => MathF.Max(CanvasWidth, CanvasHeight) / 2000f;

    public void Refresh() => Redraw?.Invoke();

    // ===== CONTENT =====
    // Shows a map's shapes and symbols (the lists are edited in place, so the map saves what is drawn).
    public void Load(MapData data)
    {
        _shapes = data.Shapes;
        _symbols = data.Symbols;
        _undo.Clear();
        _redo.Clear();
        _active = null;
        _dragSymbol = null;
        _dragSnapshot = null;
        _panning = _pinching = false;
        SetSelected(null);
        _fitPending = true;
        Redraw?.Invoke();
    }

    // The map's picture (null = none). The canvas becomes the picture's size.
    public void SetBackground(IImage? image)
    {
        _background = image;
        CanvasWidth = image != null ? image.Width : DefaultCanvas;
        CanvasHeight = image != null ? image.Height : DefaultCanvas;
        _fitPending = true;
        Redraw?.Invoke();
    }

    // The symbol library, so placed symbols can be drawn. Images already loaded are kept.
    public void SetSymbols(IEnumerable<SymbolDef> defs, Func<SymbolDef, IImage?> loadImage)
    {
        _defs.Clear();
        foreach (var def in defs)
        {
            _defs[def.Id] = def;
            if (def.Kind == SymbolKind.Image && !_images.ContainsKey(def.Id)) _images[def.Id] = loadImage(def);
        }
        Redraw?.Invoke();
    }

    // ===== SYMBOLS =====
    // Puts a library symbol in the middle of what is on screen, selected and ready to drag.
    public MapSymbol? PlaceSymbolAtCenter(Guid symbolId)
    {
        if (!_defs.ContainsKey(symbolId)) return null;

        float visW = ViewWidth > 0 ? ViewWidth / Zoom : CanvasWidth;
        float visH = ViewHeight > 0 ? ViewHeight / Zoom : CanvasHeight;
        float cx = Math.Clamp(OffsetX + visW / 2, 0, CanvasWidth);
        float cy = Math.Clamp(OffsetY + visH / 2, 0, CanvasHeight);
        if (SnapToGrid)
        {
            cx = MathF.Round(cx / SnapStep) * SnapStep;
            cy = MathF.Round(cy / SnapStep) * SnapStep;
        }

        var symbol = new MapSymbol
        {
            SymbolId = symbolId,
            X = cx,
            Y = cy,
            Size = MathF.Max(CanvasWidth, CanvasHeight) / 30f
        };

        PushUndo();
        _symbols.Add(symbol);
        _redo.Clear();
        SetSelected(symbol);
        Tool = MapTool.Select;
        Changed?.Invoke();
        Redraw?.Invoke();
        return symbol;
    }

    public void RotateSelected(float degrees)
    {
        if (Selected == null) return;
        PushUndo();
        Selected.Rotation = (((Selected.Rotation + degrees) % 360f) + 360f) % 360f;
        _redo.Clear();
        Changed?.Invoke();
        Redraw?.Invoke();
    }

    public void ScaleSelected(float factor)
    {
        if (Selected == null) return;
        float longSide = MathF.Max(CanvasWidth, CanvasHeight);

        PushUndo();
        Selected.Size = Math.Clamp(Selected.Size * factor, longSide / 200f, longSide / 3f);
        _redo.Clear();
        Changed?.Invoke();
        Redraw?.Invoke();
    }

    public void BringSelectedToFront()
    {
        if (Selected == null || _symbols.Count == 0 || ReferenceEquals(_symbols[^1], Selected)) return;
        PushUndo();
        _symbols.Remove(Selected);
        _symbols.Add(Selected);
        _redo.Clear();
        Changed?.Invoke();
        Redraw?.Invoke();
    }

    public void DuplicateSelected()
    {
        if (Selected == null) return;

        var copy = Selected.Clone();
        copy.Id = Guid.NewGuid();
        copy.X = Math.Clamp(copy.X + copy.Size * 0.6f, 0, CanvasWidth);
        copy.Y = Math.Clamp(copy.Y + copy.Size * 0.6f, 0, CanvasHeight);

        PushUndo();
        _symbols.Add(copy);
        _redo.Clear();
        SetSelected(copy);
        Changed?.Invoke();
        Redraw?.Invoke();
    }

    public void DeleteSelected()
    {
        if (Selected == null) return;
        PushUndo();
        _symbols.Remove(Selected);
        _redo.Clear();
        SetSelected(null);
        Changed?.Invoke();
        Redraw?.Invoke();
    }

    private void SetSelected(MapSymbol? symbol)
    {
        if (ReferenceEquals(Selected, symbol)) return;
        Selected = symbol;
        SelectionChanged?.Invoke();
    }

    private MapSymbol? HitSymbol(float wx, float wy)
    {
        for (int i = _symbols.Count - 1; i >= 0; i--)   // the top one wins
        {
            var s = _symbols[i];
            float half = MathF.Max(s.Size / 2, 14f / Zoom);   // small symbols stay easy to grab
            float dx = wx - s.X, dy = wy - s.Y;
            float rad = s.Rotation * MathF.PI / 180f;
            float cos = MathF.Cos(rad), sin = MathF.Sin(rad);
            float lx = dx * cos + dy * sin;
            float ly = -dx * sin + dy * cos;
            if (MathF.Abs(lx) <= half && MathF.Abs(ly) <= half) return s;
        }
        return null;
    }

    // A symbol drag in progress is dropped and the symbol goes back where it was.
    private void CancelSymbolDrag()
    {
        if (_dragSymbol != null && _dragSnapshot != null)
        {
            var original = _dragSnapshot.Symbols.FirstOrDefault(s => s.Id == _dragSymbol.Id);
            if (original != null)
            {
                _dragSymbol.X = original.X;
                _dragSymbol.Y = original.Y;
            }
        }
        _dragSymbol = null;
        _dragSnapshot = null;
        _dragMoved = false;
    }

    // ===== HOVER / DESKTOP =====
    // The mouse moved over the surface without drawing (null = it left). Redraws only when the snap point changes.
    public void Hover(PointF? p)
    {
        _hover = p;

        bool wanted = SnapToGrid && Tool != MapTool.Hand && Tool != MapTool.Select && Tool != MapTool.Eraser && p.HasValue;
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
        CancelSymbolDrag();
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

    // ===== INPUT (screen points from the view) =====
    public void Begin(PointF[] touches)
    {
        if (touches.Length == 0) return;
        if (touches.Length >= 2) { StartPinch(touches); return; }
        _ignore = false;

        var p = touches[0];
        _hover = p;

        if (Tool == MapTool.Hand) { _panning = true; _lastScreen = p; return; }

        if (Tool == MapTool.Select)
        {
            var (wx, wy) = WorldPoint(p);
            var hit = HitSymbol(wx, wy);
            if (hit != null)
            {
                SetSelected(hit);
                _dragSymbol = hit;
                _dragMoved = false;
                _grabDx = hit.X - wx;
                _grabDy = hit.Y - wy;
                _dragSnapshot = TakeSnapshot();
            }
            else
            {
                SetSelected(null);
                _panning = true;      // empty space drags the map
                _lastScreen = p;
            }
            Redraw?.Invoke();
            return;
        }

        switch (Tool)
        {
            case MapTool.Pen:
                var pen = ToWorld(p, true);
                _active = New(ShapeKind.Pen);
                _active.Points = new List<DrawPoint> { pen };
                break;
            case MapTool.Line:
            case MapTool.Rectangle:
            case MapTool.Circle:
                var a = ToWorld(p, true);
                _active = New(Tool == MapTool.Line ? ShapeKind.Line : Tool == MapTool.Rectangle ? ShapeKind.Rectangle : ShapeKind.Circle);
                _active.X1 = _active.X2 = a.X;
                _active.Y1 = _active.Y2 = a.Y;
                break;
            case MapTool.Text:
                TextRequested?.Invoke(ToWorld(p, true));
                break;
            case MapTool.Eraser:
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

        if (_dragSymbol != null)
        {
            var (wx, wy) = WorldPoint(p);
            float nx = wx + _grabDx, ny = wy + _grabDy;
            if (SnapToGrid)
            {
                nx = MathF.Round(nx / SnapStep) * SnapStep;
                ny = MathF.Round(ny / SnapStep) * SnapStep;
            }
            _dragSymbol.X = Math.Clamp(nx, 0, CanvasWidth);
            _dragSymbol.Y = Math.Clamp(ny, 0, CanvasHeight);
            _dragMoved = true;
            Redraw?.Invoke();
            return;
        }

        if (_panning)
        {
            Pan(p.X - _lastScreen.X, p.Y - _lastScreen.Y);
            _lastScreen = p;
            Redraw?.Invoke();
            return;
        }

        if (Tool == MapTool.Eraser) { EraseAt(ToWorld(p, false)); return; }
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
            _dragSymbol = null;
            _dragSnapshot = null;
            _dragMoved = false;
            _panning = _pinching = false;
            Redraw?.Invoke();
            return;
        }

        if (_dragSymbol != null)
        {
            if (_dragMoved && _dragSnapshot != null)
            {
                PushUndo(_dragSnapshot);
                _redo.Clear();
                Changed?.Invoke();
            }
            _dragSymbol = null;
            _dragSnapshot = null;
            _dragMoved = false;
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
        _redo.Add(TakeSnapshot());
        Restore(_undo[^1]);
        _undo.RemoveAt(_undo.Count - 1);
        Changed?.Invoke();
        Redraw?.Invoke();
    }

    public void RedoLast()
    {
        if (_redo.Count == 0) return;
        _undo.Add(TakeSnapshot());
        Restore(_redo[^1]);
        _redo.RemoveAt(_redo.Count - 1);
        Changed?.Invoke();
        Redraw?.Invoke();
    }

    private Snapshot TakeSnapshot()
        => new(new List<DrawShape>(_shapes), _symbols.Select(s => s.Clone()).ToList());

    private void PushUndo() => PushUndo(TakeSnapshot());

    private void PushUndo(Snapshot snapshot)
    {
        _undo.Add(snapshot);
        if (_undo.Count > MaxUndo) _undo.RemoveAt(0);
    }

    private void Restore(Snapshot snapshot)
    {
        var selectedId = Selected?.Id;

        _shapes.Clear();
        _shapes.AddRange(snapshot.Shapes);
        _symbols.Clear();
        _symbols.AddRange(snapshot.Symbols.Select(s => s.Clone()));

        SetSelected(selectedId == null ? null : _symbols.FirstOrDefault(s => s.Id == selectedId));
    }

    // ===== VIEW =====
    public void ZoomBy(float factor)
    {
        ZoomAt(new PointF(ViewWidth / 2, ViewHeight / 2), factor);
        Redraw?.Invoke();
    }

    // Fits the whole map into the view.
    public void ResetView()
    {
        _fitPending = true;
        Redraw?.Invoke();
    }

    private void Fit()
    {
        if (ViewWidth <= 0 || ViewHeight <= 0) return;
        Zoom = Math.Clamp(MathF.Min(ViewWidth / CanvasWidth, ViewHeight / CanvasHeight), MinZoom, MaxZoom);
        OffsetX = OffsetY = 0;
        ClampOffset();   // centers whichever side is smaller than the view
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

    // The map can be dragged a little past its edges; when the whole map is smaller than the view, it stays centered.
    private void ClampOffset()
    {
        float visW = ViewWidth / Zoom, visH = ViewHeight / Zoom;

        if (CanvasWidth <= visW) OffsetX = (CanvasWidth - visW) / 2;
        else OffsetX = Math.Clamp(OffsetX, -50f, CanvasWidth - visW + 50f);

        if (CanvasHeight <= visH) OffsetY = (CanvasHeight - visH) / 2;
        else OffsetY = Math.Clamp(OffsetY, -50f, CanvasHeight - visH + 50f);
    }

    private void StartPinch(PointF[] t)
    {
        CancelSymbolDrag();
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

    private (float X, float Y) WorldPoint(PointF screen)
        => (Math.Clamp(screen.X / Zoom + OffsetX, 0, CanvasWidth), Math.Clamp(screen.Y / Zoom + OffsetY, 0, CanvasHeight));

    private DrawPoint ToWorld(PointF screen, bool snap)
    {
        var (x, y) = WorldPoint(screen);
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

    // ===== ERASER (drawn shapes only; symbols are removed with their own Delete button) =====
    private void EraseAt(DrawPoint p)
    {
        float tolerance = 8f / Zoom;
        if (!_shapes.Any(s => Hit(s, p, tolerance))) return;

        if (!_erased) { PushUndo(); _redo.Clear(); _erased = true; }   // one undo step per stroke
        _shapes.RemoveAll(s => Hit(s, p, tolerance));
        Changed?.Invoke();
        Redraw?.Invoke();
    }

    private bool Hit(DrawShape s, DrawPoint p, float tol)
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
                float l = MathF.Min(s.X1, s.X2), r = MathF.Max(s.X1, s.X2), t = MathF.Min(s.Y1, s.Y2), b = MathF.Max(s.Y1, s.Y2);
                return DistSeg(p.X, p.Y, l, t, r, t) <= tol || DistSeg(p.X, p.Y, r, t, r, b) <= tol
                    || DistSeg(p.X, p.Y, r, b, l, b) <= tol || DistSeg(p.X, p.Y, l, b, l, t) <= tol;

            case ShapeKind.Circle:
                float radius = MathF.Sqrt((s.X2 - s.X1) * (s.X2 - s.X1) + (s.Y2 - s.Y1) * (s.Y2 - s.Y1));
                float away = MathF.Sqrt((p.X - s.X1) * (p.X - s.X1) + (p.Y - s.Y1) * (p.Y - s.Y1));
                return MathF.Abs(away - radius) <= tol;

            case ShapeKind.Text:
                return p.X >= s.X1 - tol && p.X <= s.X1 + TextWidth(s) + tol && p.Y >= s.Y1 - tol && p.Y <= s.Y1 + 26f * K + tol;
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

    private float TextWidth(DrawShape s) => MathF.Max(20f * K, (s.Text?.Length ?? 0) * 11f * K);

    // ===== PAINTING =====
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        ViewWidth = dirtyRect.Width;
        ViewHeight = dirtyRect.Height;

        if (_fitPending && ViewWidth > 0)
        {
            _fitPending = false;
            Fit();
            ViewFitted?.Invoke();
        }

        canvas.SaveState();
        canvas.Scale(Zoom, Zoom);
        canvas.Translate(-OffsetX, -OffsetY);

        canvas.FillColor = Color.FromArgb("#F7F4EC");
        canvas.FillRectangle(0, 0, CanvasWidth, CanvasHeight);

        if (_background != null) canvas.DrawImage(_background, 0, 0, CanvasWidth, CanvasHeight);

        if (ShowGrid) DrawGrid(canvas);

        canvas.StrokeColor = Color.FromArgb("#888888");
        canvas.StrokeSize = 2f / Zoom;
        canvas.DrawRectangle(0, 0, CanvasWidth, CanvasHeight);

        foreach (var shape in _shapes) DrawShapeOn(canvas, shape);
        if (_active != null) DrawShapeOn(canvas, _active);

        foreach (var symbol in _symbols) DrawSymbol(canvas, symbol, ReferenceEquals(symbol, Selected));

        if (SnapToGrid && _hover.HasValue && Tool != MapTool.Hand && Tool != MapTool.Select && Tool != MapTool.Eraser)
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
        float step = Zoom < 0.4f ? GridStep * 4 : Zoom < 0.8f ? GridStep * 2 : GridStep;   // keep the lines from crowding when zoomed out

        float right = MathF.Min(CanvasWidth, OffsetX + ViewWidth / Zoom);
        float bottom = MathF.Min(CanvasHeight, OffsetY + ViewHeight / Zoom);

        canvas.StrokeColor = Color.FromArgb("#66808080");
        canvas.StrokeSize = 1f / Zoom;

        float startX = MathF.Max(0, MathF.Floor(OffsetX / step) * step);
        float startY = MathF.Max(0, MathF.Floor(OffsetY / step) * step);

        for (float x = startX; x <= right; x += step) canvas.DrawLine(x, 0, x, CanvasHeight);
        for (float y = startY; y <= bottom; y += step) canvas.DrawLine(0, y, CanvasWidth, y);
    }

    private static Color ColorOf(string name) => name switch
    {
        "Red" => Color.FromArgb("#d62828"),
        "Blue" => Color.FromArgb("#1d4ed8"),
        _ => Color.FromArgb("#1a1a1a")
    };

    private void DrawShapeOn(ICanvas canvas, DrawShape s)
    {
        float k = K;
        float stroke = 2.5f * k;

        var color = ColorOf(s.Color);
        canvas.StrokeColor = color;
        canvas.FillColor = color;
        canvas.StrokeSize = stroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        switch (s.Kind)
        {
            case ShapeKind.Pen:
                var pts = s.Points;
                if (pts == null || pts.Count == 0) return;
                if (pts.Count == 1) { canvas.FillCircle(pts[0].X, pts[0].Y, stroke); return; }
                var path = new PathF();
                path.MoveTo(pts[0].X, pts[0].Y);
                for (int i = 1; i < pts.Count; i++) path.LineTo(pts[i].X, pts[i].Y);
                canvas.DrawPath(path);
                break;

            case ShapeKind.Line:
                canvas.DrawLine(s.X1, s.Y1, s.X2, s.Y2);
                break;

            case ShapeKind.Rectangle:
                canvas.DrawRectangle(MathF.Min(s.X1, s.X2), MathF.Min(s.Y1, s.Y2), MathF.Abs(s.X2 - s.X1), MathF.Abs(s.Y2 - s.Y1));
                break;

            case ShapeKind.Circle:
                float radius = MathF.Sqrt((s.X2 - s.X1) * (s.X2 - s.X1) + (s.Y2 - s.Y1) * (s.Y2 - s.Y1));
                canvas.DrawCircle(s.X1, s.Y1, radius);
                break;

            case ShapeKind.Text:
                canvas.FontColor = color;
                canvas.FontSize = 20f * k;
                canvas.DrawString(s.Text ?? string.Empty, s.X1, s.Y1, TextWidth(s), 26f * k,
                    HorizontalAlignment.Left, VerticalAlignment.Top, TextFlow.OverflowBounds);
                break;
        }
    }

    private void DrawSymbol(ICanvas canvas, MapSymbol s, bool selected)
    {
        canvas.SaveState();
        canvas.Translate(s.X, s.Y);
        canvas.Rotate(s.Rotation);

        float half = s.Size / 2;

        if (_defs.TryGetValue(s.SymbolId, out var def))
        {
            if (def.Kind == SymbolKind.Token)
            {
                var fill = MapTokenColors.Get(def.Color);
                canvas.FillColor = fill;
                canvas.FillCircle(0, 0, half);
                canvas.StrokeColor = Colors.Black.WithAlpha(0.8f);
                canvas.StrokeSize = MathF.Max(1f, s.Size * 0.06f);
                canvas.DrawCircle(0, 0, half);

                string letter = def.Letter ?? string.Empty;
                if (letter.Length > 0)
                {
                    canvas.FontColor = MapTokenColors.TextOn(def.Color);
                    canvas.FontSize = s.Size * (letter.Length == 1 ? 0.6f : letter.Length == 2 ? 0.45f : 0.35f);
                    canvas.DrawString(letter, -half, -half, s.Size, s.Size, HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }
            else if (_images.TryGetValue(def.Id, out var image) && image != null)
            {
                float fit = MathF.Min(s.Size / image.Width, s.Size / image.Height);
                float w = image.Width * fit, h = image.Height * fit;
                canvas.DrawImage(image, -w / 2, -h / 2, w, h);
            }
            else
            {
                DrawMissing(canvas, s, half);
            }
        }
        else
        {
            DrawMissing(canvas, s, half);
        }

        if (selected)
        {
            var gold = Color.FromArgb("#f1c40f");
            canvas.StrokeColor = gold;
            canvas.StrokeSize = 3f / Zoom;
            canvas.DrawRectangle(-half, -half, s.Size, s.Size);

            // A marker on the top edge shows which way the symbol points.
            float tip = half + s.Size * 0.35f;
            canvas.DrawLine(0, -half, 0, -tip);
            canvas.FillColor = gold;
            canvas.FillCircle(0, -tip, 5f / Zoom);
        }

        canvas.RestoreState();
    }

    private static void DrawMissing(ICanvas canvas, MapSymbol s, float half)
    {
        canvas.FillColor = Colors.Gray.WithAlpha(0.7f);
        canvas.FillRoundedRectangle(-half, -half, s.Size, s.Size, s.Size * 0.15f);
        canvas.FontColor = Colors.White;
        canvas.FontSize = s.Size * 0.6f;
        canvas.DrawString("?", -half, -half, s.Size, s.Size, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}