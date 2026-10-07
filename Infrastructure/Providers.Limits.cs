using System.Text.Json.Nodes;

namespace Usage;

public static partial class Providers
{
    public static List<UsageLimit> ParseLimits(string service, JsonObject data, DateTimeOffset now)
    {
        var result = new List<UsageLimit>();
        if (service == "Claude")
        {
            if (ParseClaude(data) is { } week)
                result.Add(new("weekly", "Weekly", week));
            void Add(string key, string name, JsonNode? used, JsonNode? reset, TimeSpan period)
            {
                if (Number(used) is not { } percent || !double.IsFinite(percent) || percent < 0 || ResetExpiry(reset) is not { } end)
                    return;
                if (result.All(r => r.Key != key))
                    result.Add(new(key, name, new(percent, end, period)));
            }

            Add("session", "Five-hour", data["five_hour"]?["utilization"], data["five_hour"]?["resets_at"], TimeSpan.FromHours(5));
            if (data["limits"] is JsonArray limits)
                foreach (var limit in limits.OfType<JsonObject>())
                {
                    string? model = Text(limit["scope"]?["model"]?["display_name"]);
                    if (Text(limit["group"]) != "weekly" || string.IsNullOrWhiteSpace(model))
                        continue;
                    Add("model:" + model, model + " · Weekly", limit["percent"], limit["resets_at"], TimeSpan.FromDays(7));
                }

            foreach (var (key, label) in new[]
            {
                ("seven_day_opus", "Opus"),
                ("seven_day_sonnet", "Sonnet")
            }

            )
                Add("model:" + label, label + " · Weekly", data[key]?["utilization"], data[key]?["resets_at"], TimeSpan.FromDays(7));
        }
        else
        {
            void AddWindows(JsonNode? block, string prefix, string? name)
            {
                if (block is not JsonObject)
                    return;
                foreach (string field in new[]
                {
                    "primary_window",
                    "secondary_window",
                    "primary",
                    "secondary"
                }

                )
                {
                    var item = block[field];
                    double seconds = Number(item?["limit_window_seconds"]) ?? (Number(item?["window_minutes"]) * 60) ?? 0;
                    if (Number(item?["used_percent"]) is not { } used || !double.IsFinite(used) || used < 0 || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
                        continue;
                    var reset = ResetExpiry(item?["reset_at"]);
                    if (reset == null && Number(item?["reset_after_seconds"]) is { } remaining)
                    {
                        try
                        {
                            reset = now.AddSeconds(remaining);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                        }
                    }

                    if (reset == null)
                        continue;
                    string duration = Math.Abs(seconds - 604800) < 60 ? "Weekly" : Math.Abs(seconds - 18000) < 60 ? "Five-hour" : PaceMath.Duration(TimeSpan.FromSeconds(seconds));
                    result.Add(new(prefix + field, string.IsNullOrWhiteSpace(name) ? duration : name + " · " + duration, new(used, reset.Value, TimeSpan.FromSeconds(seconds))));
                }
            }

            AddWindows(data["rate_limit"] ?? data["rate_limits"], "main:", null);
            if (data["additional_rate_limits"] is JsonArray additional)
                foreach (var item in additional.OfType<JsonObject>())
                    AddWindows(item["rate_limit"], "extra:" + Text(item["limit_name"]) + ":", Text(item["limit_name"]));
            result = result.OrderBy(r => Math.Abs(r.Window.Period.TotalDays - 7)).ToList();
        }

        return result;
    }
}
