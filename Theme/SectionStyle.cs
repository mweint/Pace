namespace Pace;

internal enum SectionGroup { None, SecondaryLimits }
internal interface IThemedSection
{
    bool ShowSeparator { get; set; }
    SectionGroup Group => SectionGroup.None;
}
internal static class SectionStyle
{
    public static void DrawWindowFrame(DrawingContext context, Size size) =>
        context.DrawRectangle(null, new Pen(Palette.Brush(Palette.WindowBorder), UiMetrics.WindowBorderWidth),
            new Rect(.5, .5, size.Width - UiMetrics.WindowBorderWidth, size.Height - UiMetrics.WindowBorderWidth));
    public static Border Identity(Control content) => new()
    {
        Padding = new Thickness(UiMetrics.ContentInset),
        Background = Palette.Brush(Palette.SectionBackground), Child = content
    };
    public static void DrawSeparator(DrawingContext context, double width, bool visible)
    {
        if (visible)
            context.DrawLine(new Pen(Palette.Brush(Palette.InputBorder), UiMetrics.BorderWidth),
                new Point(UiMetrics.ContentInset, .5), new Point(width - UiMetrics.ContentInset, .5));
    }
}
