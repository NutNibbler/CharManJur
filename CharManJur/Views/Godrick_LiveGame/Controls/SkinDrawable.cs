using CharManJur.Models;
using Microsoft.Maui.Graphics.Platform;
using System.Diagnostics;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace CharManJur.Controls;

// Decoded skin images, cached by path. A changed file (new write time) is reloaded automatically.
internal static class SkinImageCache
{
    private static readonly Dictionary<string, (DateTime Stamp, IImage Image)> Cache =
        new(StringComparer.OrdinalIgnoreCase);

    public static IImage? Load(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        try
        {
            var stamp = File.GetLastWriteTimeUtc(path);
            if (Cache.TryGetValue(path, out var cached) && cached.Stamp == stamp) return cached.Image;

            using var stream = File.OpenRead(path);
            IImage image = PlatformImage.FromStream(stream);
            if (image == null) return null;

            Cache[path] = (stamp, image);
            return image;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== SkinImageCache: could not decode '{path}': {ex.Message} ===");
            return null;
        }
    }
}

// Draws a skin image stretched to the view with nine-slice scaling, clipped to a rounded rectangle.
// Corners keep their size, edges stretch along one axis, the center stretches both ways.
internal sealed class SkinDrawable : IDrawable
{
    private const float Bleed = 0.5f;   // slices overlap by half a pixel so no hairline seams show

    public IImage? Image { get; set; }
    public NineSliceMargins Margins { get; set; } = NineSliceMargins.Uniform(SkinPack.DefaultNineSlice);
    public float CornerRadius { get; set; } = 8;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var image = Image;
        if (image == null) return;

        float w = dirtyRect.Width;
        float h = dirtyRect.Height;
        float iw = image.Width;
        float ih = image.Height;
        if (w <= 0 || h <= 0 || iw <= 0 || ih <= 0) return;

        // Source margins, kept inside the image
        float l = Math.Clamp((float)Margins.Left, 0, iw / 2);
        float r = Math.Clamp((float)Margins.Right, 0, iw / 2);
        float t = Math.Clamp((float)Margins.Top, 0, ih / 2);
        float b = Math.Clamp((float)Margins.Bottom, 0, ih / 2);

        // On a button smaller than the corners, shrink the corners (all four by the same factor)
        float scaleX = (l + r) > 0 ? w / (l + r) : 1f;
        float scaleY = (t + b) > 0 ? h / (t + b) : 1f;
        float s = Math.Min(1f, Math.Min(scaleX, scaleY));
        float dl = l * s, dr = r * s, dt = t * s, db = b * s;

        // Column / row boundaries in the source image and on screen
        float[] sx = { 0, l, iw - r, iw };
        float[] sy = { 0, t, ih - b, ih };
        float[] dx = { 0, dl, w - dr, w };
        float[] dy = { 0, dt, h - db, h };

        canvas.SaveState();

        var clip = new PathF();
        clip.AppendRoundedRectangle(0, 0, w, h, Math.Min(CornerRadius, Math.Min(w, h) / 2));
        canvas.ClipPath(clip);

        // Center first, then edges, then corners (so the overlaps hide under the sharper corner art)
        int[][] order =
        {
            new[] { 1, 1 },
            new[] { 1, 0 }, new[] { 1, 2 }, new[] { 0, 1 }, new[] { 2, 1 },
            new[] { 0, 0 }, new[] { 2, 0 }, new[] { 0, 2 }, new[] { 2, 2 }
        };

        foreach (var cell in order)
        {
            int c = cell[0], row = cell[1];
            DrawSlice(canvas, image,
                sx[c], sy[row], sx[c + 1] - sx[c], sy[row + 1] - sy[row],
                dx[c], dy[row], dx[c + 1] - dx[c], dy[row + 1] - dy[row]);
        }

        canvas.RestoreState();
    }

    // Draws source rect (sx,sy,sw,sh) of the image into destination rect (dx,dy,dw,dh)
    private static void DrawSlice(ICanvas canvas, IImage image,
        float sx, float sy, float sw, float sh,
        float dx, float dy, float dw, float dh)
    {
        if (sw <= 0 || sh <= 0 || dw <= 0 || dh <= 0) return;

        float fx = dw / sw;
        float fy = dh / sh;

        canvas.SaveState();
        canvas.ClipRectangle(dx - Bleed, dy - Bleed, dw + Bleed * 2, dh + Bleed * 2);
        canvas.DrawImage(image, dx - sx * fx, dy - sy * fy, image.Width * fx, image.Height * fy);
        canvas.RestoreState();
    }
}