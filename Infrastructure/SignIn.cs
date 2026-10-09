using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Pace;

public static class SignIn
{
    static readonly string[] ClaudeLogin = ["auth", "login", "--claudeai"];
    static readonly string[] CodexLogin = ["-c", "cli_auth_credentials_store=\"file\"", "login"];
    static readonly string[] ClaudeTokenVariables = ["ANTHROPIC_API_KEY", "ANTHROPIC_AUTH_TOKEN", "CLAUDE_CODE_OAUTH_TOKEN", "CLAUDE_CODE_OAUTH_REFRESH_TOKEN"];
    static readonly string[] CodexTokenVariables = ["OPENAI_API_KEY", "CODEX_API_KEY"];
    // Only links to these hosts are passed on as the sign-in link.
    static readonly string[] SignInHosts = ["claude.ai", "claude.com", "anthropic.com", "openai.com", "chatgpt.com"];
    static readonly Regex Link = new(@"https://[^\s\x00-\x1f""'<>\\]+");

    // Outside Windows, link receives the CLI's sign-in link in case no browser opened.
    public static async Task Begin(string service, Settings settings, string? existingFile = null, Action<string>? link = null, CancellationToken cancel = default)
    {
        string? executable = ClientDiscovery.Find(service);
        if (executable == null)
            throw new InvalidOperationException(service == Services.Claude ? "Install Claude Code, then try again." : (OperatingSystem.IsWindows() ? "Install Codex desktop or the Codex CLI, then try again." : "Install the Codex CLI, then try again."));
        string folder = existingFile == null ? Path.Combine(Settings.DirectoryPath, "accounts", service.ToLowerInvariant(), Guid.NewGuid().ToString("N")) : Path.GetDirectoryName(existingFile)!;
        Directory.CreateDirectory(folder);
        string file = existingFile ?? Path.Combine(folder, service == Services.Claude ? ".credentials.json" : "auth.json");
        if (!settings.ExtraCredentialPaths.Contains(file, StringComparer.OrdinalIgnoreCase))
            settings.ExtraCredentialPaths.Add(file);
        settings.Save();
        var start = new ProcessStartInfo(OperatingSystem.IsWindows() ? "powershell.exe" : executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (OperatingSystem.IsWindows())
        {
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-NonInteractive");
            start.ArgumentList.Add("-Command");
            // Literal paths: apostrophes are doubled, with no interpolation of user input.
            string literal = "'" + executable.Replace("'", "''") + "'";
            start.ArgumentList.Add($"$ErrorActionPreference = 'Stop'; & {literal} {LoginArguments(service)}; exit $LASTEXITCODE");
        }
        else
            foreach (string argument in service == Services.Claude ? ClaudeLogin : CodexLogin)
                start.ArgumentList.Add(argument);
        start.Environment[service == Services.Claude ? "CLAUDE_CONFIG_DIR" : "CODEX_HOME"] = folder;
        // This login is for a subscription, not an inherited API-key or managed-token session.
        foreach (string variable in service == Services.Claude ? ClaudeTokenVariables : CodexTokenVariables)
            start.Environment.Remove(variable);
        try
        {
            await Run(start, OperatingSystem.IsWindows() ? null : link, cancel);
            var account = Providers.ReadAccount(file) ?? throw new InvalidOperationException("Sign-in finished without a readable account. Please try again.");
            settings.RestoreAccount(account);
            settings.Save();
        }
        catch when (existingFile == null)
        {
            Discard(settings, folder, file);
            throw;
        }
    }

    // An unfinished new sign-in leaves no account folder or credential path behind.
    static void Discard(Settings settings, string folder, string file)
    {
        settings.ExtraCredentialPaths.RemoveAll(path => path.Equals(file, StringComparison.OrdinalIgnoreCase));
        settings.TrySave();
        try { Directory.Delete(folder, true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    internal static async Task Run(ProcessStartInfo start, Action<string>? link, CancellationToken cancel)
    {
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not open sign-in.");
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        limit.CancelAfter(AppTiming.SignInTimeoutMilliseconds);
        int reported = 0;
        // Drain both streams so the hidden CLI cannot block on a full output pipe. Login output
        // may contain sensitive data: it is only scanned for the sign-in link, never kept or logged.
        async Task Scan(StreamReader reader)
        {
            try
            {
                while (await reader.ReadLineAsync() is { } line)
                    if (link != null && SignInLink(line) is { } found && Interlocked.Exchange(ref reported, 1) == 0)
                        Dispatcher.UIThread.Post(() => link(found));
            }
            catch (Exception e) when (e is IOException or ObjectDisposedException) { }
        }
        var output = Task.WhenAll(Scan(process.StandardOutput), Scan(process.StandardError));
        try { await process.WaitForExitAsync(limit.Token); }
        catch (OperationCanceledException)
        {
            await Stop(process);
            cancel.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Sign-in timed out. Try adding the account again.");
        }
        // A browser the CLI started can inherit its output, so don't wait on it for long.
        await Task.WhenAny(output, Task.Delay(AppTiming.SignInOutputGraceMilliseconds));
        if (process.ExitCode != 0)
            throw new InvalidOperationException("Sign-in did not complete. Try adding the account again.");
    }

    // Windows ends the PowerShell wrapper and the CLI; browsers opened through the shell are
    // not in that tree. Elsewhere the CLI is asked to stop first (npm launchers forward the
    // signal to the native binary), which leaves a browser it started directly running.
    static async Task Stop(Process process)
    {
        try
        {
            if (!OperatingSystem.IsWindows() && Kill(process.Id, Terminate) == 0)
            {
                using var grace = new CancellationTokenSource(AppTiming.SignInStopGraceMilliseconds);
                try { await process.WaitForExitAsync(grace.Token); return; }
                catch (OperationCanceledException) { }
            }
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { }
    }

    internal static string? SignInLink(string line)
    {
        foreach (Match match in Link.Matches(line))
        {
            string candidate = match.Value.TrimEnd('.', ',', ';', ')', ']');
            if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && SignInHosts.Any(host => uri.Host == host || uri.Host.EndsWith("." + host, StringComparison.Ordinal)))
                return candidate;
        }
        return null;
    }

    const int Terminate = 15;
    [DllImport("libc", EntryPoint = "kill", SetLastError = true)] static extern int Kill(int processId, int signal);

    // Only Pace's isolated account folder uses file storage; existing client settings stay intact.
    internal static string LoginArguments(string service) => service == Services.Claude
        ? "auth login --claudeai" : "-c 'cli_auth_credentials_store=\"file\"' login";
}
