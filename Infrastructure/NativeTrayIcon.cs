using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Pace;

// Windows shell integration only; no application or view ownership.
[SupportedOSPlatform("windows")]
internal sealed class NativeTrayIcon : IDisposable
{
    const uint Callback = 0x8001, UpdateMessage = 0x8002;
    readonly WindowProcedure procedure;
    readonly Thread thread;
    readonly ManualResetEventSlim ready = new();
    readonly object sync = new();
    readonly uint taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    IntPtr window, icon;
    byte[]? pending;
    bool added;
    public bool Added => added;
    internal int IconError { get; private set; }
    internal int ShellError { get; private set; }
    public event Action? Hovered, Clicked;
    public event Action<int>? MenuSelected;
    public NativeTrayIcon()
    {
        procedure = Dispatch;
        thread = new Thread(Run) { IsBackground = true, Name = "Pace tray" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!ready.Wait(TimeSpan.FromSeconds(5)) || window == IntPtr.Zero) throw new InvalidOperationException("Could not create the tray icon.");
    }
    void Run()
    {
        string name = "Pace.Tray." + Environment.ProcessId;
        var cls = new WindowClass { Size = (uint)Marshal.SizeOf<WindowClass>(), Procedure = Marshal.GetFunctionPointerForDelegate(procedure), Name = name };
        if (RegisterClassEx(ref cls) != 0)
            window = CreateWindowEx(0, name, "Pace tray messages", 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        ready.Set();
        if (window == IntPtr.Zero) return;
        while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0) { TranslateMessage(ref message); DispatchMessage(ref message); }
        UnregisterClass(name, IntPtr.Zero);
    }
    public void Update(byte[] png)
    {
        lock (sync) pending = png;
        PostMessage(window, UpdateMessage, IntPtr.Zero, IntPtr.Zero);
    }
    public PixelRect? Bounds
    {
        get
        {
            var identifier = new IconIdentifier { Size = (uint)Marshal.SizeOf<IconIdentifier>(), Window = window, Id = 1 };
            return ShellNotifyIconGetRect(ref identifier, out var rect) == 0 ? new PixelRect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top) : null;
        }
    }
    IntPtr Dispatch(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam)
    {
        if (message == UpdateMessage || message == taskbarCreated)
        {
            if (message == taskbarCreated) added = false;
            byte[]? data;
            lock (sync) { data = pending; pending = null; }
            if (data != null)
            {
                var next = CreateIconFromResourceEx(data, (uint)data.Length, true, 0x30000, 0, 0, 0);
                IconError = next == IntPtr.Zero ? Marshal.GetLastWin32Error() : 0;
                if (next != IntPtr.Zero) { var previous = icon; icon = next; UpdateShell(); if (previous != IntPtr.Zero) DestroyIcon(previous); }
            }
            else UpdateShell();
        }
        if (message == Callback)
        {
            int notification = (int)(lparam.ToInt64() & 0xffff);
            if (notification is 0x200 or 0x406) Dispatcher.UIThread.Post(() => Hovered?.Invoke());
            if (notification is 0x400 or 0x401) Dispatcher.UIThread.Post(() => Clicked?.Invoke());
            if (notification == 0x7b) ShowMenu();
        }
        if (message == 0x10)
        {
            var data = Data(); ShellNotifyIcon(2, ref data); added = false;
            if (icon != IntPtr.Zero) DestroyIcon(icon);
            DestroyWindow(hwnd); return IntPtr.Zero;
        }
        if (message == 2) { PostQuitMessage(0); return IntPtr.Zero; }
        return DefWindowProc(hwnd, message, wparam, lparam);
    }
    void UpdateShell()
    {
        if (icon == IntPtr.Zero) return;
        var data = Data();
        bool previous = added;
        added = ShellNotifyIcon(added ? 1u : 0u, ref data);
        ShellError = added ? 0 : Marshal.GetLastWin32Error();
        if (added && !previous) { data.Version = 4; ShellNotifyIcon(4, ref data); }
    }
    NotifyData Data() => new() { Size = (uint)Marshal.SizeOf<NotifyData>(), Window = window, Id = 1, Flags = 3, CallbackMessage = Callback, Icon = icon, Tip = "", Info = "", Title = "" };
    void ShowMenu()
    {
        var menu = CreatePopupMenu();
        try
        {
            AppendMenu(menu, 0, 1, "Show Pace"); AppendMenu(menu, 0, 2, "Refresh");
            AppendMenu(menu, 0, 3, "Settings…"); AppendMenu(menu, 0x800, 0, "");
            AppendMenu(menu, 0, 4, "Quit");
            var pointer = DesktopIntegration.Pointer ?? default;
            SetForegroundWindow(window);
            int command = TrackPopupMenu(menu, 0x100 | 0x2, pointer.X, pointer.Y, 0, window, IntPtr.Zero);
            PostMessage(window, 0, IntPtr.Zero, IntPtr.Zero);
            if (command != 0) Dispatcher.UIThread.Post(() => MenuSelected?.Invoke(command));
        }
        finally { DestroyMenu(menu); }
    }
    public void Dispose()
    {
        PostMessage(window, 0x10, IntPtr.Zero, IntPtr.Zero);
        thread.Join(TimeSpan.FromSeconds(2));
        ready.Dispose();
        GC.KeepAlive(procedure);
    }
    delegate IntPtr WindowProcedure(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WindowClass
    {
        public uint Size, Style;
        public IntPtr Procedure;
        public int ClassExtra, WindowExtra;
        public IntPtr Instance, Icon, Cursor, Background;
        public string? Menu, Name;
        public IntPtr SmallIcon;
    }
    [StructLayout(LayoutKind.Sequential)] struct NativeMessage { public IntPtr Window; public uint Message; public IntPtr WParam, LParam; public uint Time; public int X, Y; public uint Private; }
    [StructLayout(LayoutKind.Sequential)] struct IconIdentifier { public uint Size; public IntPtr Window; public uint Id; public Guid Guid; }
    [StructLayout(LayoutKind.Sequential)] struct IconBounds { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct NotifyData
    {
        public uint Size; public IntPtr Window; public uint Id, Flags, CallbackMessage; public IntPtr Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State, StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string Title;
        public uint InfoFlags; public Guid Guid; public IntPtr BalloonIcon;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern ushort RegisterClassEx(ref WindowClass cls);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool UnregisterClass(string name, IntPtr instance);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr CreateWindowEx(uint exstyle, string cls, string caption, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
    [DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr hwnd);
    // The class is registered as Unicode, so its message loop and default procedure must be too.
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetMessage(out NativeMessage message, IntPtr hwnd, uint min, uint max);
    [DllImport("user32.dll")] static extern bool TranslateMessage(ref NativeMessage message);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr DispatchMessage(ref NativeMessage message);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam);
    [DllImport("user32.dll")] static extern void PostQuitMessage(int code);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr CreateIconFromResourceEx(byte[] data, uint size, bool icon, uint version, int width, int height, uint flags);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool AppendMenu(IntPtr menu, uint flags, uint id, string label);
    [DllImport("user32.dll")] static extern int TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr hwnd, IntPtr rect);
    [DllImport("user32.dll")] static extern bool DestroyMenu(IntPtr menu);
    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool ShellNotifyIcon(uint operation, ref NotifyData data);
    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconGetRect")] static extern int ShellNotifyIconGetRect(ref IconIdentifier identifier, out IconBounds bounds);
}
