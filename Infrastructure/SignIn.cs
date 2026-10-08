using System.Diagnostics;

namespace Usage;

public static class SignIn
{
    public static async Task Begin(string service, Settings settings, string? existingFile = null)
    {
        string? executable = ClientDiscovery.Find(service);
        if (executable == null)
            throw new InvalidOperationException(service == "Claude" ? "Install Claude Code, then try again." : "Install Codex desktop or the Codex CLI, then try again.");
        string folder = existingFile == null ? Path.Combine(Settings.DirectoryPath, "accounts", service.ToLowerInvariant(), Guid.NewGuid().ToString("N")) : Path.GetDirectoryName(existingFile)!;
        Directory.CreateDirectory(folder);
        string file = existingFile ?? Path.Combine(folder, service == "Claude" ? ".credentials.json" : "auth.json");
        if (!settings.ExtraCredentialPaths.Contains(file, StringComparer.OrdinalIgnoreCase))
            settings.ExtraCredentialPaths.Add(file);
        settings.Save();
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-Command");
        // Paths are literal PowerShell strings; apostrophes are doubled, with no interpolation of user input.
        string literal = "'" + executable.Replace("'", "''") + "'";
        start.ArgumentList.Add($"$ErrorActionPreference = 'Stop'; & {literal} {LoginArguments(service)}; exit $LASTEXITCODE");
        start.Environment[service == "Claude" ? "CLAUDE_CONFIG_DIR" : "CODEX_HOME"] = folder;
        // This login is for a subscription, not an inherited API-key or managed-token session.
        foreach (string variable in service == "Claude" ? new[]
        {
            "ANTHROPIC_API_KEY",
            "ANTHROPIC_AUTH_TOKEN",
            "CLAUDE_CODE_OAUTH_TOKEN",
            "CLAUDE_CODE_OAUTH_REFRESH_TOKEN"
        }

        : new[]
        {
            "OPENAI_API_KEY",
            "CODEX_API_KEY"
        }

        )
            start.Environment.Remove(variable);
        var process = Process.Start(start) ?? throw new InvalidOperationException("Could not open sign-in.");
        await Finish(process);
        var account = Providers.ReadAccount(file) ?? throw new InvalidOperationException("Sign-in finished without a readable account. Please try again.");
        settings.RestoreAccount(account);
        settings.Save();
    }

    static async Task Finish(Process p)
    {
        using (p)
        {
            // Drain both streams so the hidden CLI cannot block on a full output pipe.
            // Login output may contain sensitive data; discard it rather than logging it.
            await Task.WhenAll(p.StandardOutput.BaseStream.CopyToAsync(Stream.Null), p.StandardError.BaseStream.CopyToAsync(Stream.Null), p.WaitForExitAsync());
            if (p.ExitCode != 0)
                throw new InvalidOperationException("Sign-in did not complete. Try adding the account again.");
        }
    }

    // Only Pace's isolated account folder uses file storage; existing client settings stay intact.
    internal static string LoginArguments(string service) => service == "Claude"
        ? "auth login --claudeai" : "-c 'cli_auth_credentials_store=\"file\"' login";
}
