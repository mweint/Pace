namespace Pace;

internal static class LimitWarning
{
    public const double UsageThreshold = 90;

    public static bool Approaching(UsageWindow window, DateTimeOffset now) =>
        double.IsFinite(window.Used) && window.Used >= UsageThreshold &&
        window.Period > TimeSpan.Zero && (window.Reset == null || PaceMath.Calculate(window, now) != null);

    public static List<UsageLimit> For(Reading reading, DateTimeOffset now)
    {
        if (reading.Error != null)
            return [];
        var limits = reading.Limits ?? [];
        var result = limits.Where(limit => Approaching(limit.Window, now)).ToList();
        if (reading.Weekly is { } weekly && Approaching(weekly, now) &&
            !result.Any(limit => limit.Window == weekly))
            result.Insert(0, new(UsageLimit.WeeklyKey, "Weekly", weekly));
        return result;
    }

    public static List<UsageLimit> Hidden(Reading reading, Preference? preference, DateTimeOffset now) =>
        For(reading, now).Where(limit => limit.Window != reading.Weekly && !Preference.ShowsBar(preference, reading.Account, limit)).ToList();

    public static string Summary(Reading reading, DateTimeOffset now) => Summary(For(reading, now));

    public static string Summary(IEnumerable<UsageLimit> limits) =>
        string.Join("\n", limits.Select(limit =>
            $"{limit.Name} · {limit.Window.Used:0}% used · {(limit.Window.Used >= PaceMath.ExhaustedThreshold ? "limit reached" : "approaching limit")}"));
}
