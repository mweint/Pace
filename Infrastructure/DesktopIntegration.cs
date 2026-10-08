using System.Runtime.InteropServices;

namespace Pace;

internal static class DesktopIntegration
{
    public static void ShowStartupError(Window? owner)
    {
        if (!OperatingSystem.IsWindows()) return;
        bool navigating = owner is PageDialog page && page.Navigating;
        if (owner is PageDialog dialog) dialog.Navigating = true;
        try { MessageBox(owner?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero, "Couldn't change Windows startup. Please try again.", "Pace", 0); }
        finally { if (owner is PageDialog restored) restored.Navigating = navigating; }
    }
    public static void PreventActivation(Window window)
    {
        if (!OperatingSystem.IsWindows() || window.TryGetPlatformHandle() is not { } handle) return;
        const int extendedStyle = -20;
        var style = GetWindowLongPtr(handle.Handle, extendedStyle);
        SetWindowLongPtr(handle.Handle, extendedStyle, new IntPtr(style.ToInt64() | 0x08000000));
    }
    public static int TrayIconSize
    {
        get
        {
            if (!OperatingSystem.IsWindows()) return 32;
            uint dpi = GetDpiForWindow(FindWindow("Shell_TrayWnd", null));
            return Math.Max(16, GetSystemMetricsForDpi(49, dpi == 0 ? (uint)UiMetrics.BaseDpi : dpi));
        }
    }
    public static PixelPoint? Pointer
    {
        get
        {
            if (OperatingSystem.IsWindows() && GetCursorPos(out var point)) return new(point.X, point.Y);
            return null;
        }
    }
    public static bool AnimationsEnabled
    {
        get
        {
            if (OperatingSystem.IsWindows() && SystemParametersInfo(GetClientAreaAnimation, 0, out int enabled, 0)) return enabled != 0;
            return true;
        }
    }
    // SPI_GETCLIENTAREAANIMATION: the Windows "Animation effects" accessibility setting.
    const uint GetClientAreaAnimation = 0x1042;
    [StructLayout(LayoutKind.Sequential)] struct NativePoint { public int X, Y; }
    [DllImport("user32.dll")] static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string className, string? title);
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] static extern int GetSystemMetricsForDpi(int index, uint dpi);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);
    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)] static extern int MessageBox(IntPtr owner, string text, string caption, uint flags);
    [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint action, uint parameter, out int value, uint flags);
}
