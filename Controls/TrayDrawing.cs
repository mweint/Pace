using Avalonia.Media.Imaging;

namespace Pace;

public static class TrayDrawing
{
    public static RenderTargetBitmap Bitmap(List<Reading> readings, DateTimeOffset now, int size = 32)
    {
        var lines = new TrayLines(readings, now);
        lines.Measure(new Size(size, size)); lines.Arrange(new Rect(0, 0, size, size));
        var bitmap = new RenderTargetBitmap(new PixelSize(size, size), new Vector(UiMetrics.BaseDpi, UiMetrics.BaseDpi));
        bitmap.Render(lines);
        return bitmap;
    }
    public static byte[] Png(List<Reading> readings, int size = 32)
    {
        using var bitmap = Bitmap(readings, DateTimeOffset.UtcNow, size);
        using var stream = new MemoryStream(); bitmap.Save(stream, PngBitmapEncoderOptions.Default); return stream.ToArray();
    }
    sealed class TrayLines(List<Reading> readings, DateTimeOffset now) : Control
    {
        public override void Render(DrawingContext context)
        {
            int size = (int)Bounds.Width, count = Math.Clamp(readings.Count, 1, AccountRules.MaxTrayAccounts);
            // Bars share the icon height, so one or two accounts read as solid bars rather than a thin dash.
            int gap = Math.Max(2, (int)Math.Round(size / 8d)), height = size - 2 * (int)Math.Round(size / 16d);
            int thickness = Math.Min((height - (count - 1) * gap) / count, (int)Math.Round(size * .3125));
            int spacing = thickness + gap;
            int first = (size - ((count - 1) * spacing + thickness)) / 2;
            int left = (int)Math.Round(size * .125);
            double corner = Math.Max(1, size / 16d);
            for (int i = 0; i < count; i++)
            {
                var reading = i < readings.Count ? readings[i] : null;
                var state = reading?.Weekly is { } weekly && reading.Error == null ? PaceMath.Classify(weekly, now) : PaceState.Unavailable;
                context.DrawRectangle(Palette.Brush(state == PaceState.OnPace ? Palette.TrayOnPace : Palette.Status(state)), null, new Rect(left, first + i * spacing, size - 2 * left, thickness), corner, corner);
            }
        }
    }
}
