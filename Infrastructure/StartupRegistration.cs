using Microsoft.Win32;

namespace Pace;

internal static class StartupRegistration
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ApprovalKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    public static bool Enabled
    {
        get
        {
            if (!OperatingSystem.IsWindows()) return false;
            using var run = Registry.CurrentUser.OpenSubKey(RunKey);
            using var approval = Registry.CurrentUser.OpenSubKey(ApprovalKey);
            var state = approval?.GetValue("Pace") as byte[];
            return run?.GetValue("Pace") is string command && command == Command && (state == null || state.Length == 0 || state[0] == 2);
        }
        set
        {
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
}
