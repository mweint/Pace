using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace Usage;
// Original implementation. Credentials are read in place; the sign-in applications own renewal.
public static partial class Providers
{
    public static Window? ParseCodex(JsonObject document, DateTimeOffset received)
    {
        JsonNode limits = document["rate_limit"] ?? document["rate_limits"] ?? document;
        var windows = new List<Window>();
        foreach (string field in new[]
        {
            "primary_window",
            "secondary_window",
            "primary",
            "secondary"
        }

        )
        {
            JsonNode? item = limits[field];
            double? used = Number(item?["used_percent"]);
            double length = Number(item?["limit_window_seconds"]) ?? (Number(item?["window_minutes"]) * 60) ?? (field.StartsWith("secondary") ? 604800 : 18000);
            if (used == null || used < 0 || length <= 21600 || length > TimeSpan.MaxValue.TotalSeconds)
                continue;
            DateTimeOffset? reset = null;
            if (Number(item?["reset_at"]) is double timestamp)
            {
                try
                {
                    reset = timestamp >= 1000000000000 ? DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp) : DateTimeOffset.FromUnixTimeSeconds((long)timestamp);
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
            else if (Number(item?["reset_after_seconds"] ?? item?["resets_in_seconds"]) is double seconds)
            {
                try
                {
                    reset = received.AddSeconds(seconds);
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }

            if (reset != null)
                windows.Add(new(used.Value, reset.Value, TimeSpan.FromSeconds(length)));
        }

        // Pick the allowance closest to a seven-day period, independent of which slot the API uses.
        return windows.OrderBy(w => Math.Abs(w.Period.TotalDays - 7)).FirstOrDefault();
    }

    public static Window? ParseClaude(JsonObject document)
    {
        JsonNode? week = document["seven_day"];
        double? used = Number(week?["utilization"]);
        if (used == null || used < 0 || !DateTimeOffset.TryParse(Text(week?["resets_at"]), out var end))
            return null;
        return new(used.Value, end, TimeSpan.FromDays(7));
    }
}
