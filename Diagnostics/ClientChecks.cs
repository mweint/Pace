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
            string named = Path.Combine(root, "claude");
            check(Providers.ClaudeFolderVariable(Providers.DefaultClaudeFolder) == null && Providers.ClaudeFolderVariable(named + Path.DirectorySeparatorChar) == named,
                "Claude Code is launched with the default folder unnamed and other folders by full path");
            // Expected names computed with Claude Code's own scheme: sha256 of the raw folder value.
            check(ClaudeKeychain.ServiceName(null) == "Claude Code-credentials" && ClaudeKeychain.ServiceName("/Users/sample/.claude-work") == "Claude Code-credentials-ab6f4779" &&
                ClaudeKeychain.AccountFor("sample.user") == "sample.user" && ClaudeKeychain.AccountFor("Sample User") == "claude-code-user",
                "macOS Keychain item names and accounts match Claude Code's");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
