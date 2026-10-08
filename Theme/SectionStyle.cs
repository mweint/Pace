namespace Usage;

internal enum SectionGroup { None, SecondaryLimits }

internal interface IThemedSection
{
    bool ShowSeparator { get; set; }
    SectionGroup Group => SectionGroup.None;
}

internal static class SectionStyle
{
    public static void Apply(Control section)
    {
        section.BackColor = Palette.SectionBackground;
        section.Margin = Padding.Empty;
    }

    public static void DrawSeparator(Graphics graphics, float width, bool visible)
    {
        if (!visible)
            return;
        using var edge = new Pen(Palette.InputBorder, UiMetrics.BorderWidth);
        graphics.DrawLine(edge, UiMetrics.ContentInset, 0, width - UiMetrics.ContentInset, 0);
    }
}
