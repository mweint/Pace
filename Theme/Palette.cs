using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Usage;

public static class Palette
{
    public static Icon ApplicationIcon { get; } = LoadApplicationIcon();
    static Icon LoadApplicationIcon()
    {
        using var stream = typeof(Palette).Assembly.GetManifestResourceStream("Usage.Assets.Icons.pace.ico")
            ?? throw new InvalidOperationException("Missing application icon");
        return new Icon(stream);
    }

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
    public static Color SectionBackground => Background;
    public static readonly Color InteractionSurface = Color.FromArgb(29, 33, 41);
    public static readonly Color Text = Color.FromArgb(238, 241, 246);
    public static readonly Color Muted = Color.FromArgb(154, 164, 180);
    public static Color Status(Pace? p) => Status(PaceMath.Classify(p));
    public static Color Status(Window window, DateTimeOffset now) => Status(PaceMath.Classify(window, now));
    public static Color Status(PaceState state) => state switch
    {
        PaceState.Exhausted => Warning,
        PaceState.Above => Above,
        PaceState.Below => Below,
        PaceState.OnPace => OnPace,
        _ => Muted
    };
    public static readonly Color Above = Color.FromArgb(255, 161, 104), Below = Color.FromArgb(111, 210, 178), OnPace = Color.FromArgb(154, 193, 255);
    public static readonly Color TrayOnPace = Color.FromArgb(79, 166, 255);
    public static readonly Color Footer = Color.FromArgb(26, 30, 37), Action = Color.FromArgb(40, 46, 57);
    public static readonly Color InputBorder = Color.FromArgb(51, 58, 70), WindowBorder = Color.FromArgb(61, 69, 83);
    public static readonly Color InputHoverBorder = Color.FromArgb(88, 98, 114);
    public static readonly Color Track = Color.FromArgb(57, 64, 77), BarDivider = Color.FromArgb(110, 20, 23, 29);
    public static readonly Color ToggleOff = Color.FromArgb(66, 74, 89), Claude = Color.FromArgb(224, 162, 124);
    public static readonly Color Disabled = Color.FromArgb(75, 83, 96), Hover = Color.FromArgb(47, 54, 66);
    public static Color Warning => Above;
    public static Color ScrollThumb => Muted;
    public static Color FocusBorder => Muted;
    public static Color SelectionBorder => OnPace;
    public static Color ButtonHover => Hover;
    public static Color ButtonPressed => Hover;
    public static Color ToggleOn => Below;
    public static Color ToggleTrack(bool enabled, bool selected) => !enabled ? Disabled : selected ? ToggleOn : ToggleOff;
    public static Color ToggleKnob(bool selected) => selected ? Background : Text;
    public static Color ControlText(bool enabled) => enabled ? Text : Muted;
    public static Color IconInk(bool enabled, bool selected) => selected ? OnPace : enabled ? Muted : Disabled;
    public static Color InputOutline(bool editing, bool active) => editing ? FocusBorder : active ? InputHoverBorder : InputBorder;
    public const float BodyPointSize = 9, AccountPointSize = 10.5f, BarPointSize = 10;
    static Font AppFont(string familyName, float size)
    {
        var family = AppFonts.Families.FirstOrDefault(f => f.Name == familyName);
        return family is null ? new Font(familyName, size) : new Font(family, size);
    }

    public static Font BodyFont() => AppFont(FontFamily, BodyPointSize);
    public static Font AccountFont() => AppFont(HeadingFontFamily, AccountPointSize);
    public static Font DetailLimitFont() => AppFont(HeadingFontFamily, BodyPointSize);
    public static Font BarFont() => AppFont(HeadingFontFamily, BarPointSize);
    public static Button ServiceButton(string service)
    {
        var button = Button("Add " + service);
        var image = ServiceMark.ButtonArtwork(service);
        button.Image = image;
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
        button.ImageAlign = ContentAlignment.MiddleCenter;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Disposed += (_, _) => image.Dispose();
        return button;
    }

    public static Button Button(string text, bool compact = false)
    {
        var b = new FilledButton
        {
            Text = text,
            AutoSize = false,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Text,
            BackColor = Action,
            Padding = compact ? Padding.Empty : new Padding(UiMetrics.TextButtonInset),
            Cursor = Cursors.Hand,
            Font = BodyFont()
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = ButtonHover;
        b.FlatAppearance.MouseDownBackColor = ButtonPressed;
        return b;
    }
}
