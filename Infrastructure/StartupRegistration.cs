using Microsoft.Win32;

namespace Pace;

// Windows uses the per-user Run key; Linux desktops use an XDG autostart entry.
internal static class StartupRegistration
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ApprovalKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    public static bool Supported => OperatingSystem.IsWindows() || OperatingSystem.IsLinux();
    public static bool Enabled
    {
        get
        {
            if (OperatingSystem.IsLinux()) return AutostartEnabled();
            if (!OperatingSystem.IsWindows()) return false;
            using var run = Registry.CurrentUser.OpenSubKey(RunKey);
            using var approval = Registry.CurrentUser.OpenSubKey(ApprovalKey);
            var state = approval?.GetValue("Pace") as byte[];
            return run?.GetValue("Pace") is string command && command == Command && (state == null || state.Length == 0 || state[0] == 2);
        }
        set
        {
            if (OperatingSystem.IsLinux()) { SetAutostart(value); return; }
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Startup integration has not been ported to this desktop yet.");
            using var run = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value)
            {
                run.SetValue("Pace", Command);
                using var approval = Registry.CurrentUser.OpenSubKey(ApprovalKey, true);
                if (approval?.GetValue("Pace") != null)
                    approval.SetValue("Pace", new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);
            }
            else run.DeleteValue("Pace", false);
        }
    }
    static string Command => $"\"{Environment.ProcessPath}\"";

    internal static string AutostartFile
    {
        get
        {
            string? config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrEmpty(config) || !Path.IsPathRooted(config))
                config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            return Path.Combine(config, "autostart", "pace.desktop");
        }
    }
    // An AppImage runs from a temporary mount; autostart must launch the image itself.
    static string Executable => Environment.GetEnvironmentVariable("APPIMAGE") is { Length: > 0 } image ? image : Environment.ProcessPath!;
    // Desktop Entry Exec quoting: reserved characters are backslash-escaped inside double quotes,
    // the entry's string escaping then doubles each backslash, and % is written as %%.
    internal static string ExecLine(string path) =>
        "\"" + path.Replace("\\", "\\\\\\\\").Replace("\"", "\\\\\"").Replace("`", "\\\\`").Replace("$", "\\\\$").Replace("%", "%%") + "\"";

    static bool AutostartEnabled()
    {
        try
        {
            var lines = File.ReadAllLines(AutostartFile);
            return lines.Contains("Exec=" + ExecLine(Executable)) && !lines.Contains("Hidden=true") && !lines.Contains("X-GNOME-Autostart-enabled=false");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
    }

    static void SetAutostart(bool enabled)
    {
        string file = AutostartFile;
        if (!enabled) { File.Delete(file); return; }
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, string.Join('\n',
            "[Desktop Entry]", "Type=Application", "Name=Pace", "Comment=Claude and Codex usage in the system tray",
            "Exec=" + ExecLine(Executable), "Icon=pace", "Terminal=false", "X-GNOME-Autostart-enabled=true", ""));
    }
}
