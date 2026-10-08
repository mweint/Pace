namespace Usage;

internal static class SettingsMigration
{
    // Rename the existing private data directory; do not copy or discard sign-ins.
    internal static void MoveDirectory(string legacy, string current)
    {
        if (Directory.Exists(legacy) && !Directory.Exists(current))
            Directory.Move(legacy, current);
    }

    internal static bool RewritePaths(Settings settings, string legacy, string current)
    {
        bool changed = false;
        string prefix = Path.GetFullPath(legacy).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string replacement = Path.GetFullPath(current).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string Rewrite(string value)
        {
            string marker = value.StartsWith("claude:", StringComparison.OrdinalIgnoreCase) ? "claude:" : "";
            string path = value[marker.Length..];
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return value;
            changed = true;
            return marker + replacement + path[prefix.Length..];
        }
        settings.ExtraCredentialPaths = settings.ExtraCredentialPaths.Select(Rewrite).ToList();
        settings.RemovedAccountKeys = settings.RemovedAccountKeys.Select(Rewrite).ToList();
        foreach (var account in settings.Accounts)
        {
            account.Key = Rewrite(account.Key);
            if (account.RememberedAccount is { } remembered)
                account.RememberedAccount = remembered with { Key = Rewrite(remembered.Key), CredentialPath = Rewrite(remembered.CredentialPath) };
        }
        return changed;
    }
}
