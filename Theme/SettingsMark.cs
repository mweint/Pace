namespace Usage;

internal static class SettingsMark
{
    public static void Draw(Graphics graphics, Color ink)
    {
        using var pen = new Pen(ink, UiMetrics.EmphasisBorderWidth);
        float center = UiMetrics.IconButtonSize / 2f;
        float radius = UiMetrics.IconSize / 2f - UiMetrics.BorderWidth;
        graphics.DrawEllipse(pen, center - radius, center - radius, radius * 2, radius * 2);
        graphics.DrawEllipse(pen, center - radius / 3, center - radius / 3, radius * 2 / 3, radius * 2 / 3);
        for (int tooth = 0; tooth < 8; tooth++)
        {
            double angle = tooth * Math.PI / 4;
            graphics.DrawLine(pen, center + (float)Math.Cos(angle) * radius, center + (float)Math.Sin(angle) * radius,
                center + (float)Math.Cos(angle) * (radius + UiMetrics.EmphasisBorderWidth), center + (float)Math.Sin(angle) * (radius + UiMetrics.EmphasisBorderWidth));
        }
    }
}
