using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Simple;
using Avalonia.Controls.Primitives;

namespace Pace;

public sealed class PaceApplication : Avalonia.Application
{
    TrayApp? coordinator;
    SingleInstance? instance;
    public override void Initialize()
    {
        Name = "Pace";
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        Styles.Add(new SimpleTheme());
        // ToolTip's outer chrome is themed as well as its content.
        var tip = new Avalonia.Styling.Style(x => x.OfType<ToolTip>());
        tip.Setters.Add(new Avalonia.Styling.Setter(TemplatedControl.BackgroundProperty, Palette.Brush(Palette.Background)));
        tip.Setters.Add(new Avalonia.Styling.Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
        tip.Setters.Add(new Avalonia.Styling.Setter(TemplatedControl.PaddingProperty, new Thickness(0)));
        Styles.Add(tip);
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? [];
            if (args.Any(a => a is "--render-preview" or "--self-test" or "--live-check"))
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        if (args.Contains("--render-preview")) await Preview.Render(args.Last());
                        else if (args.Contains("--self-test")) await SelfTests.Run(args.Last());
                        else await LiveCheck.Run(args.Last());
                    }
                    catch (Exception e)
                    {
                        File.WriteAllText(args.Last() + ".error.txt", e.ToString()); Environment.ExitCode = 1;
                    }
                    desktop.Shutdown(Environment.ExitCode);
                });
            }
            else
            {
                instance = SingleInstance.Acquire();
                if (instance == null || (Settings.Load().AutomaticUpdates && UpdateInstaller.Apply()))
                {
                    instance?.Dispose();
                    Dispatcher.UIThread.Post(() => desktop.Shutdown());
                }
                else
                {
                    // A tray app should survive an unexpected error in one refresh or view.
                    Dispatcher.UIThread.UnhandledException += (_, e) => { ErrorLog.Write(e.Exception); e.Handled = true; };
                    TaskScheduler.UnobservedTaskException += (_, e) => { ErrorLog.Write(e.Exception); e.SetObserved(); };
                    coordinator = new(desktop);
                    instance.Listen(coordinator.Show);
                    desktop.Exit += (_, _) => { coordinator.Dispose(); instance.Dispose(); };
                }
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
