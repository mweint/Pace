using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Pace;

// Claude Code on macOS keeps each sign-in in the login Keychain, using .credentials.json only
// as a fallback. It writes the item with Apple's security tool, so reading it with the same tool
// needs no Keychain prompt. The item holds the same JSON document as the file; it is returned
// to the caller only, never logged.
internal static class ClaudeKeychain
{
    const string Tool = "/usr/bin/security", Service = "Claude Code-credentials", FallbackAccount = "claude-code-user";
    static readonly Regex PlainName = new("^[a-zA-Z0-9._-]+$");

    public static bool Supported => OperatingSystem.IsMacOS();

    // The sign-in for a Claude Code folder, or null when there is none or the Keychain is unavailable.
    public static string? Read(string folder)
    {
        var start = new ProcessStartInfo(Tool)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (string argument in new[] { "find-generic-password", "-a", Account, "-w", "-s", ServiceFor(folder) })
            start.ArgumentList.Add(argument);
        try
        {
            using var process = Process.Start(start);
            if (process == null) return null;
            var output = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();
            // A locked Keychain asks the user to unlock it; give up rather than wait indefinitely.
            if (!process.WaitForExit(AppTiming.KeychainReadTimeoutMilliseconds))
            {
                process.Kill();
                return null;
            }
            return process.ExitCode == 0 && output.Result.Trim() is { Length: > 0 } text ? text : null;
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
    }

    // Claude Code launched for this folder sees CLAUDE_CONFIG_DIR as Pace sets it (or inherits it).
    internal static string ServiceFor(string folder) =>
        ServiceName(Providers.ClaudeFolderVariable(folder) ?? Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR"));

    // A set CLAUDE_CONFIG_DIR adds a hash of its raw value (in NFC) to the item name.
    internal static string ServiceName(string? configFolder) => string.IsNullOrEmpty(configFolder) ? Service
        : $"{Service}-{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(configFolder.Normalize(NormalizationForm.FormC))))[..8]}";

    // The Keychain account Claude Code uses: the login name when it is a plain name.
    internal static string Account => AccountFor(Environment.GetEnvironmentVariable("USER") is { Length: > 0 } user ? user : Environment.UserName);
    internal static string AccountFor(string name) => PlainName.IsMatch(name) ? name : FallbackAccount;
}
