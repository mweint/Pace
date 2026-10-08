namespace Pace;

// Shared tray facade. Windows retains the custom nonactivating hover; other desktops use Avalonia's backend.
internal sealed class TrayService : IDisposable
{
    readonly NativeTrayIcon? windows;
    readonly TrayIcon? portable;
    public event Action? Clicked, Hovered;
    public event Action<int>? MenuSelected;
    public PixelRect? Bounds => OperatingSystem.IsWindows() ? windows?.Bounds : null;
    public TrayService()
    {
        if (OperatingSystem.IsWindows())
        {
            windows = new();
            windows.Clicked += () => Clicked?.Invoke();
            windows.Hovered += () => Hovered?.Invoke();
            windows.MenuSelected += command => MenuSelected?.Invoke(command);
        }
        else
        {
            portable = new TrayIcon { IsVisible = true, ToolTipText = "" };
            portable.Clicked += (_, _) => Clicked?.Invoke();
            var menu = new NativeMenu();
            foreach (var (label, command) in new[] { ("Show Pace", 1), ("Refresh", 2), ("Settings…", 3), ("Quit", 4) })
            {
                var item = new NativeMenuItem(label);
                item.Click += (_, _) => MenuSelected?.Invoke(command);
                menu.Items.Add(item);
            }
            portable.Menu = menu;
        }
    }
    public void Update(List<Reading> readings)
    {
        var png = TrayDrawing.Png(readings, DesktopIntegration.TrayIconSize);
        if (OperatingSystem.IsWindows()) windows?.Update(png);
        else if (portable != null) portable.Icon = new WindowIcon(new MemoryStream(png));
    }
    public void Dispose()
    {
        if (OperatingSystem.IsWindows()) windows?.Dispose();
        portable?.Dispose();
    }
}
