using System.Diagnostics;

namespace Usage;

public static class SignIn
{
    public static async Task Begin(string service, Settings settings, string? existingFile = null)
    {
        string? executable = FindExecutable(service);
        if (executable == null)
            throw new InvalidOperationException($"Install the {service} CLI first, then try again.");
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
        start.ArgumentList.Add($"$ErrorActionPreference = 'Stop'; & {literal} {(service == "Claude" ? "auth login --claudeai" : "login")}; exit $LASTEXITCODE");
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
        if (Providers.ReadAccount(file) is { } account)
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

    static string? FindExecutable(string service)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var known = service == "Claude" ? new[]
        {
            Path.Combine(home, ".local", "bin", "claude.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "claude.cmd")
        }

        : new[]
        {
            Path.Combine(local, "Programs", "OpenAI", "Codex", "bin", "codex.exe")
        };
        foreach (string file in known)
            if (File.Exists(file))
                return file;
        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            foreach (string extension in new[]
            {
                ".exe",
                ".cmd"
            }

            )
            {
                string path = Path.Combine(directory, service.ToLowerInvariant() + extension);
                if (File.Exists(path))
                    return path;
            }

        return null;
    }
}
