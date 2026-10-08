namespace Usage;
// Separate native windows share one simultaneous opacity handoff.
internal static class PageNavigation
{
    public static void Show(UsagePanel panel, PageDialog destination)
    {
        bool returning = false;
        destination.Opacity = Motion.Enabled ? 0 : 1;
        EventHandler enter = (_, _) => { if (!panel.IsDisposed && !panel.Disposing) panel.Dismiss(navigation: true); };
        FormClosingEventHandler leave = (_, e) =>
        {
            if (returning || destination.CloseAllRequested || panel.IsDisposed || panel.Disposing || e.CloseReason != CloseReason.UserClosing)
                return;
            returning = true;
            panel.OpenNearTray(newSession: false);
        };
        destination.Shown += enter;
        destination.FormClosing += leave;
        try
        {
            destination.ShowDialog(panel);
        }
        finally
        {
            destination.Shown -= enter;
            destination.FormClosing -= leave;
        }
    }
}
