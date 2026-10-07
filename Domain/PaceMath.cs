namespace Usage;

public static class PaceMath
{
    public const double BelowThreshold = -4;
    public const double AboveThreshold = 2;
    public static PaceState Classify(Pace? pace) => pace is null ? PaceState.Unavailable : pace.Points > AboveThreshold ? PaceState.Above : pace.Points < BelowThreshold ? PaceState.Below : PaceState.OnPace;
    public static Pace? Calculate(Window w, DateTimeOffset now)
    {
        if (!double.IsFinite(w.Used) || w.Period <= TimeSpan.Zero || now >= w.Reset)
            return null;
        var elapsed = now - (w.Reset - w.Period);
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
