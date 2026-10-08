namespace Usage;

// One layout owns both painted bounds and preferred height. All coordinates are
// logical pixels; the control applies the display scale once when rendering.
internal sealed record AccountRowLayout(RectangleF Header, RectangleF Bar, RectangleF Metadata,
    RectangleF Error, RectangleF[] Secondary, float ContentBottom, int Height)
{
    public static int SegmentCount(TimeSpan period) => period == TimeSpan.FromHours(5) ? 5 :
        period == TimeSpan.FromDays(7) ? 7 : 1;

    public static AccountRowLayout Create(float width, Font title, Font body, bool hasWeekly, int secondaryCount, bool compactDetail = false)
    {
        float inset = UiMetrics.ContentInset;
        float available = Math.Max(0, width - 2 * inset);
        float titleHeight = Math.Max(UiMetrics.ServiceIconSize, (float)Math.Ceiling(title.GetHeight(UiMetrics.BaseDpi)));
        float bodyHeight = (float)Math.Ceiling(body.GetHeight(UiMetrics.BaseDpi));
        var header = new RectangleF(inset, inset, available, titleHeight);
        var error = new RectangleF(inset, header.Bottom + UiMetrics.CardGap, available, bodyHeight * 2);
        int gap = compactDetail ? UiMetrics.DetailLimitGap : UiMetrics.CardGap;
        var bar = new RectangleF(inset, header.Bottom + gap + UiMetrics.PaceMarkerOverhang,
            available, compactDetail ? UiMetrics.DetailLimitBarHeight : UiMetrics.WeeklyBarHeight);
        var metadata = new RectangleF(inset, bar.Bottom + UiMetrics.PaceMarkerOverhang + UiMetrics.ResetLineGap,
            available, compactDetail ? bodyHeight : UiMetrics.ResetBadgeHeight);
        float bottom = hasWeekly ? metadata.Bottom : error.Bottom;
        var secondary = new RectangleF[hasWeekly ? secondaryCount : 0];
        for (int i = 0; i < secondary.Length; i++)
        {
            float top = bottom + (i == 0 ? UiMetrics.CompactLimitGroupGap : UiMetrics.InlineGap);
            secondary[i] = new(inset, top, available, Math.Max(bodyHeight, UiMetrics.CompactLimitBarHeight));
            bottom = secondary[i].Bottom;
        }
        return new(header, bar, metadata, error, secondary, bottom, (int)Math.Ceiling(bottom + inset));
    }

    public static float TextTop(RectangleF row, Font font) => row.Top + (row.Height - font.GetHeight(UiMetrics.BaseDpi)) / 2;
}
