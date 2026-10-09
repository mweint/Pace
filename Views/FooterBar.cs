namespace Pace;

internal sealed class FooterBar : Canvas
{
    readonly TextBlock label;
    TextBlock? status;
    // Status text replaces the title: body size, muted and centred in the bar.
    public string Status
    {
        set
        {
            if (status == null)
            {
                Children.Remove(label);
                status = Palette.Label("", color: Palette.Muted);
                Children.Add(status); SetLeft(status, UiMetrics.ContentInset);
            }
            status.Text = value;
            status.Measure(Size.Infinity);
            SetTop(status, Math.Round((Height - status.DesiredSize.Height) / 2));
        }
    }
    public FooterBar(string caption, IconButton? back = null, params IconButton[] actions)
    {
        Height = UiMetrics.ToolbarHeight;
        Background = Palette.Brush(Palette.Footer);
        if (back != null)
        {
            Children.Add(back); SetLeft(back, UiMetrics.ToolbarInset); SetTop(back, UiMetrics.ToolbarInset);
        }
        label = Palette.Label(caption, true, Palette.Muted, Palette.TitleSize);
        Children.Add(label);
        SetLeft(label, back == null ? UiMetrics.ContentInset : UiMetrics.ToolbarLabelLeft);
        SetTop(label, UiMetrics.ToolbarTextTop);
        for (int i = 0; i < actions.Length; i++)
        {
            Children.Add(actions[i]);
            SetRight(actions[i], UiMetrics.ToolbarInset + (actions.Length - 1 - i) * (UiMetrics.IconButtonSize + UiMetrics.InlineGap));
            SetTop(actions[i], UiMetrics.ToolbarInset);
        }
    }
}
