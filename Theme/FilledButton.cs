namespace Pace;

public sealed class FilledButton : Button
{
    readonly FocusCue focus;
    public string Text { get; }
    public bool Selected { get; set; }
    internal bool ShowsFocusOutline => IsFocused && focus.Visible && IsEnabled;
    internal string? Service { get; init; }
    public FilledButton(string text)
    {
        Text = text;
        Height = UiMetrics.TextButtonHeight;
        MinWidth = UiMetrics.TextButtonWidth(text);
        Cursor = new Cursor(StandardCursorType.Hand);
        Template = ControlSurface.Template<Button>();
        focus = new(this);
        Avalonia.Automation.AutomationProperties.SetName(this, text);
        PropertyChanged += (_, _) => InvalidateVisual();
    }
    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Palette.Brush(Selected ? Palette.InteractionSurface : IsEnabled && IsPressed ? Palette.ButtonPressed : IsEnabled && IsPointerOver ? Palette.ButtonHover : Palette.Action), new Rect(Bounds.Size));
        var ink = Selected ? Palette.SelectionBorder : Palette.ControlText(IsEnabled);
        var label = Palette.Format(Text, ink);
        double mark = Service == null ? 0 : UiMetrics.ServiceIconSize + UiMetrics.CardGap;
        double left = Math.Round((Bounds.Width - label.WidthIncludingTrailingWhitespace - mark) / 2);
        if (Service != null) ServiceMark.Draw(context, Service, new Point(left, Math.Round((Bounds.Height - UiMetrics.ServiceIconSize) / 2)));
        context.DrawText(label, new Point(left + mark, Math.Round((Bounds.Height - label.Height) / 2)));
        if (ShowsFocusOutline)
            context.DrawRectangle(null, new Pen(Palette.Brush(Palette.FocusBorder), UiMetrics.BorderWidth), new Rect(UiMetrics.FocusInset, UiMetrics.FocusInset, Bounds.Width - 2 * UiMetrics.FocusInset - UiMetrics.BorderWidth, Bounds.Height - 2 * UiMetrics.FocusInset - UiMetrics.BorderWidth));
    }
}
