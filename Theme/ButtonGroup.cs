namespace Pace;

internal sealed class ButtonGroup : StackPanel
{
    public ButtonGroup(params FilledButton[] buttons)
    {
        Orientation = Orientation.Horizontal;
        Spacing = UiMetrics.InlineGap;
        Margin = new Thickness(0, 0, 0, UiMetrics.CardGap);
        HorizontalAlignment = HorizontalAlignment.Left;
        Children.AddRange(buttons);
    }
}
