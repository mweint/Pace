namespace Pace;

internal sealed record AccountRowLayout(Rect Header, Rect Bar, Rect Metadata, Rect Error,
    Rect[] Secondary, double ContentBottom, double Height)
{
    public static int SegmentCount(TimeSpan period) => period == TimeSpan.FromHours(5) ? 5 : period == TimeSpan.FromDays(7) ? 7 : 1;
    public static AccountRowLayout Create(double width, bool hasWeekly, int secondaryCount, bool compactDetail = false)
    {
        double inset = UiMetrics.ContentInset, available = Math.Max(0, width - 2 * inset);
        double titleHeight = Math.Max(UiMetrics.ServiceIconSize, Palette.TextHeight(true, compactDetail ? Palette.BodySize : Palette.AccountSize));
        double bodyHeight = Palette.TextHeight();
        var header = new Rect(inset, inset, available, titleHeight);
        var error = new Rect(inset, header.Bottom + UiMetrics.CardGap, available, bodyHeight * 2);
        var bar = new Rect(inset, header.Bottom + (compactDetail ? UiMetrics.DetailLimitGap : UiMetrics.CardGap) + UiMetrics.PaceMarkerOverhang,
            available, compactDetail ? UiMetrics.DetailLimitBarHeight : UiMetrics.WeeklyBarHeight);
        var metadata = new Rect(inset, bar.Bottom + UiMetrics.PaceMarkerOverhang + UiMetrics.ResetLineGap,
            available, compactDetail ? bodyHeight : UiMetrics.ResetBadgeHeight);
        double bottom = hasWeekly ? metadata.Bottom : error.Bottom;
        var secondary = new Rect[hasWeekly ? secondaryCount : 0];
        for (int i = 0; i < secondary.Length; i++)
        {
            secondary[i] = new(inset, bottom + (i == 0 ? UiMetrics.CompactLimitGroupGap : UiMetrics.InlineGap), available, Math.Max(bodyHeight, UiMetrics.CompactLimitBarHeight));
            bottom = secondary[i].Bottom;
        }
        return new(header, bar, metadata, error, secondary, bottom, Math.Ceiling(bottom + inset));
    }
}
