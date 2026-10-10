using Avalonia.Media.Imaging;

namespace Pace;

public static class TrayDrawing
{
    public static RenderTargetBitmap Bitmap(List<Reading> readings, DateTimeOffset now, int size = 32, bool anyLimit = false)
    {
        var lines = new TrayLines(readings, now, anyLimit);
        lines.Measure(new Size(size, size)); lines.Arrange(new Rect(0, 0, size, size));
        var bitmap = new RenderTargetBitmap(new PixelSize(size, size), new Vector(UiMetrics.BaseDpi, UiMetrics.BaseDpi));
        bitmap.Render(lines);
        return bitmap;
    }
    public static byte[] Png(List<Reading> readings, int size = 32, bool anyLimit = false)
    {
        using var bitmap = Bitmap(readings, DateTimeOffset.UtcNow, size, anyLimit);
        using var stream = new MemoryStream(); bitmap.Save(stream, PngBitmapEncoderOptions.Default); return stream.ToArray();
    }
    sealed class TrayLines(List<Reading> readings, DateTimeOffset now, bool anyLimit) : Control
    {
        public override void Render(DrawingContext context)
        {
            int size = (int)Bounds.Width, count = Math.Clamp(readings.Count, 1, AccountRules.MaxTrayAccounts);
            int thickness = Math.Max(2, (int)Math.Round(size / 8d));
            int spacing = (int)Math.Round(size * (count >= 4 ? .25 : .3125));
            int first = (size - ((count - 1) * spacing + thickness)) / 2;
            int left = (int)Math.Round(size * .125);
            for (int i = 0; i < count; i++)
            {
                var reading = i < readings.Count ? readings[i] : null;
                // A stale reading keeps its colour, matching the panel; grey means no reading at all.
                var state = reading?.Weekly is not { } weekly ? PaceState.Unavailable
                    : anyLimit ? PaceMath.Strictest(reading.Limits?.Select(l => l.Window).Append(weekly) ?? [weekly], now)
                    : PaceMath.Classify(weekly, now);
                var color = state switch { PaceState.OnPace => Palette.TrayOnPace, PaceState.Below => Palette.TrayBelow, _ => Palette.Status(state) };
                context.FillRectangle(Palette.Brush(color), new Rect(left, first + i * spacing, size - 2 * left, thickness));
            }
        }
    }
}
