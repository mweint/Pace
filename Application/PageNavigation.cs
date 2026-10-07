namespace Usage;
// Separate native windows share one simultaneous opacity handoff.
internal static class PageNavigation
{
    public static void Show(UsagePanel panel, PageDialog destination)
    {
        bool returning = false;
        destination.Opacity = Motion.Enabled ? 0 : 1;
        EventHandler enter = (_, _) => panel.Dismiss(navigation: true);
        FormClosingEventHandler leave = (_, _) =>
        {
            if (returning || destination.CloseAllRequested)
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
