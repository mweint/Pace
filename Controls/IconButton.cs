using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Usage;

public sealed class IconButton : Button
{
    readonly Bitmap? artwork;
    readonly bool settingsArtwork;
    public bool Notification { get; set; }
    System.Windows.Forms.Timer? visibilityTimer;
    double artworkOpacity = 1, fadeFrom, fadeTo;
    long fadeStarted;

    public void FadeVisible(bool show)
    {
        if (visibilityTimer is { Enabled: true } && fadeTo == (show ? 1 : 0))
            return;
        if (visibilityTimer?.Enabled != true && Visible == show)
            return;
        Enabled = TabStop = show;
        if (!Motion.Enabled || !IsHandleCreated)
        {
            visibilityTimer?.Stop();
            artworkOpacity = show ? 1 : 0;
            Visible = show;
            Invalidate();
            return;
        }
        if (!Visible)
            artworkOpacity = 0;
        Visible = true;
        fadeFrom = artworkOpacity;
        fadeTo = show ? 1 : 0;
        fadeStarted = Environment.TickCount64;
        if (visibilityTimer == null)
        {
            visibilityTimer = new() { Interval = Motion.FrameMilliseconds };
            visibilityTimer.Tick += (_, _) =>
            {
                double progress = Math.Clamp((Environment.TickCount64 - fadeStarted) / Motion.IconFadeMilliseconds, 0, 1);
                artworkOpacity = fadeFrom + (fadeTo - fadeFrom) * Motion.IconEase(progress);
                Invalidate();
                if (progress < 1)
                    return;
                visibilityTimer.Stop();
                Visible = fadeTo == 1;
            };
        }
        visibilityTimer.Start();
    }
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
        string? filename = kind switch
        {
            "refresh" => "refresh-cw",
            "accounts" => "users",
            "settings" => null,
            "pin" => "pin",
            "close" => "x",
            "delete" => "trash-2",
            "back" => "arrow-left",
            "drag" => "grip-vertical",
            "confirm" => "check",
            _ => throw new ArgumentException("Unknown icon", nameof(kind))
        };
        artwork = filename == null ? null : LoadArtwork(filename);
        settingsArtwork = kind == "settings";
        AccessibleName = label;
        Text = "";
        Font = Palette.BodyFont();
        Width = Height = UiMetrics.IconButtonSize;
        Margin = new Padding(0, 0, UiMetrics.InlineGap, 0);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Palette.Background;
        Cursor = Cursors.Hand;
        FlatAppearance.MouseOverBackColor = Palette.InteractionSurface;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        // Clear the native Button focus/default outline before drawing our themed surface.
        bool hovered = ClientRectangle.Contains(PointToClient(Cursor.Position));
        g.Clear(Selected || hovered ? Palette.InteractionSurface : BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.ScaleTransform(Width / (float)UiMetrics.IconButtonSize, Height / (float)UiMetrics.IconButtonSize);
        if (Selected)
        {
            using var background = new SolidBrush(Palette.InteractionSurface);
            g.FillRectangle(background, 0, 0, UiMetrics.IconButtonSize, UiMetrics.IconButtonSize);
        }

        // Official Lucide SVGs rasterized at 96px during development, then tinted here.
        Color ink = Palette.IconInk(Enabled || visibilityTimer?.Enabled == true, Selected);
        if (settingsArtwork)
            SettingsMark.Draw(g, ink);
        else
            DrawArtwork(g, artwork!, new Rectangle((UiMetrics.IconButtonSize - UiMetrics.IconSize) / 2, (UiMetrics.IconButtonSize - UiMetrics.IconSize) / 2, UiMetrics.IconSize, UiMetrics.IconSize), ink, (float)artworkOpacity);
        if (Notification)
        {
            using var dot = new SolidBrush(Palette.Warning);
            g.FillEllipse(dot, UiMetrics.IconButtonSize - UiMetrics.WarningDotSize - UiMetrics.FocusInset, UiMetrics.FocusInset, UiMetrics.WarningDotSize, UiMetrics.WarningDotSize);
        }
        if (Focused && ShowFocusCues && !SuppressFocusOutline && Enabled)
        {
            using var edge = new Pen(Palette.FocusBorder, UiMetrics.BorderWidth);
            g.DrawRectangle(edge, UiMetrics.FocusInset, UiMetrics.FocusInset,
                UiMetrics.IconButtonSize - 2 * UiMetrics.FocusInset - UiMetrics.BorderWidth,
                UiMetrics.IconButtonSize - 2 * UiMetrics.FocusInset - UiMetrics.BorderWidth);
        }
    }

    internal static Bitmap LoadArtwork(string filename)
    {
        using var resource = typeof(IconButton).Assembly.GetManifestResourceStream($"Usage.Assets.Icons.{filename}.png") ?? throw new InvalidOperationException($"Missing icon: {filename}");
        using var decoded = new Bitmap(resource);
        return new Bitmap(decoded);
    }

    internal static void DrawArtwork(Graphics g, Bitmap artwork, Rectangle bounds, Color ink, float opacity = 1)
    {
        using var tint = new ImageAttributes();
        tint.SetColorMatrix(new ColorMatrix { Matrix00 = ink.R / 255f, Matrix11 = ink.G / 255f, Matrix22 = ink.B / 255f, Matrix33 = opacity });
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(artwork, bounds, 0, 0, artwork.Width, artwork.Height, GraphicsUnit.Pixel, tint);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            artwork?.Dispose();
            visibilityTimer?.Dispose();
        }

        base.Dispose(disposing);
    }
}
