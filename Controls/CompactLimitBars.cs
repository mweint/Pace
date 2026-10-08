namespace Usage;

// Secondary limits show utilization, colored by the same pace rules as full cards.
internal static class CompactLimitBars
{
    public static List<(string Label, Window Window)> For(Reading reading, Preference? preference = null)
    {
        var result = new List<(string, Window)>();
        if (reading.Account.Service != "Claude" || reading.Limits == null)
            return result;
        if (preference?.ShowFiveHour != false && reading.Limits.FirstOrDefault(limit => limit.Key == "session") is { } session)
            result.Add(("5h", session.Window));
        if (preference?.ShowFable != false && reading.Limits.FirstOrDefault(limit => limit.Key.Equals("model:Fable", StringComparison.OrdinalIgnoreCase) || limit.Key == "fable") is { } fable)
            result.Add(("Fable", fable.Window));
        return result;
    }

    public static void Draw(Graphics graphics, Reading reading, AccountRowLayout layout, Preference? preference = null)
    {
        using var font = Palette.BodyFont();
        using var ink = new SolidBrush(Palette.Muted);
        using var track = new SolidBrush(Palette.Track);
        int index = 0;
        var now = DateTimeOffset.UtcNow;
        foreach (var (label, window) in For(reading, preference))
        {
            var row = layout.Secondary[index++];
            graphics.DrawString(label, font, ink, row.Left, AccountRowLayout.TextTop(row, font));
            float x = row.Left + 46, barWidth = row.Right - x;
            float y = row.Top + (row.Height - UiMetrics.CompactLimitBarHeight) / 2;
            graphics.FillRectangle(track, x, y, barWidth, UiMetrics.CompactLimitBarHeight);
            using var accent = new SolidBrush(Palette.Status(window, now));
            graphics.FillRectangle(accent, x, y, barWidth * (float)Math.Clamp(window.Used / 100, 0, 1), UiMetrics.CompactLimitBarHeight);
            if (PaceMath.Calculate(window, now) is { } pace)
            {
                float tick = x + barWidth * (float)(pace.Expected / 100);
                using var marker = new SolidBrush(Palette.Text);
                float markerX = Math.Clamp(tick - UiMetrics.CompactPaceMarkerWidth / 2f, x, x + barWidth - UiMetrics.CompactPaceMarkerWidth);
                graphics.FillRectangle(marker, markerX, y - UiMetrics.CompactPaceMarkerOverhang,
                    UiMetrics.CompactPaceMarkerWidth, UiMetrics.CompactLimitBarHeight + 2 * UiMetrics.CompactPaceMarkerOverhang);
            }
        }
    }
}
