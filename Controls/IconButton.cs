using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Usage;

public sealed class IconButton : Button
{
    readonly Bitmap artwork;
    public bool Selected
    {
        get; set;
    }
    public bool SuppressFocusOutline
    {
        get; set;
    }

    public IconButton(string kind, string label)
    {
        string filename = kind switch
        {
            "refresh" => "refresh-cw",
            "accounts" => "users",
            "pin" => "pin",
            "close" => "x",
            "back" => "arrow-left",
            "drag" => "grip-vertical",
            _ => throw new ArgumentException("Unknown icon", nameof(kind))
        };
        artwork = LoadArtwork(filename);
        AccessibleName = label;
        Text = "";
        Width = Height = UiMetrics.IconButtonSize;
        Margin = new Padding(0, 0, 4, 0);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Palette.Background;
        Cursor = Cursors.Hand;
        FlatAppearance.MouseOverBackColor = Palette.Card;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        // Clear the native Button focus/default outline before drawing our themed surface.
        bool hovered = ClientRectangle.Contains(PointToClient(Cursor.Position));
        g.Clear(Selected || hovered ? Palette.Card : BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.ScaleTransform(Width / (float)UiMetrics.IconButtonSize, Height / (float)UiMetrics.IconButtonSize);
        if (Selected)
        {
            using var background = new SolidBrush(Palette.Card);
            g.FillRectangle(background, 0, 0, UiMetrics.IconButtonSize, UiMetrics.IconButtonSize);
        }

        // Official Lucide SVGs rasterized at 96px during development, then tinted here.
        Color ink = Selected ? Palette.OnPace : Enabled ? Palette.Muted : Palette.Disabled;
        DrawArtwork(g, artwork, new Rectangle((UiMetrics.IconButtonSize - UiMetrics.IconSize) / 2, (UiMetrics.IconButtonSize - UiMetrics.IconSize) / 2, UiMetrics.IconSize, UiMetrics.IconSize), ink);
        if (Focused && ShowFocusCues && !SuppressFocusOutline)
        {
            using var edge = new Pen(Palette.Muted);
            g.DrawRectangle(edge, 3, 3, 23, 23);
        }
    }

    internal static Bitmap LoadArtwork(string filename)
    {
        using var resource = typeof(IconButton).Assembly.GetManifestResourceStream($"Usage.Assets.Icons.{filename}.png") ?? throw new InvalidOperationException($"Missing icon: {filename}");
        using var decoded = new Bitmap(resource);
        return new Bitmap(decoded);
    }

    internal static void DrawArtwork(Graphics g, Bitmap artwork, Rectangle bounds, Color ink)
    {
        using var tint = new ImageAttributes();
        tint.SetColorMatrix(new ColorMatrix { Matrix00 = ink.R / 255f, Matrix11 = ink.G / 255f, Matrix22 = ink.B / 255f });
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(artwork, bounds, 0, 0, artwork.Width, artwork.Height, GraphicsUnit.Pixel, tint);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            artwork.Dispose();
        }

        base.Dispose(disposing);
    }
}
