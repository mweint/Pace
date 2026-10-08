using System.Runtime.InteropServices;

namespace Usage;
// A single tray icon using version 4: Windows suppresses its standard tooltip,
// leaving only our nonactivating account hover. No private WinForms reflection.
public sealed class NativeTrayIcon : NativeWindow, IDisposable
{
    const int Callback = 0x8001;
    readonly uint taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    bool visible, added;
    Icon? icon;
    public bool Added => added;
    public Rectangle? Bounds
    {
        get
        {
            var identifier = new IconIdentifier { Size = (uint)Marshal.SizeOf<IconIdentifier>(), Window = Handle, Id = 1 };
            return ShellNotifyIconGetRect(ref identifier, out var bounds) == 0
                ? Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom) : null;
        }
    }
    public bool TooltipSuppressed
    {
        get; private set;
    }
    public ContextMenuStrip? ContextMenuStrip
    {
        get; set;
    }

    public event MouseEventHandler? MouseMove;
    public event MouseEventHandler? MouseClick;
    public NativeTrayIcon() => CreateHandle(new CreateParams { Caption = "Pace tray messages" });
    public bool Visible
    {
        get => visible;
        set
        {
            visible = value;
            Update();
        }
    }

    public Icon? Icon
    {
        get => icon;
        set
        {
            icon = value;
            Update();
        }
    }

    void Update()
    {
        var data = Data();
        if (!visible || icon == null)
        {
            if (added)
                ShellNotifyIcon(2, ref data);
            added = false;
            TooltipSuppressed = false;
            return;
        }

        bool wasAdded = added;
        added = ShellNotifyIcon(added ? 1u : 0u, ref data);
        if (added && !wasAdded)
        {
            data.Version = 4;
            TooltipSuppressed = ShellNotifyIcon(4, ref data);
        }
    }

    NotifyData Data() => new()
    {
        Size = (uint)Marshal.SizeOf<NotifyData>(),
        Window = Handle,
        Id = 1,
        Flags = 3,
        CallbackMessage = Callback,
        Icon = visible ? icon?.Handle ?? IntPtr.Zero : IntPtr.Zero,
        Tip = "",
        Info = "",
        Title = ""
    };
    protected override void WndProc(ref Message m)
    {
        if ((uint)m.Msg == taskbarCreated)
        {
            added = false;
            Update();
        }

        if (m.Msg == Callback)
        {
            int message = (int)(m.LParam.ToInt64() & 0xffff);
            if (message is 0x200 or 0x406)
                MouseMove?.Invoke(this, new MouseEventArgs(MouseButtons.None, 0, 0, 0, 0));
            if (message is 0x400 or 0x401)
                MouseClick?.Invoke(this, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
            if (message == 0x7b && ContextMenuStrip != null)
            {
                SetForegroundWindow(Handle);
                long anchor = m.WParam.ToInt64();
                var point = new Point((short)(anchor & 0xffff), (short)((anchor >> 16) & 0xffff));
                ContextMenuStrip.Show(point);
            }
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        visible = false;
        Update();
        ContextMenuStrip?.Dispose();
        DestroyHandle();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct NotifyData
    {
        public uint Size;
        public IntPtr Window;
        public uint Id, Flags, CallbackMessage;
        public IntPtr Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Tip;
        public uint State, StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Title;
        public uint InfoFlags;
        public Guid Guid;
        public IntPtr BalloonIcon;
    }

    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool ShellNotifyIcon(uint operation, ref NotifyData data);
    [StructLayout(LayoutKind.Sequential)]
    struct IconIdentifier
    {
        public uint Size;
        public IntPtr Window;
        public uint Id;
        public Guid Guid;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct IconBounds
    {
        public int Left, Top, Right, Bottom;
    }
    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconGetRect")]
    static extern int ShellNotifyIconGetRect(ref IconIdentifier identifier, out IconBounds bounds);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr window);
}
