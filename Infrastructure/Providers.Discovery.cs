using System.Text.Json.Nodes;

namespace Pace;

// Credentials are read in place; the official sign-in applications own renewal.
public static partial class Providers
{
    const long CredentialFileLimit = 64 * 1024, ClaudeMetadataLimit = 20 * 1024 * 1024;

    sealed record Login(Account Account, string Access, string? Workspace);

    // File scanning runs off the UI thread; settings are only touched on the caller's thread.
    public static async Task<List<Account>> Discover(Settings settings)
    {
        var extraPaths = settings.ExtraCredentialPaths.ToList();
        var found = await Task.Run(() => FindAccounts(extraPaths));
        return RememberAccounts(found, settings);
    }

    public static Account? ReadAccount(string file)
    {
        try
        {
            return OpenLogin(file)?.Account;
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            return null;
        }
    }

    internal static List<Account> RememberAccounts(IEnumerable<Account> discovered, Settings settings)
    {
        var accounts = new Dictionary<string, Account>();
        foreach (var account in discovered.Where(a => !settings.IsRemoved(a)))
            accounts.TryAdd(account.Key, account);
        foreach (var account in accounts.Values)
            settings.For(account);
        foreach (var preference in settings.Accounts)
            if (preference.RememberedAccount is { } remembered && !settings.IsRemoved(remembered))
                accounts.TryAdd(remembered.Key, remembered);
        return accounts.Values.ToList();
    }

    static List<Account> FindAccounts(IEnumerable<string> extraPaths)
    {
        var accounts = new List<Account>();
        foreach (string location in CredentialLocations().Concat(extraPaths))
        {
            try
            {
                string file = Path.GetFullPath(location);
                if (File.Exists(file) && OpenLogin(file) is { } login)
                    accounts.Add(login.Account);
            }
            catch (Exception e) when (IsUnreadable(e))
            {
                // Unreadable or foreign files are not sign-ins.
            }
        }
        return accounts;
    }

    // Default client folders, then auth files in dot-folders of home and ~/.config.
    static IEnumerable<string> CredentialLocations()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(Environment.GetEnvironmentVariable("CODEX_HOME") ?? Path.Combine(home, ".codex"), "auth.json");
        yield return Path.Combine(Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR") ?? Path.Combine(home, ".claude"), ".credentials.json");
        foreach (string parent in new[] { home, Path.Combine(home, ".config") })
        {
            string[] folders;
            try
            {
                folders = Directory.GetDirectories(parent);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                continue;
            }
            foreach (string folder in folders.Order(StringComparer.OrdinalIgnoreCase))
            {
                if (parent == home && !Path.GetFileName(folder).StartsWith('.'))
                    continue;
                try
                {
                    if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0)
                        continue;
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    continue;
                }
                yield return Path.Combine(folder, "auth.json");
                yield return Path.Combine(folder, ".credentials.json");
            }
        }
    }

    static Login? OpenLogin(string file) => Path.GetFileName(file).ToLowerInvariant() switch
    {
        "auth.json" => OpenCodexLogin(file, LoadDocument(file)),
        ".credentials.json" => OpenClaudeLogin(file, LoadDocument(file)),
        _ => null
    };

    static Login? OpenCodexLogin(string file, JsonObject document)
    {
        var credentials = document["tokens"];
        var identity = DecodeIdentity(Text(credentials?["id_token"]));
        var metadata = identity?["https://api.openai.com/auth"];
        if (metadata == null)
            return null;
        string? workspace = Text(credentials?["account_id"]) ?? Text(metadata["chatgpt_account_id"]);
        string? access = Text(credentials?["access_token"]);
        if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(access))
            return null;
        string user = Text(metadata["chatgpt_user_id"]) ?? Text(identity?["sub"]) ?? "";
        var account = new Account($"codex:{workspace}:{user}", Services.Codex, Text(identity?["email"]) ?? "Codex account", file);
        return new(account, access, workspace);
    }

    static Login? OpenClaudeLogin(string file, JsonObject document)
    {
        string? access = Text(document["claudeAiOauth"]?["accessToken"]);
        if (string.IsNullOrEmpty(access))
            return null;
        // Account identity lives beside the credentials, or in ~/.claude.json for the default folder.
        string directory = Path.GetDirectoryName(file)!;
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string metadataFile = directory.Equals(Path.Combine(home, ".claude"), StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(home, ".claude.json") : Path.Combine(directory, ".claude.json");
        JsonNode? identity = null;
        try
        {
            identity = LoadDocument(metadataFile, ClaudeMetadataLimit)["oauthAccount"];
        }
        catch (Exception e) when (IsUnreadable(e))
        {
            // Without metadata the credential path identifies the account.
        }
        string key = Text(identity?["accountUuid"]) ?? file;
        string label = Text(identity?["emailAddress"]) ?? Path.GetFileName(directory);
        return new(new Account("claude:" + key, Services.Claude, label, file), access, null);
    }

    static JsonObject? DecodeIdentity(string? jwt)
    {
        // Unverified JWT claims, used only for display and de-duplication.
        // The service authenticates the access token when usage is requested.
        var pieces = jwt?.Split('.');
        if (pieces is not { Length: 3 })
            return null;
        try
        {
            string encoded = pieces[1].Replace('_', '/').Replace('-', '+');
            encoded += new string('=', (4 - encoded.Length % 4) % 4);
            return JsonNode.Parse(Convert.FromBase64String(encoded)) as JsonObject;
        }
        catch (Exception e) when (e is FormatException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    static JsonObject LoadDocument(string file, long maximum = CredentialFileLimit)
    {
        using var stream = File.OpenRead(file);
        if (stream.Length > maximum)
            throw new InvalidDataException();
        return JsonNode.Parse(stream) as JsonObject ?? throw new InvalidDataException();
    }

    static bool IsUnreadable(Exception e) =>
        e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidDataException
            or InvalidOperationException or ArgumentException or NotSupportedException;

    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    static double? Number(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<double>(out var number) && double.IsFinite(number) ? number : null;
}
