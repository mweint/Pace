using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace Usage;
// Original implementation. Credentials are read in place; the sign-in applications own renewal.
public static partial class Providers
{
    private static JsonObject LoadDocument(string file, long maximum = 65536)
    {
        using var stream = File.OpenRead(file);
        if (stream.Length > maximum)
            throw new InvalidDataException();
        return JsonNode.Parse(stream) as JsonObject ?? throw new InvalidDataException();
    }

    private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
    private static double? Number(JsonNode? node)
    {
        if (node is not JsonValue value || !value.TryGetValue<double>(out var number) || !double.IsFinite(number))
            return null;
        return number;
    }

    private static JsonObject? DecodeIdentity(string? jwt)
    {
        // Identity hints are unverified JWT claims, used only for display and de-duplication.
        // The service authenticates the access token when usage is requested.
        if (jwt == null)
            return null;
        var pieces = jwt.Split('.');
        if (pieces.Length != 3)
            return null;
        try
        {
            string encoded = pieces[1].Replace('_', '/').Replace('-', '+');
            encoded += new string('=', (4 - encoded.Length % 4) % 4);
            return JsonNode.Parse(Convert.FromBase64String(encoded)) as JsonObject;
        }
        catch
        {
            return null;
        }
    }

    private sealed record Login(Account Account, string Access, string? Workspace);
    private static Login? OpenLogin(string file)
    {
        var document = LoadDocument(file);
        if (Path.GetFileName(file).Equals("auth.json", StringComparison.OrdinalIgnoreCase))
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
            return new(new($"codex:{workspace}:{user}", "Codex", Text(identity?["email"]) ?? "Codex account", file), access, workspace);
        }

        if (!Path.GetFileName(file).Equals(".credentials.json", StringComparison.OrdinalIgnoreCase))
            return null;
        string? claudeAccess = Text(document["claudeAiOauth"]?["accessToken"]);
        if (string.IsNullOrEmpty(claudeAccess))
            return null;
        string directory = Path.GetDirectoryName(file)!;
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string metadataFile = Path.Combine(directory, ".claude.json");
        if (directory.Equals(Path.Combine(home, ".claude"), StringComparison.OrdinalIgnoreCase))
            metadataFile = Path.Combine(home, ".claude.json");
        JsonNode? claudeIdentity = null;
        try
        {
            claudeIdentity = LoadDocument(metadataFile, 20 * 1024 * 1024)["oauthAccount"];
        }
        catch
        {
        }

        string key = Text(claudeIdentity?["accountUuid"]) ?? file;
        string label = Text(claudeIdentity?["emailAddress"]) ?? Path.GetFileName(directory);
        return new(new("claude:" + key, "Claude", label, file), claudeAccess, null);
    }

    public static Account? ReadAccount(string file)
    {
        try
        {
            return OpenLogin(file)?.Account;
        }
        catch
        {
            return null;
        }
    }

    public static List<Account> Discover(Settings settings)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var locations = new List<string>
        {
            Path.Combine(Environment.GetEnvironmentVariable("CODEX_HOME") ?? Path.Combine(home, ".codex"), "auth.json"),
            Path.Combine(Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR") ?? Path.Combine(home, ".claude"), ".credentials.json")
        };
        foreach (string parent in new[]
        {
            home,
            Path.Combine(home, ".config")
        }

        )
        {
            try
            {
                foreach (string folder in Directory.GetDirectories(parent).Order(StringComparer.OrdinalIgnoreCase))
                {
                    if (parent == home && !Path.GetFileName(folder).StartsWith('.'))
                        continue;
                    if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0)
                        continue;
                    locations.Add(Path.Combine(folder, "auth.json"));
                    locations.Add(Path.Combine(folder, ".credentials.json"));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        locations.AddRange(settings.ExtraCredentialPaths);
        var accounts = new Dictionary<string, Account>();
        foreach (string location in locations)
        {
            try
            {
                string file = Path.GetFullPath(location);
                if (!File.Exists(file))
                    continue;
                var login = OpenLogin(file);
                if (login != null && !settings.IsRemoved(login.Account))
                    accounts.TryAdd(login.Account.Key, login.Account);
            }
            catch
            {
            }
        }

        return RememberAccounts(accounts.Values, settings);
    }

    internal static List<Account> RememberAccounts(IEnumerable<Account> discovered, Settings settings)
    {
        var accounts = discovered.Where(a => !settings.IsRemoved(a)).ToDictionary(a => a.Key);
        foreach (var account in accounts.Values)
            settings.For(account);
        foreach (var preference in settings.Accounts)
            if (preference.RememberedAccount is { } remembered && !settings.IsRemoved(remembered))
                accounts.TryAdd(remembered.Key, remembered);
        return accounts.Values.ToList();
    }
}
