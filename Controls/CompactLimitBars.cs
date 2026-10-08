namespace Pace;

internal static class CompactLimitBars
{
    public static List<(string Label, UsageWindow Window)> For(Reading reading, Preference? preference = null)
    {
        var shown = (reading.Limits ?? []).Where(limit => Preference.ShowsBar(preference, reading.Account, limit)).ToList();
        var result = new List<(string, UsageWindow)>();
        if (shown.FirstOrDefault(limit => limit.IsSession) is { } session) result.Add(("5h", session.Window));
        if (shown.FirstOrDefault(limit => limit.IsFable) is { } fable) result.Add(("Fable", fable.Window));
        return result;
    }
    public static void Draw(DrawingContext context, Reading reading, AccountRowLayout layout, Preference? preference = null)
    {
        int index = 0;
        foreach (var (label, window) in For(reading, preference))
        {
            var row = layout.Secondary[index++];
            var text = Palette.Format(label, Palette.Muted);
            context.DrawText(text, new Point(row.Left, Math.Round(row.Top + (row.Height - text.Height) / 2)));
            var bar = new Rect(row.Left + UiMetrics.CompactLimitLabelWidth, row.Top + (row.Height - UiMetrics.CompactLimitBarHeight) / 2,
                row.Width - UiMetrics.CompactLimitLabelWidth, UiMetrics.CompactLimitBarHeight);
            UsageBar.Draw(context, window, bar, false);
        }
    }
}
internal static class UsageBar
{
    public static void Draw(DrawingContext context, UsageWindow window, Rect bar, bool segmented)
    {
        var now = DateTimeOffset.UtcNow;
        context.FillRectangle(Palette.Brush(Palette.Track), bar);
        context.FillRectangle(Palette.Brush(Palette.Status(window, now)), new Rect(bar.X, bar.Y, bar.Width * Math.Clamp(window.Used / 100, 0, 1), bar.Height));
        if (segmented)
        {
            int segments = AccountRowLayout.SegmentCount(window.Period);
            var divider = new Pen(Palette.Brush(Palette.BarDivider), UiMetrics.BorderWidth);
            for (int i = 1; i < segments; i++)
                context.DrawLine(divider, new Point(bar.X + bar.Width * i / segments, bar.Y), new Point(bar.X + bar.Width * i / segments, bar.Bottom));
        }
        if (PaceMath.Calculate(window, now) is { } pace)
        {
            double x = Math.Clamp(bar.X + bar.Width * pace.Expected / 100 - UiMetrics.PaceMarkerWidth / 2d, bar.X, bar.Right - UiMetrics.PaceMarkerWidth);
            context.FillRectangle(Palette.Brush(Palette.Text), new Rect(x, bar.Y - UiMetrics.PaceMarkerOverhang, UiMetrics.PaceMarkerWidth, bar.Height + 2 * UiMetrics.PaceMarkerOverhang));
        }
    }
}
