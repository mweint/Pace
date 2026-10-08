using Microsoft.Win32;

namespace Pace;

internal static class ClientDiscovery
{
    internal static string? Find(string service) => Candidates(service).FirstOrDefault(File.Exists);

    internal static IEnumerable<string> Candidates(string service)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!OperatingSystem.IsWindows())
        {
            yield return Path.Combine(home, ".local", "bin", service.ToLowerInvariant());
            foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                yield return Path.Combine(directory, service.ToLowerInvariant());
            yield break;
        }
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (service == Services.Claude)
        {
            yield return Path.Combine(home, ".local", "bin", "claude.exe");
            yield return Path.Combine(roaming, "npm", "claude.cmd");
            yield return Path.Combine(home, ".claude", "local", "node_modules", ".bin", "claude.cmd");
        }
        else
        {
            yield return Path.Combine(local, "Programs", "OpenAI", "Codex", "bin", "codex.exe");
            yield return Path.Combine(roaming, "npm", "codex.cmd");
        }

        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            // WindowsApps aliases can launch the GUI rather than a login-capable CLI.
            if (Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar)).Equals("WindowsApps", StringComparison.OrdinalIgnoreCase))
                continue;
            yield return Path.Combine(directory, service.ToLowerInvariant() + ".exe");
            yield return Path.Combine(directory, service.ToLowerInvariant() + ".cmd");
        }

        if (service != Services.Codex)
            yield break;
        foreach (string root in new[] { Path.Combine(local, "Programs", "OpenAI", "Codex"), Path.Combine(local, "OpenAI", "Codex") }.Concat(PackageRoots()))
            foreach (string candidate in BundledCodexCandidates(root))
                yield return candidate;
    }

    internal static IEnumerable<string> BundledCodexCandidates(string root)
    {
        // Do not select the desktop GUI executable at the app root.
        yield return Path.Combine(root, "app", "resources", "codex.exe");
        yield return Path.Combine(root, "resources", "codex.exe");
        yield return Path.Combine(root, "bin", "codex.exe");
    }

    static List<string> PackageRoots()
    {
        var roots = new List<string>();
        if (!OperatingSystem.IsWindows()) return roots;
        try
        {
            using var packages = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages");
            foreach (string name in (packages?.GetSubKeyNames() ?? []).Where(name => name.StartsWith("OpenAI.Codex_", StringComparison.OrdinalIgnoreCase)).OrderDescending())
            {
                using var package = packages!.OpenSubKey(name);
                if (package?.GetValue("PackageRootFolder") is string root)
                    roots.Add(root);
            }
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // A standalone CLI can still be used if package registration is inaccessible.
        }
        return roots;
    }
}
