using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace Usage;
// Original implementation. Credentials are read in place; the sign-in applications own renewal.
public static partial class Providers
{
    private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(20)
    };
    private static readonly Dictionary<string, DateTimeOffset> Cooldowns = LoadCooldowns();
    private static string CooldownFile => Path.Combine(Settings.DirectoryPath, "retry-times.json");

    private static Dictionary<string, DateTimeOffset> LoadCooldowns()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, DateTimeOffset>>(File.ReadAllText(CooldownFile)) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static void SaveCooldowns()
    {
        try
        {
            Directory.CreateDirectory(Settings.DirectoryPath);
            File.WriteAllText(CooldownFile, System.Text.Json.JsonSerializer.Serialize(Cooldowns));
        }
        catch
        {
        }
    }

    public static async Task<Reading> Fetch(Account account)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Reading Failed(string message) => new(account, null, message, now);
        if (Cooldowns.TryGetValue(account.Key, out var eligible) && now < eligible)
            return Failed($"Retry in {PaceMath.Duration(eligible - now)} (service rate limit)");
        try
        {
            Login? login = OpenLogin(account.CredentialPath);
            if (login == null || login.Account.Key != account.Key)
                return Failed("Sign-in changed; refresh account discovery");
            string endpoint = account.Service == "Claude" ? "https://api.anthropic.com/api/oauth/usage?cedar_ember=1&skip_spend=1" : "https://chatgpt.com/backend-api/wham/usage";
            using var message = new HttpRequestMessage(HttpMethod.Get, endpoint);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.Access);
            if (login.Workspace != null)
                message.Headers.Add("chatgpt-account-id", login.Workspace);
            else
                message.Headers.Add("anthropic-beta", "oauth-2025-04-20");
            using HttpResponseMessage reply = await Client.SendAsync(message);
            if (reply.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retry = reply.Headers.RetryAfter;
                Cooldowns[account.Key] = retry?.Date ?? now + (retry?.Delta ?? TimeSpan.FromMinutes(5));
                SaveCooldowns();
                return Failed($"Service rate limit; retry in {PaceMath.Duration(Cooldowns[account.Key] - now)}");
            }

            if (reply.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return Failed("Open this account in its CLI to renew sign-in, then refresh");
            if (!reply.IsSuccessStatusCode)
                return Failed($"Service returned HTTP {(int)reply.StatusCode}");
            await using var body = await reply.Content.ReadAsStreamAsync();
            var data = await JsonNode.ParseAsync(body) as JsonObject;
            if (data == null)
                return Failed("Service returned an unexpected response");
            Window? weekly = account.Service == "Claude" ? ParseClaude(data) : ParseCodex(data, now);
            if (weekly == null)
                return Failed("No weekly allowance in the service response");
            var resets = account.Service == "Claude" ? ParseClaudeResets(data, now) : ParseCodexResets(data, now);
            string? resetError = null;
            if (account.Service == "Codex" && resets?.Count != 0)
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
        catch
        {
            return Failed("Sign-in or usage data could not be read");
        }
    }
}
