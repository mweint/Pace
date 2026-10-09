using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pace;

public static partial class Providers
{
    const string ClaudeUsage = "https://api.anthropic.com/api/oauth/usage?cedar_ember=1&skip_spend=1";
    const string CodexUsage = "https://chatgpt.com/backend-api/wham/usage";
    const string CodexResetCredits = "https://chatgpt.com/backend-api/wham/rate-limit-reset-credits";
    static readonly TimeSpan DefaultRetry = TimeSpan.FromMinutes(5), MaxRetry = TimeSpan.FromHours(1);
    static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(20) };
    static readonly Dictionary<string, DateTimeOffset> Cooldowns = LoadCooldowns();
    static readonly Dictionary<string, int> RateLimitStreaks = [];
    static string CooldownFile => Path.Combine(Settings.DirectoryPath, "retry-times.json");

    public static async Task<Reading> Fetch(Account account)
    {
        var now = DateTimeOffset.UtcNow;
        Reading Failed(string message, ConnectionIssue issue = ConnectionIssue.None) => new(account, null, message, now, ConnectionIssue: issue);
        var login = await Task.Run(() => ReadLogin(account.CredentialPath));
        if (login == null)
            return Failed("Sign-in is missing or unreadable. Reconnect in Accounts.", ConnectionIssue.CredentialsMissing);
        if (login.Account.Key != account.Key)
            return Failed("A different account is signed in. Reconnect in Accounts.", ConnectionIssue.AccountChanged);
        if (Cooldowns.TryGetValue(account.Key, out var eligible) && now < eligible)
            return Failed($"Retry in {PaceMath.Duration(eligible - now)} (service rate limit)");
        bool claude = account.Service == Services.Claude;
        try
        {
            using var reply = await Send(claude ? ClaudeUsage : CodexUsage, login);
            if (reply.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retry = SetCooldown(account.Key, reply, now);
                return Failed($"Service rate limit; retry in {PaceMath.Duration(retry - now)}");
            }
            if (reply.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return Failed("Service rejected this sign-in. Reconnect in Accounts.", ConnectionIssue.SignInRejected);
            if (!reply.IsSuccessStatusCode)
                return Failed($"Service returned HTTP {(int)reply.StatusCode}");
            RateLimitStreaks.Remove(account.Key);
            if (await ReadJson(reply) is not { } data)
                return Failed("Service returned an unexpected response");
            if ((claude ? ParseClaude(data) : ParseCodex(data, now)) is not { } weekly)
                return Failed("No weekly allowance in the service response");
            var resets = claude ? ParseClaudeResets(data, now) : ParseCodexResets(data, now);
            string? resetError = null;
            if (!claude && resets?.Count != 0)
                (resets, resetError) = await FetchCodexResets(login, now, resets);
            return new(account, weekly, null, now, resets, resetError, ParseLimits(account.Service, data, now));
        }
        catch (OperationCanceledException)
        {
            return Failed("Request timed out");
        }
        catch (HttpRequestException)
        {
            return Failed("Service could not be reached");
        }
        catch (Exception)
        {
            // Unexpected response shapes must not interrupt the refresh of other accounts.
            return Failed("Usage data could not be read");
        }
    }

    static async Task<(BankedResets? Resets, string? Error)> FetchCodexResets(Login login, DateTimeOffset now, BankedResets? summary)
    {
        string cooldownKey = login.Account.Key + ":reset-credits";
        if (Cooldowns.TryGetValue(cooldownKey, out var eligible) && now < eligible)
            return (summary, "Reset expiry dates are waiting for the service rate limit");
        try
        {
            using var reply = await Send(CodexResetCredits, login);
            if (reply.StatusCode == HttpStatusCode.TooManyRequests)
                SetCooldown(cooldownKey, reply, now);
            if (!reply.IsSuccessStatusCode)
                return (summary, "Reset expiry dates could not be refreshed");
            RateLimitStreaks.Remove(cooldownKey);
            var document = await ReadJson(reply);
            return (document == null ? summary : ParseCodexResets(document, now) ?? summary, null);
        }
        catch (Exception)
        {
            return (summary, "Reset expiry dates could not be refreshed");
        }
    }

    static Login? ReadLogin(string file)
    {
        try
        {
            return OpenLogin(file);
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    static Task<HttpResponseMessage> Send(string endpoint, Login login)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.Access);
        if (login.Workspace != null)
            request.Headers.Add("chatgpt-account-id", login.Workspace);
        else
            request.Headers.Add("anthropic-beta", "oauth-2025-04-20");
        return Client.SendAsync(request);
    }

    static async Task<JsonObject?> ReadJson(HttpResponseMessage reply)
    {
        await using var body = await reply.Content.ReadAsStreamAsync();
        return await JsonNode.ParseAsync(body) as JsonObject;
    }

    static DateTimeOffset SetCooldown(string key, HttpResponseMessage reply, DateTimeOffset now)
    {
        // Without a service retry time, each consecutive 429 doubles the wait.
        int streak = RateLimitStreaks[key] = RateLimitStreaks.GetValueOrDefault(key) + 1;
        var backoff = TimeSpan.FromTicks(Math.Min(DefaultRetry.Ticks << Math.Min(streak - 1, 4), MaxRetry.Ticks));
        var retry = reply.Headers.RetryAfter;
        var eligible = retry?.Date ?? now + (retry?.Delta ?? backoff);
        Cooldowns[key] = eligible;
        SaveCooldowns();
        return eligible;
    }

    static Dictionary<string, DateTimeOffset> LoadCooldowns()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, DateTimeOffset>>(File.ReadAllText(CooldownFile)) ?? [];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    static void SaveCooldowns()
    {
        try
        {
            Directory.CreateDirectory(Settings.DirectoryPath);
            File.WriteAllText(CooldownFile, JsonSerializer.Serialize(Cooldowns));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Retry times are advisory; the next 429 records them again.
        }
    }
}
