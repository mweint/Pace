namespace Pace;

internal static class Program
{
    [STAThread]
    static void Main(string[] args) => BuildApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
    // A menu bar app on macOS: no Dock icon (the bundle's LSUIElement covers launches from Finder).
    internal static AppBuilder BuildApp() => AppBuilder.Configure<PaceApplication>().UsePlatformDetect().With(new MacOSPlatformOptions { ShowInDock = false });
}
