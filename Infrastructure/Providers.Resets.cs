using System.Text.Json.Nodes;

namespace Pace;

public static partial class Providers
{
    const int MaxResetCount = 10_000;

    public static BankedResets? ParseCodexResets(JsonObject document, DateTimeOffset now)
    {
        var block = document["rate_limit_reset_credits"] ?? document;
        int? count = Number(block["available_count"]) is { } n ? (int)Math.Clamp(n, 0, MaxResetCount) : null;
        List<ResetGrant>? grants = null;
        if (block["credits"] is JsonArray credits)
            grants = credits.OfType<JsonObject>()
                .Where(credit => Text(credit["status"]) == "available")
                .Select(credit => ResetExpiry(credit["expires_at"]))
                .Where(expiry => !(expiry <= now))
                .Select(expiry => new ResetGrant(1, expiry)).ToList();
        return count != null || grants != null ? new(count ?? grants!.Count, grants) : null;
    }

    public static BankedResets? ParseClaudeResets(JsonObject document, DateTimeOffset now)
    {
        if (document["cedar_ember"]?["grants"] is not JsonArray rows)
            return null;
        var grants = new List<ResetGrant>();
        foreach (var row in rows.OfType<JsonObject>())
        {
            int left = (int)Math.Clamp(Number(row["resets_left"]) ?? 0, 0, MaxResetCount);
            var expiry = ResetExpiry(row["ends_at"]);
            if (left > 0 && !(expiry <= now))
                grants.Add(new(left, expiry));
        }
        return new(grants.Sum(g => g.Count), grants);
    }
}
