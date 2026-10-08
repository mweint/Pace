namespace Pace;

internal static class ClientChecks
{
    public static void Run(Action<bool, string> check)
    {
        string root = Path.Combine(Path.GetTempPath(), "pace-client-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "Codex.exe"), "demo GUI");
            check(!ClientDiscovery.BundledCodexCandidates(root).Any(File.Exists), "Desktop GUI executables are not mistaken for login CLIs");
            string resources = Path.Combine(root, "app", "resources");
            Directory.CreateDirectory(resources);
            string cli = Path.Combine(resources, "codex.exe");
            File.WriteAllText(cli, "demo CLI");
            check(ClientDiscovery.BundledCodexCandidates(root).FirstOrDefault(File.Exists) == cli, "Desktop-bundled Codex CLI is discovered independently of PATH");
            check(SignIn.LoginArguments("Codex").Contains("cli_auth_credentials_store=\"file\"") && SignIn.LoginArguments("Claude") == "auth login --claudeai", "Official login commands use readable isolated credentials without implementing OAuth");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
