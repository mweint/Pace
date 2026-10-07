using System.Text.Json.Nodes;

namespace Usage;

public static partial class Providers
{
    static DateTimeOffset? ResetExpiry(JsonNode? value)
    {
        if (DateTimeOffset.TryParse(Text(value), out var date))
            return date;
        if (Number(value) is { } number)
        {
            try
            {
                return number >= 1_000_000_000_000 ? DateTimeOffset.FromUnixTimeMilliseconds((long)number) : DateTimeOffset.FromUnixTimeSeconds((long)number);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        return null;
    }

    public static BankedResets? ParseCodexResets(JsonObject document, DateTimeOffset now)
    {
        var block = document["rate_limit_reset_credits"] ?? document;
        int? count = Number(block?["available_count"]) is { } n ? (int)Math.Clamp(n, 0, 10000) : null;
        List<ResetGrant>? grants = null;
        if (block?["credits"] is JsonArray credits)
        {
            grants = [];
            foreach (var credit in credits.OfType<JsonObject>())
            {
                if (Text(credit["status"]) != "available")
                    continue;
                var expiry = ResetExpiry(credit["expires_at"]);
                if (expiry <= now)
                    continue;
                grants.Add(new(1, expiry));
            }
        }

        return count != null || grants != null ? new(count ?? grants!.Count, grants) : null;
    }

    public static BankedResets? ParseClaudeResets(JsonObject document, DateTimeOffset now)
    {
        if (document["cedar_ember"] is not JsonObject block)
            return null;
        if (block["grants"] is not JsonArray rows)
            return null;
        var grants = new List<ResetGrant>();
        foreach (var row in rows.OfType<JsonObject>())
        {
            int left = (int)Math.Clamp(Number(row["resets_left"]) ?? 0, 0, 10000);
            var expiry = ResetExpiry(row["ends_at"]);
            if (left == 0 || expiry <= now)
                continue;
            grants.Add(new(left, expiry));
        }

        return new(grants.Sum(g => g.Count), grants);
    }

    static async Task<(BankedResets? Resets, string? Error)> FetchCodexResets(Login login, DateTimeOffset now, BankedResets? summary)
    {
        string cooldownKey = login.Account.Key + ":reset-credits";
        if (Cooldowns.TryGetValue(cooldownKey, out var eligible) && now < eligible)
            return (summary, "Reset expiry dates are waiting for the service rate limit");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://chatgpt.com/backend-api/wham/rate-limit-reset-credits");
            request.Headers.Authorization = new("Bearer", login.Access);
            if (login.Workspace != null)
                request.Headers.Add("chatgpt-account-id", login.Workspace);
            using var reply = await Client.SendAsync(request);
            if (reply.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Cooldowns[cooldownKey] = reply.Headers.RetryAfter?.Date ?? now + (reply.Headers.RetryAfter?.Delta ?? TimeSpan.FromMinutes(5));
                SaveCooldowns();
            }

            if (!reply.IsSuccessStatusCode)
                return (summary, "Reset expiry dates could not be refreshed");
            await using var body = await reply.Content.ReadAsStreamAsync();
            var document = await JsonNode.ParseAsync(body) as JsonObject;
            return (document == null ? summary : ParseCodexResets(document, now) ?? summary, null);
        }
        catch
        {
            return (summary, "Reset expiry dates could not be refreshed");
        }
    }
}
