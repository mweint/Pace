namespace Usage;

public static class PaceMath
{
    public const double BelowThreshold = -4;
    public const double AboveThreshold = 2;
    public const double ExhaustedThreshold = 100;
    public static PaceState Classify(Pace? pace) => pace is null ? PaceState.Unavailable : pace.Points > AboveThreshold ? PaceState.Above : pace.Points < BelowThreshold ? PaceState.Below : PaceState.OnPace;
    public static PaceState Classify(Window window, DateTimeOffset now)
    {
        var pace = Calculate(window, now);
        bool usable = double.IsFinite(window.Used) && window.Period > TimeSpan.Zero && (window.Reset == null || pace != null);
        return usable && window.Used >= ExhaustedThreshold ? PaceState.Exhausted : Classify(pace);
    }
    public static string Compact(Window window, DateTimeOffset now) =>
        Classify(window, now) == PaceState.Exhausted ? "limit reached" : window.Reset == null ? "reset unavailable" : Compact(Calculate(window, now));
    public static string Summary(Window window, DateTimeOffset now) =>
        Classify(window, now) == PaceState.Exhausted ? "Limit reached" : Summary(Calculate(window, now));
    public static string HoverSummary(Window window, DateTimeOffset now) =>
        Classify(window, now) == PaceState.Exhausted ? "Limit reached" : HoverSummary(Calculate(window, now));
    public static Pace? Calculate(Window w, DateTimeOffset now)
    {
        if (!double.IsFinite(w.Used) || w.Period <= TimeSpan.Zero || w.Reset is not { } reset || now >= reset)
            return null;
        var elapsed = now - (reset - w.Period);
        if (elapsed < TimeSpan.Zero)
            return null;
        double fraction = Math.Clamp(elapsed.TotalSeconds / w.Period.TotalSeconds, 0, 1);
        double expected = fraction * 100;
        double points = w.Used - expected;
        TimeSpan? ahead = points > 0 ? TimeSpan.FromSeconds(w.Period.TotalSeconds * points / 100) : null;
        return new(expected, points, ahead);
    }

    public static string Summary(Pace? p) => p is null ? "Pace unavailable" : Classify(p) == PaceState.OnPace ? "On pace" : $"{Math.Abs(p.Points):0}% {(p.Points > 0 ? "above" : "below")} pace";
    public static string Compact(Pace? p) => p is null ? "—" : Classify(p) == PaceState.OnPace ? "on pace" : $"{Math.Abs(p.Points):0}% {(p.Points > 0 ? "above" : "below")}";
    public static string? AheadHours(Pace? pace) => Classify(pace) == PaceState.Above && pace?.Ahead is { } ahead ? $"{ahead.TotalHours:0}h" : null;
    public static string? AheadLabel(Pace? pace) => AheadHours(pace) is { } hours ? $"~{hours} ahead" : null;
    public static string HoverSummary(Pace? pace) => pace is null ? "Unavailable" : $"{pace.Points:+0;-0;0}%" + (AheadHours(pace) is { } hours ? $" · {hours}" : "");
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
