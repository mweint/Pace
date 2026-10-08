namespace Pace;

internal sealed class SettingsSection : PaintedPanel, IThemedSection
{
    bool separator;
    public bool ShowSeparator { get => separator; set { separator = value; InvalidateVisual(); } }
    public AccountToggle Toggle(string text, bool selected) => new(text, selected)
    {
        Margin = new Thickness(0, 0, 0, UiMetrics.InlineGap)
    };
    public TextBlock Label(string text, bool heading = false)
    {
        var label = Palette.Label(text, heading, heading ? Palette.Text : Palette.Muted, heading ? Palette.BarSize : Palette.BodySize);
        label.Margin = new Thickness(0, 0, 0, UiMetrics.CardGap);
        Children.Add(label);
        return label;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        double height = 2 * UiMetrics.ContentInset;
        foreach (var child in Children)
        {
            child.Measure(new Size(Math.Max(1, availableSize.Width - 2 * UiMetrics.ContentInset), double.PositiveInfinity));
            height += child.DesiredSize.Height;
        }
        return new(availableSize.Width, height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = UiMetrics.ContentInset;
        foreach (var child in Children)
        {
            child.Arrange(new Rect(UiMetrics.ContentInset, y, Math.Max(1, finalSize.Width - 2 * UiMetrics.ContentInset), child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
        return finalSize;
    }
    protected override void DrawSurface(DrawingContext context) => SectionStyle.DrawSeparator(context, Bounds.Width, ShowSeparator);
}
