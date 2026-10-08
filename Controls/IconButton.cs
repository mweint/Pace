namespace Pace;

public sealed class IconButton : Button
{
    readonly string artwork;
    readonly MotionTween fade = new();
    readonly FocusCue focus;
    public bool Notification { get; set; }
    public bool Selected { get; set; }
    public bool SuppressFocusOutline { get; set; }
    internal bool ShowsFocusOutline => IsFocused && focus.Visible && !SuppressFocusOutline && IsEnabled;
    public string Kind { get; }
    public IconButton(string kind, string label)
    {
        Kind = kind;
        artwork = kind switch
        {
            "refresh" => "refresh-cw", "accounts" => "users", "settings" => "settings",
            "pin" => "pin", "close" => "x", "delete" => "trash-2", "back" => "arrow-left",
            "drag" => "grip-vertical", "confirm" => "check", _ => throw new ArgumentException("Unknown icon")
        };

        Width = Height = UiMetrics.IconButtonSize;
        Cursor = new Cursor(StandardCursorType.Hand);
        Template = ControlSurface.Template<Button>();
        focus = new(this);
        Avalonia.Automation.AutomationProperties.SetName(this, label);
        PropertyChanged += (_, _) => InvalidateVisual();
        DetachedFromVisualTree += (_, _) => fade.Dispose();
    }
    public void FadeVisible(bool show)
    {
        IsHitTestVisible = show;
        Focusable = show;
        double from = Opacity, target = show ? 1 : 0;
        fade.Start(Motion.IconFadeMilliseconds, t => Opacity = from + (target - from) * t, ease: Motion.Linear);
    }
    public override void Render(DrawingContext context)
    {
        if (Selected || IsPointerOver)
            context.FillRectangle(Palette.Brush(Palette.InteractionSurface), new Rect(Bounds.Size));
        var ink = Palette.IconInk(IsEnabled, Selected);
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var size = UiMetrics.IconSize;
        IconArtwork.Draw(context, artwork, new Rect(center.X - size / 2d, center.Y - size / 2d, size, size), ink);
        if (Notification)
            context.DrawEllipse(Palette.Brush(Palette.Warning), null,
                new Point(Bounds.Width - UiMetrics.FocusInset - UiMetrics.WarningDotSize / 2d, UiMetrics.FocusInset + UiMetrics.WarningDotSize / 2d),
                UiMetrics.WarningDotSize / 2d, UiMetrics.WarningDotSize / 2d);
        if (ShowsFocusOutline)
            context.DrawRectangle(null, new Pen(Palette.Brush(Palette.FocusBorder), UiMetrics.BorderWidth),
                new Rect(UiMetrics.FocusInset, UiMetrics.FocusInset, Bounds.Width - 2 * UiMetrics.FocusInset - UiMetrics.BorderWidth, Bounds.Height - 2 * UiMetrics.FocusInset - UiMetrics.BorderWidth));
    }
}
