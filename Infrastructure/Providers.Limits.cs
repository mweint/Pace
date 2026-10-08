using System.Text.Json.Nodes;

namespace Pace;

public static partial class Providers
{
    public static List<UsageLimit> ParseLimits(string service, JsonObject data, DateTimeOffset now) =>
        service == Services.Claude ? ClaudeLimits(data) : CodexLimits(data, now);

    static List<UsageLimit> ClaudeLimits(JsonObject data)
    {
        var result = new List<UsageLimit>();
        if (ParseClaude(data) is { } week)
            result.Add(new(UsageLimit.WeeklyKey, "Weekly", week));
        void Add(string key, string name, JsonNode? block, string usedField, TimeSpan period)
        {
            if (Number(block?[usedField]) is { } percent && percent >= 0 && result.All(r => r.Key != key))
                result.Add(new(key, name, new(percent, ResetExpiry(block?["resets_at"]), period)));
        }
        Add(UsageLimit.SessionKey, "Five-hour", data["five_hour"], "utilization", FiveHours);
        if (data["limits"] is JsonArray limits)
            foreach (var limit in limits.OfType<JsonObject>())
                if (Text(limit["group"]) == "weekly" && Text(limit["scope"]?["model"]?["display_name"]) is { } model && !string.IsNullOrWhiteSpace(model))
                    Add("model:" + model, model + " · Weekly", limit, "percent", Week);
        Add("model:Opus", "Opus · Weekly", data["seven_day_opus"], "utilization", Week);
        Add("model:Sonnet", "Sonnet · Weekly", data["seven_day_sonnet"], "utilization", Week);
        return result;
    }

    static List<UsageLimit> CodexLimits(JsonObject data, DateTimeOffset now)
    {
        var result = new List<UsageLimit>();
        void AddWindows(JsonNode? block, string prefix, string? name)
        {
            foreach (var (field, window) in CodexWindows(block, now))
            {
                string duration = Math.Abs(window.Period.TotalSeconds - Week.TotalSeconds) < 60 ? "Weekly"
                    : Math.Abs(window.Period.TotalSeconds - FiveHours.TotalSeconds) < 60 ? "Five-hour" : PaceMath.Duration(window.Period);
                result.Add(new(prefix + field, string.IsNullOrWhiteSpace(name) ? duration : name + " · " + duration, window));
            }
        }
        AddWindows(data["rate_limit"] ?? data["rate_limits"], "main:", null);
        if (data["additional_rate_limits"] is JsonArray additional)
            foreach (var item in additional.OfType<JsonObject>())
                AddWindows(item["rate_limit"], "extra:" + Text(item["limit_name"]) + ":", Text(item["limit_name"]));
        return result.OrderBy(r => Math.Abs(r.Window.Period.TotalDays - 7)).ToList();
    }
}
