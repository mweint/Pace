using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Usage;

public static class Palette
{
    // Semantic colors and typography are owned here. Views choose a role, never RGB values.
    // Keep private fonts alive for GDI+ drawing; register with GDI for WinForms controls.
    static readonly System.Drawing.Text.PrivateFontCollection AppFonts = LoadAppFonts();
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    static extern int AddFontResourceEx(string path, uint flags, IntPtr reserved);
    static System.Drawing.Text.PrivateFontCollection LoadAppFonts()
    {
        var fonts = new System.Drawing.Text.PrivateFontCollection();
        foreach (string filename in new[]
        {
            "Inter-Regular.ttf",
            "Inter-SemiBold.ttf"
        }

        )
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", filename);
            if (!File.Exists(path))
                continue;
            fonts.AddFontFile(path);
            AddFontResourceEx(path, 0x10, IntPtr.Zero); // FR_PRIVATE: only this process.
        }

        return fonts;
    }

    public static string FontFamily { get; } = AppFonts.Families.Any(f => f.Name == "Pace Sans") ? "Pace Sans" : "Segoe UI";
    public static string HeadingFontFamily { get; } = AppFonts.Families.Any(f => f.Name == "Pace Sans SemiBold") ? "Pace Sans SemiBold" : FontFamily;

    public static readonly Color Background = Color.FromArgb(20, 23, 29);
    public static readonly Color Card = Color.FromArgb(29, 33, 41);
    public static readonly Color Text = Color.FromArgb(238, 241, 246);
    public static readonly Color Muted = Color.FromArgb(154, 164, 180);
    public static Color Status(Pace? p) => PaceMath.Classify(p) switch
    {
        PaceState.Above => Above,
        PaceState.Below => Below,
        PaceState.OnPace => OnPace,
        _ => Muted
    };
    public static readonly Color Above = Color.FromArgb(255, 161, 104), Below = Color.FromArgb(111, 210, 178), OnPace = Color.FromArgb(154, 193, 255);
    public static readonly Color TrayOnPace = Color.FromArgb(79, 166, 255);
    public static readonly Color Footer = Color.FromArgb(26, 30, 37), Action = Color.FromArgb(40, 46, 57);
    public static readonly Color InputBorder = Color.FromArgb(51, 58, 70), WindowBorder = Color.FromArgb(61, 69, 83);
    public static readonly Color Track = Color.FromArgb(57, 64, 77), DayDivider = Color.FromArgb(110, 20, 23, 29);
    public static readonly Color ToggleOff = Color.FromArgb(66, 74, 89), Claude = Color.FromArgb(224, 162, 124);
    public static readonly Color Disabled = Color.FromArgb(75, 83, 96), Hover = Color.FromArgb(47, 54, 66);
    public static readonly Color ResetWarning = Color.FromArgb(235, 192, 108);
    static Font AppFont(string familyName, float size)
    {
        var family = AppFonts.Families.FirstOrDefault(f => f.Name == familyName);
        return family is null ? new Font(familyName, size) : new Font(family, size);
    }

    public static Font BodyFont() => AppFont(FontFamily, 9);
    public static Font AccountFont() => AppFont(HeadingFontFamily, 10.5f);
    public static Font BarFont() => AppFont(HeadingFontFamily, 10);
    public static Button Button(string text, bool compact = false)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = false,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Text,
            BackColor = Action,
            Padding = compact ? Padding.Empty : new Padding(5),
            Cursor = Cursors.Hand,
            Font = BodyFont()
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Hover;
        return b;
    }
}
