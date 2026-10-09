namespace Pace;

internal sealed class FooterBar : Canvas
{
    readonly TextBlock label;
    public string Caption { set => label.Text = value; }
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
