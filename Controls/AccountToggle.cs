namespace Pace;

public sealed class AccountToggle : CheckBox
{
    readonly FocusCue focus;
    public string Text { get; }
    public bool Checked { get => IsChecked == true; set => IsChecked = value; }
    public AccountToggle(string text, bool value = false)
    {
        Text = text;
        Checked = value;
        Height = UiMetrics.ToggleHeight;
        Width = UiMetrics.ToggleTrackWidth + UiMetrics.ToggleTextGap + UiMetrics.InlineGap + Palette.TextWidth(text);
        HorizontalAlignment = HorizontalAlignment.Left;
        Template = ControlSurface.Template<CheckBox>();
        focus = new(this);
        Cursor = new Cursor(StandardCursorType.Hand);
        Avalonia.Automation.AutomationProperties.SetName(this, text);
        PropertyChanged += (_, _) => InvalidateVisual();
    }
    public override void Render(DrawingContext context)
    {
        double y = Math.Truncate((Bounds.Height - UiMetrics.ToggleTrackHeight) / 2);
        context.FillRectangle(Palette.Brush(Palette.ToggleTrack(IsEnabled, Checked)), new Rect(0, y, UiMetrics.ToggleTrackWidth, UiMetrics.ToggleTrackHeight));
        double x = Checked ? UiMetrics.ToggleTrackWidth - UiMetrics.ToggleKnobSize - UiMetrics.ToggleKnobInset : UiMetrics.ToggleKnobInset;
        context.FillRectangle(Palette.Brush(Palette.ToggleKnob(Checked)), new Rect(x, y + UiMetrics.ToggleKnobInset, UiMetrics.ToggleKnobSize, UiMetrics.ToggleKnobSize));
        var text = Palette.Format(Text, Palette.ControlText(IsEnabled));
        context.DrawText(text, new Point(UiMetrics.ToggleTrackWidth + UiMetrics.ToggleTextGap, Math.Round((Bounds.Height - text.Height) / 2)));
        if (IsFocused && focus.Visible)
            context.DrawRectangle(null, new Pen(Palette.Brush(Palette.FocusBorder), UiMetrics.BorderWidth), new Rect(0, UiMetrics.FocusInset, Bounds.Width - UiMetrics.EmphasisBorderWidth, Bounds.Height - 2 * UiMetrics.FocusInset));
    }
}
