namespace Pace;

internal static class SettingsChecks
{
    public static void Run(Action<bool, string> Check)
    {
        Check(StartupRegistration.ExecLine("/opt/Pace/Pace") == "\"/opt/Pace/Pace\"" &&
            StartupRegistration.ExecLine(@"/opt/a b/$x`\""100%/Pace") == @"""/opt/a b/\\$x\\`\\\\\\""100%%/Pace""",
            "Autostart entries quote the executable path for the Desktop Entry Exec key");
        string root = Path.Combine(Path.GetTempPath(), "pace-migration-" + Guid.NewGuid().ToString("N"));
        string legacy = Path.Combine(root, "legacy"), current = Path.Combine(root, "current");
        string oldCredential = Path.Combine(legacy, "accounts", "sample", "auth.json");
        string newCredential = Path.Combine(current, "accounts", "sample", "auth.json");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(oldCredential)!);
            File.WriteAllText(oldCredential, "synthetic fixture");
            var settings = new Settings
            {
                ExtraCredentialPaths = [oldCredential, Path.Combine(root, "external", "auth.json")],
                Accounts = [new() { Key = "claude:" + oldCredential, Alias = "Sample", Show = false, Tray = false }],
                RemovedAccountKeys = ["claude:" + oldCredential]
            };
            SettingsMigration.MoveDirectory(legacy, current);
            Check(File.ReadAllText(newCredential) == "synthetic fixture" && !Directory.Exists(legacy),
                "App-name migration preserves managed sign-in files");
            Check(SettingsMigration.RewritePaths(settings, legacy, current) &&
                settings.ExtraCredentialPaths[0] == newCredential &&
                settings.Accounts[0].Key == "claude:" + newCredential &&
                settings.RemovedAccountKeys[0] == "claude:" + newCredential &&
                settings.Accounts[0].Alias == "Sample" && !settings.Accounts[0].Show,
                "Migration updates managed paths and account keys without changing preferences");
            Check(settings.ExtraCredentialPaths[1] == Path.Combine(root, "external", "auth.json") &&
                !SettingsMigration.RewritePaths(settings, legacy, current),
                "Migration leaves external sign-ins unchanged and is idempotent");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
