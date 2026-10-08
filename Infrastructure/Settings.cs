using System.Text.Json;

namespace Pace;

public sealed class Settings
{
    public bool RelativeResetTime { get; set; }
    public bool AutomaticUpdates { get; set; }
    public string? DismissedUpdateVersion { get; set; }
    public List<Preference> Accounts { get; set; } = [];
    public List<string> ExtraCredentialPaths { get; set; } = [];
    public List<string> RemovedAccountKeys { get; set; } = [];

    public bool IsRemoved(Account account) => RemovedAccountKeys.Contains(account.Key, StringComparer.OrdinalIgnoreCase);
    public void RemoveAccount(Account account)
    {
        if (!IsRemoved(account))
            RemovedAccountKeys.Add(account.Key);
        Accounts.RemoveAll(p => p.Key == account.Key);
        ExtraCredentialPaths.RemoveAll(path => path.Equals(account.CredentialPath, StringComparison.OrdinalIgnoreCase));
    }

    public void RestoreAccount(Account account) => RemovedAccountKeys.RemoveAll(key => key.Equals(account.Key, StringComparison.OrdinalIgnoreCase));
    public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Pace");
    public static string FilePath => Path.Combine(DirectoryPath, "settings.json");

    public static Settings Load()
    {
        string legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UsageMonitor");
        SettingsMigration.MoveDirectory(legacy, DirectoryPath);
        if (!File.Exists(FilePath))
            return new();
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new();
                if (SettingsMigration.RewritePaths(settings, legacy, DirectoryPath))
                    settings.TrySave();
                return settings;
            }
            catch (IOException) when (attempt < 5)
            {
                // Briefly locked by sync or antivirus software; retry before giving up.
                Thread.Sleep(100);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                // Never let the next save silently replace settings that could not be read.
                return new() { preserveExisting = true };
            }
        }
    }

    bool preserveExisting;

    public Preference For(Account a)
    {
        var p = Accounts.FirstOrDefault(p => p.Key == a.Key);
        if (p != null)
        {
            p.RememberedAccount = a;
            return p;
        }
        p = new()
        {
            Key = a.Key,
            RememberedAccount = a,
            Tray = Accounts.Count(x => x.Tray && x.Show) < AccountRules.MaxTrayAccounts
        };
        Accounts.Add(p);
        return p;
    }

    public string DisplayName(Account account)
    {
        var alias = For(account).Alias;
        return string.IsNullOrWhiteSpace(alias) ? account.Label : alias;
    }

    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        if (preserveExisting && File.Exists(FilePath))
        {
            // Throws if the unreadable file cannot be kept aside, so it is never overwritten.
            File.Copy(FilePath, Path.Combine(DirectoryPath, $"settings.unreadable-{DateTime.Now:yyyyMMdd-HHmmss}.json"));
            preserveExisting = false;
        }
        var tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, FilePath, true);
    }

    public bool TrySave()
    {
        try
        {
            Save();
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
