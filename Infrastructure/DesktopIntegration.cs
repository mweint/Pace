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
    // A second launch has the foreground; let the running copy take it to show its panel.
    public static void AllowOtherProcessActivation()
    {
        if (OperatingSystem.IsWindows()) AllowSetForegroundWindow(-1);
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
    // Screen pixels. X11 reports it; Wayland (and XWayland while over another client) does not.
    public static PixelPoint? Pointer
    {
        get
        {
            if (OperatingSystem.IsWindows()) return GetCursorPos(out var point) ? new(point.X, point.Y) : null;
            return IsX11Session ? X11Pointer() : null;
        }
    }
    internal static bool IsX11Session => OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") != "wayland"
        && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));
    static PixelPoint? X11Pointer()
    {
        try
        {
            var display = XOpenDisplay(IntPtr.Zero);
            if (display == IntPtr.Zero) return null;
            try { return XQueryPointer(display, XDefaultRootWindow(display), out _, out _, out int x, out int y, out _, out _, out _) ? new(x, y) : null; }
            finally { XCloseDisplay(display); }
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException) { return null; }
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
    [DllImport("user32.dll")] static extern bool AllowSetForegroundWindow(int processId);
    [DllImport("libX11.so.6")] static extern IntPtr XOpenDisplay(IntPtr name);
    [DllImport("libX11.so.6")] static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport("libX11.so.6")] static extern bool XQueryPointer(IntPtr display, IntPtr window, out IntPtr root, out IntPtr child, out int rootX, out int rootY, out int windowX, out int windowY, out uint mask);
    [DllImport("libX11.so.6")] static extern int XCloseDisplay(IntPtr display);
    [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint action, uint parameter, out int value, uint flags);
}
