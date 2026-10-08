namespace Pace;

internal class InfoSection : PaintedPanel, IThemedSection
{
    bool separator;
    public bool ShowSeparator { get => separator; set { separator = value; InvalidateVisual(); } }
    public void Add(string text, Color color)
    {
        var label = Palette.Label(text, color: color);
        label.TextWrapping = TextWrapping.Wrap;
        label.TextTrimming = TextTrimming.None;
        Children.Add(label);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        double height = 2 * UiMetrics.ContentInset;
        foreach (var child in Children)
        {
            child.Measure(new Size(Math.Max(1, availableSize.Width - 2 * UiMetrics.ContentInset), double.PositiveInfinity));
            height += Math.Max(UiMetrics.DetailInfoLineHeight, child.DesiredSize.Height);
        }
        return new(availableSize.Width, height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = UiMetrics.ContentInset;
        foreach (var child in Children)
        {
            double height = Math.Max(UiMetrics.DetailInfoLineHeight, child.DesiredSize.Height);
            child.Arrange(new Rect(UiMetrics.ContentInset, y, Math.Max(1, finalSize.Width - 2 * UiMetrics.ContentInset), height));
            y += height;
        }
        return finalSize;
    }
    protected override void DrawSurface(DrawingContext context) => SectionStyle.DrawSeparator(context, Bounds.Width, ShowSeparator);
}
