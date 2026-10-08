namespace Pace;

public class SectionList : PaintedPanel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double height = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            height += child.DesiredSize.Height;
        }
        return new(availableSize.Width, height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;
        bool first = true;
        SectionGroup previous = SectionGroup.None;
        foreach (var child in Children)
        {
            if (child is IThemedSection section)
            {
                section.ShowSeparator = !first && (section.Group == SectionGroup.None || section.Group != previous);
                previous = section.Group;
                first = false;
            }
            child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
        return finalSize;
    }
}
