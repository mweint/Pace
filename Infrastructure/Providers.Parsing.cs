using System.Text.Json.Nodes;

namespace Pace;

public static partial class Providers
{
    static readonly string[] CodexWindowFields = ["primary_window", "secondary_window", "primary", "secondary"];
    static readonly TimeSpan Week = TimeSpan.FromDays(7), FiveHours = TimeSpan.FromHours(5);

    public static UsageWindow? ParseClaude(JsonObject document)
    {
        var week = document["seven_day"];
        if (Number(week?["utilization"]) is not { } used || used < 0 || !DateTimeOffset.TryParse(Text(week?["resets_at"]), out var end))
            return null;
        return new(used, end, Week);
    }

    // The allowance closest to seven days (and longer than six hours), whichever slot the API uses.
    public static UsageWindow? ParseCodex(JsonObject document, DateTimeOffset received) =>
        CodexWindows(document["rate_limit"] ?? document["rate_limits"] ?? document, received)
            .Select(w => w.Window).Where(w => w.Period > TimeSpan.FromHours(6))
            .OrderBy(w => Math.Abs(w.Period.TotalDays - 7)).FirstOrDefault();

    static IEnumerable<(string Field, UsageWindow Window)> CodexWindows(JsonNode? block, DateTimeOffset received)
    {
        if (block is not JsonObject)
            yield break;
        foreach (string field in CodexWindowFields)
        {
            var item = block[field];
            if (Number(item?["used_percent"]) is not { } used || used < 0)
                continue;
            double seconds = Number(item?["limit_window_seconds"]) ?? Number(item?["window_minutes"]) * 60
                ?? (field.StartsWith("secondary", StringComparison.Ordinal) ? Week : FiveHours).TotalSeconds;
            if (seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
                continue;
            var reset = ResetExpiry(item?["reset_at"]) ?? After(received, Number(item?["reset_after_seconds"] ?? item?["resets_in_seconds"]));
            if (reset != null)
                yield return (field, new(used, reset, TimeSpan.FromSeconds(seconds)));
        }
    }

    // ISO dates, or Unix timestamps in seconds or milliseconds.
    static DateTimeOffset? ResetExpiry(JsonNode? value)
    {
        if (DateTimeOffset.TryParse(Text(value), out var date))
            return date;
        if (Number(value) is not { } number)
            return null;
        try
        {
            return number >= 1_000_000_000_000 ? DateTimeOffset.FromUnixTimeMilliseconds((long)number) : DateTimeOffset.FromUnixTimeSeconds((long)number);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    static DateTimeOffset? After(DateTimeOffset start, double? seconds)
    {
        if (seconds is not { } value)
            return null;
        try
        {
            return start.AddSeconds(value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
