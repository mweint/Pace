namespace Usage;

internal static class LifecycleChecks
{
    public static void Run(Action<bool, string> check)
    {
        using var panel = new UsagePanel();
        panel.EditingAccounts = true;
        panel.OpenNearTray();
        using var page = new AccountsDialog([], new Settings(), () => { }, persist: () => { });
        using var shutdown = new System.Windows.Forms.Timer { Interval = 30 };
        shutdown.Tick += (_, _) =>
        {
            shutdown.Stop();
            panel.Dispose();
        };
        shutdown.Start();
        PageNavigation.Show(panel, page);
        check(panel.IsDisposed && !page.Visible, "Disposing the overview during modal navigation closes the page without reopening its owner");
        panel.OpenNearTray(newSession: false);
        panel.Dismiss();
        Application.DoEvents();
        check(panel.IsDisposed && !panel.IsHandleCreated, "Late show and hide requests do not recreate a disposed overview window");
    }
}
