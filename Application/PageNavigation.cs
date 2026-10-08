namespace Pace;

// Simultaneous handoffs between separate native windows, with one session monitor.
internal sealed class PageNavigation(UsagePanel panel)
{
    public PageDialog? ActivePage { get; private set; }
    public event Action? Changed;
    public void Show(PageDialog page)
    {
        if (ActivePage != null) { ActivePage.Activate(); page.Close(); return; }
        panel.EditingAccounts = true;
        if (!panel.IsVisible) panel.OpenNearTray();
        ActivePage = page;
        page.Navigating = true;
        page.LeaveRequested += Leave;
        page.OpenPage(panel.AnchorArea);
        panel.Dismiss(navigation: true);
    }
    void Leave(bool all)
    {
        var page = ActivePage;
        if (page == null || page.IsClosing || !page.SaveBeforeLeave()) return;
        page.Navigating = true;
        if (!all)
        {
            panel.OpenNearTray(newSession: false);
            // Keep the outgoing page in front of the overview until its fade completes.
            page.Activate();
        }
        page.ExitPage(all, () =>
        {
            page.LeaveRequested -= Leave;
            ActivePage = null;
            panel.EditingAccounts = false;
            if (!all) panel.Activate();
            Changed?.Invoke();
        });
        if (all) panel.Dismiss();
    }
    public void DismissAll() { if (ActivePage != null) Leave(true); else panel.Dismiss(); }
    public void Shutdown()
    {
        if (ActivePage is { } page) { page.Navigating = true; page.PreviewMode = true; page.SaveBeforeLeave(); page.Close(); ActivePage = null; }
        panel.EditingAccounts = false;
    }
}
