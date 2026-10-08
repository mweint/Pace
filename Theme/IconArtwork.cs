using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;

namespace Pace;

// Tints the packaged white Lucide rasters with a theme color, preserving alpha.
internal static class IconArtwork
{
    static readonly Dictionary<(string, Color), WriteableBitmap> cache = new();
    public static void Draw(DrawingContext context, string name, Rect bounds, Color ink)
    {
        if (!cache.TryGetValue((name, ink), out var tinted))
        {
            using var source = new Bitmap(AssetLoader.Open(new Uri($"avares://Pace/Assets/Icons/{name}.png")));
            tinted = new WriteableBitmap(source.PixelSize, source.Dpi, PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            using var pixels = tinted.Lock();
            source.CopyPixels(pixels);
            var data = new byte[pixels.RowBytes * pixels.Size.Height];
            Marshal.Copy(pixels.Address, data, 0, data.Length);
            for (int y = 0; y < pixels.Size.Height; y++)
                for (int x = 0; x < pixels.Size.Width; x++)
                {
                    int offset = y * pixels.RowBytes + x * 4;
                    data[offset] = (byte)(data[offset] * ink.B / 255);
                    data[offset + 1] = (byte)(data[offset + 1] * ink.G / 255);
                    data[offset + 2] = (byte)(data[offset + 2] * ink.R / 255);
                }
            Marshal.Copy(data, 0, pixels.Address, data.Length);
            cache.Add((name, ink), tinted);
        }
        context.DrawImage(tinted, bounds);
    }
}
