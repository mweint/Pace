namespace Pace;

internal static class Program
{
    [STAThread]
    static void Main(string[] args) => BuildApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
    internal static AppBuilder BuildApp() => AppBuilder.Configure<PaceApplication>().UsePlatformDetect();
}
