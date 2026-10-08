using System.Drawing.Drawing2D;

namespace Usage;

public static class ServiceMark
{
    static readonly Bitmap Claude = IconButton.LoadArtwork("claude");
    static readonly Bitmap Codex = IconButton.LoadArtwork("codex");
    static readonly Dictionary<int, Bitmap> SmallCodex = new[] { 16, 20, 24, 32 }
        .ToDictionary(size => size, size => IconButton.LoadArtwork($"codex-{size}"));

    public static Bitmap ButtonArtwork(string service)
    {
        var image = new Bitmap(UiMetrics.ServiceIconSize + UiMetrics.CardGap, UiMetrics.ServiceIconSize);
        using var graphics = Graphics.FromImage(image);
        Draw(graphics, service, 0, 0);
        return image;
    }

    public static void Draw(Graphics g, string service, float x, float y)
    {
        // Provider artwork retains its original color and proportions in every view.
        using var transform = g.Transform;
        float scale = transform.Elements[0];
        int pixels = (int)Math.Round(UiMetrics.ServiceIconSize * scale);
        var artwork = service == "Claude" ? Claude : SmallCodex.GetValueOrDefault(pixels, Codex);
        var state = g.Save();
        try
        {
            // Bitmap interpolation, rather than SmoothingMode, controls small-image quality.
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(artwork, new RectangleF(x, y, UiMetrics.ServiceIconSize, UiMetrics.ServiceIconSize));
        }
        finally
        {
            g.Restore(state);
        }
    }
}
