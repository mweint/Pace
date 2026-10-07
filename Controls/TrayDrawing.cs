using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Usage;

public static class TrayDrawing
{
    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr handle);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string className, string? title);
    [DllImport("user32.dll")]
    static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")]
    static extern int GetSystemMetricsForDpi(int index, uint dpi);
    public static Bitmap Bitmap(List<Reading> readings, DateTimeOffset now, int size = 32)
    {
        var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.None;
        int count = Math.Clamp(readings.Count, 1, AccountRules.MaxTrayAccounts);
        int thickness = Math.Max(2, (int)Math.Round(size / 8d));
        int spacing = (int)Math.Round(size * (count >= 4 ? 0.25 : 0.3125));
        int firstY = (size - ((count - 1) * spacing + thickness)) / 2;
        int lineLeft = (int)Math.Round(size * 0.125), lineRight = size - lineLeft;
        for (int i = 0; i < count; i++)
        {
            var r = i < readings.Count ? readings[i] : null;
            var pace = r?.Weekly is { } w && r.Error == null ? PaceMath.Calculate(w, now) : null;
            int y = firstY + i * spacing;
            var state = PaceMath.Classify(pace);
            using var ink = new SolidBrush(state == PaceState.OnPace ? Palette.TrayOnPace : Palette.Status(pace));
            g.FillRectangle(ink, lineLeft, y, lineRight - lineLeft, thickness);
        }

        return bitmap;
    }

    public static Icon Icon(List<Reading> readings)
    {
        uint dpi = GetDpiForWindow(FindWindow("Shell_TrayWnd", null));
        int size = GetSystemMetricsForDpi(49 /* SM_CXSMICON */, dpi == 0 ? 96u : dpi);
        using var bmp = Bitmap(readings, DateTimeOffset.UtcNow, Math.Max(16, size));
        var handle = bmp.GetHicon();
        try
        {
            using var temporary = System.Drawing.Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
