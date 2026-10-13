namespace Pace;

public static class PaceMath
{
    // At or below the marker, the normal rate lasts until the reset. Slightly ahead is on pace;
    // ahead means slowing down by more than AheadSlowdown for the rest of the window, and never
    // less than AheadFloor points, so the last stretch before a reset does not flicker.
    public const double AheadSlowdown = .05, AheadFloor = 2;
    public const double ExhaustedThreshold = 100;
    static readonly TimeSpan StartTolerance = TimeSpan.FromMinutes(10);
    public static double AheadThreshold(PaceGap pace) => Math.Max(AheadFloor, AheadSlowdown * (100 - pace.Expected));
    public static PaceState Classify(PaceGap? pace) => pace is null ? PaceState.Unavailable
        : pace.Points > AheadThreshold(pace) ? PaceState.Above : pace.Points <= 0 ? PaceState.Below : PaceState.OnPace;
    static bool Level(PaceGap p) => Math.Round(p.Points) == 0;
    // The most pressing state across windows: exhausted, then ahead, on pace, below.
    public static PaceState Strictest(IEnumerable<UsageWindow> windows, DateTimeOffset now) =>
        windows.Select(w => Classify(w, now)).DefaultIfEmpty(PaceState.Unavailable).MaxBy(Urgency);
    static int Urgency(PaceState state) => state switch
    {
        PaceState.Exhausted => 4, PaceState.Above => 3, PaceState.OnPace => 2, PaceState.Below => 1, _ => 0
    };
    public static PaceState Classify(UsageWindow window, DateTimeOffset now)
    {
        var pace = Calculate(window, now);
        bool usable = double.IsFinite(window.Used) && window.Period > TimeSpan.Zero && (window.Reset == null || pace != null);
        return usable && window.Used >= ExhaustedThreshold ? PaceState.Exhausted : Classify(pace);
    }
    public static string Compact(UsageWindow window, DateTimeOffset now) =>
        Classify(window, now) == PaceState.Exhausted ? "limit reached" : window.Reset == null ? "reset unavailable" : Compact(Calculate(window, now));
    public static string Summary(UsageWindow window, DateTimeOffset now) =>
        Classify(window, now) == PaceState.Exhausted ? "Limit reached" : Summary(Calculate(window, now));
    public static string HoverSummary(UsageWindow window, DateTimeOffset now) =>
        Classify(window, now) == PaceState.Exhausted ? "Limit reached" : HoverSummary(Calculate(window, now));
    public static string HoverSummary(Reading reading, DateTimeOffset now) =>
        reading.Weekly is { } weekly && reading.Error == null ? HoverSummary(weekly, now) : "Unavailable";
    public static PaceGap? Calculate(UsageWindow w, DateTimeOffset now)
    {
        if (!double.IsFinite(w.Used) || w.Period <= TimeSpan.Zero || w.Reset is not { } reset || now >= reset)
            return null;
        var elapsed = now - (reset - w.Period);
        // An unused window reports a fresh full period on every request, so its start can sit a
        // moment ahead of this clock; treat that as just started rather than unavailable.
        if (elapsed < -StartTolerance)
            return null;
        double fraction = Math.Clamp(elapsed.TotalSeconds / w.Period.TotalSeconds, 0, 1);
        double expected = fraction * 100;
        double points = w.Used - expected;
        TimeSpan? ahead = points > 0 ? TimeSpan.FromSeconds(w.Period.TotalSeconds * points / 100) : null;
        return new(expected, points, ahead);
    }

    public static string Summary(PaceGap? p) => p is null ? "Pace unavailable" : Level(p) ? "On pace" : $"{Math.Abs(p.Points):0}% {(p.Points > 0 ? "above" : "below")} pace";
    public static string Compact(PaceGap? p) => p is null ? "—" : Level(p) ? "on pace" : $"{Math.Abs(p.Points):0}% {(p.Points > 0 ? "above" : "below")}";
    public static string? AheadHours(PaceGap? pace) => Classify(pace) == PaceState.Above && pace?.Ahead is { } ahead ? $"{ahead.TotalHours:0}h" : null;
    public static string? AheadLabel(PaceGap? pace) => AheadHours(pace) is { } hours ? $"~{hours} ahead" : null;
    public static string HoverSummary(PaceGap? pace) => pace is null ? "Unavailable" : $"{pace.Points:+0;-0;0}%" + (AheadHours(pace) is { } hours ? $" · {hours}" : "");
    public static string ResetLabel(DateTimeOffset? reset, DateTimeOffset now, TimeZoneInfo? zone = null)
    {
        if (reset is not { } end)
            return "Reset time unavailable";
        if (now >= end)
            return "Reset passed · refresh needed";
        zone ??= TimeZoneInfo.Local;
        var localEnd = TimeZoneInfo.ConvertTime(end, zone);
        var localNow = TimeZoneInfo.ConvertTime(now, zone);
        return "Resets " + localEnd.ToString(localEnd.Date == localNow.Date ? "h:mm tt" : "ddd · h:mm tt");
    }

    public static string ResetCountdown(DateTimeOffset? reset, DateTimeOffset now) => reset is not { } end
        ? "Reset time unavailable" : now >= end ? "Reset passed · refresh needed" : $"Resets in {Duration(end - now)}";

    public static string Duration(TimeSpan t)
    {
        if (t.TotalMinutes <= 0)
            return "now";
        if (t.TotalDays >= 1)
            return $"{(int)t.TotalDays}d {t.Hours}h";
        if (t.TotalHours >= 1)
            return $"{(int)t.TotalHours}h {t.Minutes}m";
        return $"{Math.Max(1, (int)t.TotalMinutes)}m";
    }
}
