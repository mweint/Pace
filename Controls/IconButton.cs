namespace Pace;

public sealed class IconButton : Button
{
    readonly string artwork;
    readonly MotionTween fade;
    readonly FocusCue focus;
    public bool Notification { get; set; }
    public bool Selected { get; set; }
    public bool SuppressFocusOutline { get; set; }
    internal bool ShowsFocusOutline => IsFocused && focus.Visible && !SuppressFocusOutline && IsEnabled;
    public string Kind { get; }
    bool spinning, turning, framePending;
    long spinOrigin;
    double stopAfterTurns;
    // Turns slowly while work is in progress, then finishes its current turn.
    public bool Spinning
    {
        get => spinning;
        set
        {
            if (value == spinning) return;
            spinning = value;
            if (value && !turning && Motion.Enabled) { turning = true; spinOrigin = Environment.TickCount64; }
            else if (!value) stopAfterTurns = Math.Ceiling(Turns);
            InvalidateVisual();
        }
    }
    double Turns => (Environment.TickCount64 - spinOrigin) / Motion.SpinMilliseconds;
    public IconButton(string kind, string label)
    {
        fade = new(this);
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
        if (turning && !spinning && Turns >= stopAfterTurns) turning = false;
        IconArtwork.Draw(context, artwork, new Rect(center.X - size / 2d, center.Y - size / 2d, size, size), ink, turning ? Turns % 1 * 360 : 0);
        if (turning && !framePending && TopLevel.GetTopLevel(this) is { } top)
        {
            framePending = true;
            top.RequestAnimationFrame(_ => { framePending = false; InvalidateVisual(); });
        }
        if (Notification)
            context.DrawEllipse(Palette.Brush(Palette.Warning), null,
                new Point(Bounds.Width - UiMetrics.FocusInset - UiMetrics.WarningDotSize / 2d, UiMetrics.FocusInset + UiMetrics.WarningDotSize / 2d),
                UiMetrics.WarningDotSize / 2d, UiMetrics.WarningDotSize / 2d);
        if (ShowsFocusOutline)
            context.DrawRectangle(null, new Pen(Palette.Brush(Palette.FocusBorder), UiMetrics.BorderWidth),
                new Rect(UiMetrics.FocusInset, UiMetrics.FocusInset, Bounds.Width - 2 * UiMetrics.FocusInset - UiMetrics.BorderWidth, Bounds.Height - 2 * UiMetrics.FocusInset - UiMetrics.BorderWidth));
    }
}
