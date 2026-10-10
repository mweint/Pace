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
            portable = new TrayIcon { IsVisible = true, ToolTipText = "Pace" };
            portable.Clicked += (_, _) => Clicked?.Invoke();
            var menu = new NativeMenu();
            foreach (var (label, command) in new[] { ("Show Pace", 1), ("Settings…", 3), ("Quit", 4) })
            {
                var item = new NativeMenuItem(label);
                item.Click += (_, _) => MenuSelected?.Invoke(command);
                menu.Items.Add(item);
            }
            portable.Menu = menu;
        }
    }
    public void Update(List<Reading> readings, Settings? settings = null)
    {
        var png = TrayDrawing.Png(readings, DesktopIntegration.TrayIconSize, settings?.TrayFollowsAnyLimit ?? false);
        if (OperatingSystem.IsWindows()) windows?.Update(png);
        else if (portable != null)
        {
            portable.Icon = new WindowIcon(new MemoryStream(png));
            // Tray hosts there report no hover, so the hover summary becomes the tooltip.
            var now = DateTimeOffset.UtcNow;
            portable.ToolTipText = string.Join('\n', readings.Select(r => $"{settings?.DisplayName(r.Account) ?? r.Account.Label}: {PaceMath.HoverSummary(r, now)}").Prepend("Pace"));
        }
    }
    public void Dispose()
    {
        if (OperatingSystem.IsWindows()) windows?.Dispose();
        portable?.Dispose();
    }
}
