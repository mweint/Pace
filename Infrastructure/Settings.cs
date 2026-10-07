using System.Text.Json;

namespace Usage;

public sealed class Settings
{
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

        try
        {
            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new();
            if (SettingsMigration.RewritePaths(settings, legacy, DirectoryPath))
                settings.Save();
            return settings;
        }
        catch
        {
            return new();
        }
    }

    public Preference For(Account a)
    {
        var p = Accounts.FirstOrDefault(p => p.Key == a.Key);
        if (p != null)
            return p;
        p = new()
        {
            Key = a.Key,
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
        var tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, FilePath, true);
    }
}
