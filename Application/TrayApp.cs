using Avalonia.Controls.ApplicationLifetimes;

namespace Pace;

public sealed class TrayApp : IDisposable
{
    readonly IClassicDesktopStyleApplicationLifetime lifetime;
    readonly TrayService tray = new();
    readonly UsagePanel panel = new();
    readonly TrayHover hover = new();
    readonly PageNavigation navigation;
    readonly Settings settings = Settings.Load();
    readonly AppUpdates updates;
    readonly SignInFlow signIn = new();
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(AppTiming.PaceRefreshMilliseconds) };
    readonly DispatcherTimer usageTimer = new() { Interval = TimeSpan.FromMilliseconds(AppTiming.UsageRefreshMilliseconds) };
    readonly DispatcherTimer updateTimer = new() { Interval = TimeSpan.FromMilliseconds(AppTiming.UpdateCheckMilliseconds) };
    readonly DispatcherTimer hoverTimer = new() { Interval = TimeSpan.FromMilliseconds(AppTiming.HoverPollMilliseconds) };
    List<Reading> readings = [];
    Task? refreshing;
    bool busy, refreshQueued, exiting, hoverPending;
    long hoverStarted, suppressHoverUntil;
    PixelPoint hoverAnchor;
    PixelRect hoverBounds;
    PixelPoint? trayClick;
    public TrayApp(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        this.lifetime = lifetime;
        navigation = new(panel);
        updates = new(settings);
        updates.Changed += () => { if (!exiting) panel.UpdateNotification(updates.Notify); };
        navigation.Changed += Render;
        panel.LocateTray = () => tray.Bounds ?? (TrayPointer() is { } pointer ? new PixelRect(pointer, new PixelSize(1, 1)) : null);
        tray.Update([]);
        tray.Clicked += () =>
        {
            HideHover();
            if (!OperatingSystem.IsWindows() && DesktopIntegration.Pointer is { } pointer && OnPanel(pointer)) trayClick = pointer;
            if (navigation.ActivePage != null) navigation.DismissAll();
            else if (TrayToggle.ShouldClose(panel.IsVisible, panel.LastAutoHide, Environment.TickCount64)) panel.Dismiss();
            else { panel.OpenNearTray(); RefreshIfStale(); }
        };
        tray.MenuSelected += command =>
        {
            HideHover();
            switch (command)
            {
                case 1: Show(); break;
                case 3: EditAccounts(); break;
                case 4: lifetime.Shutdown(); break;
            }
        };
        tray.Hovered += () =>
        {
            if (hoverPending || panel.IsVisible || navigation.ActivePage != null || Environment.TickCount64 < suppressHoverUntil) return;
            if (DesktopIntegration.Pointer is not { } pointer) return;
            hoverStarted = Environment.TickCount64; hoverAnchor = pointer;
            hoverBounds = tray.Bounds ?? new PixelRect(pointer, new PixelSize(0, 0));
            hoverPending = true;
        };
        hoverTimer.Tick += (_, _) =>
        {
            if (!hoverPending || DesktopIntegration.Pointer is not { } pointer) return;
            bool near = Math.Abs(pointer.X - hoverAnchor.X) <= UiMetrics.HoverPointerTolerance && Math.Abs(pointer.Y - hoverAnchor.Y) <= UiMetrics.HoverPointerTolerance;
            var hoverRect = new PixelRect(hover.Position, new PixelSize((int)(hover.Width * hover.RenderScaling), (int)(hover.Height * hover.RenderScaling)));
            if (panel.IsVisible || navigation.ActivePage != null || (!near && !hoverRect.Contains(pointer))) { HideHover(); return; }
            if (!hover.IsVisible && Environment.TickCount64 - hoverStarted >= UiMetrics.HoverDelayMilliseconds) hover.Open(hoverBounds);
        };
        panel.RefreshRequested += async () => await Refresh();
        panel.SettingsRequested += () => EditAccounts();
        panel.ManageAccountsRequested += () => EditAccounts(false);
        panel.AccountRequested += OpenDetails;
        panel.AddAccountRequested += async service => await AddAccount(service);
        panel.CancelSignInRequested += signIn.Cancel;
        signIn.Changed += () =>
        {
            if (exiting) return;
            if (signIn.Active) panel.UpdateSignIn(true, "Complete sign-in in your browser…", signIn.Link);
            else panel.UpdateSignIn(false, "");
        };
        timer.Tick += (_, _) => Render();
        usageTimer.Tick += async (_, _) => await Refresh();
        updateTimer.Tick += async (_, _) => await updates.Check();
        timer.Start(); usageTimer.Start(); updateTimer.Start(); hoverTimer.Start();
        _ = Refresh(); _ = updates.Check();
        _ = OpenAtStartup();
    }
    // The Windows icon is added on the tray thread; opening before then would anchor the panel
    // at the pointer instead of the icon, and the first click would move it.
    async Task OpenAtStartup()
    {
        for (int i = 0; OperatingSystem.IsWindows() && tray.Bounds == null && i < AppTiming.TrayReadyChecks; i++)
            await Task.Delay(AppTiming.TrayReadyPollMilliseconds);
        if (!exiting && !panel.IsVisible && navigation.ActivePage == null) panel.OpenNearTray();
    }
    // Where the tray icon is when its bounds are unknown. Windows uses the pointer. Elsewhere only
    // X11 reports it, and only a click that landed on a panel (not the work area) is trusted;
    // the last such click also places later openings from the menu or a second launch.
    PixelPoint? TrayPointer() => OperatingSystem.IsWindows() ? DesktopIntegration.Pointer : trayClick is { } click && OnPanel(click) ? click : null;
    bool OnPanel(PixelPoint point) => panel.Screens.ScreenFromPoint(point) is { } screen && PopupAnchor.OnPanel(screen.Bounds, screen.WorkingArea, point);
    // The tray menu's Show Pace, and a second launch of Pace.
    public void Show()
    {
        if (exiting) return;
        HideHover();
        if (navigation.ActivePage is { } page) page.Activate(); else { panel.OpenNearTray(); RefreshIfStale(); }
    }
    void HideHover() { hover.Hide(); hoverPending = false; suppressHoverUntil = Environment.TickCount64 + AppTiming.HoverSuppressMilliseconds; }
    List<Reading> Ordered() => readings.Where(r => !settings.IsRemoved(r.Account)).OrderBy(r => settings.Accounts.FindIndex(p => p.Key == r.Account.Key)).ToList();
    async Task AddAccount(string service)
    {
        if (exiting || signIn.Active) return;
        var result = await signIn.Run(service, settings);
        if (exiting) return;
        if (!result.Succeeded) { panel.UpdateSignIn(false, result.Message); return; }
        await Refresh();
        if (exiting) return;
        panel.UpdateSignIn(false, Ordered().Count == 0 ? "No sign-in found. Please try again." : "");
        if (!panel.IsVisible && navigation.ActivePage == null) panel.OpenNearTray(false);
    }
    // One refresh at a time. A request during a refresh runs once more afterward,
    // and callers awaiting either request see the final result.
    Task Refresh()
    {
        if (exiting) return Task.CompletedTask;
        if (refreshing != null) { refreshQueued = true; return refreshing; }
        var task = RefreshLoop();
        if (!task.IsCompleted) refreshing = task;
        return task;
    }
    async Task RefreshLoop()
    {
        busy = true;
        long started = Environment.TickCount64;
        try
        {
            do { refreshQueued = false; await RefreshOnce(); }
            while (refreshQueued && !exiting);
            int remaining = (int)(Motion.MinimumSpinMilliseconds - (Environment.TickCount64 - started));
            if (remaining > 0 && !exiting) await Task.Delay(remaining);
        }
        finally { busy = false; refreshing = null; Render(); }
    }
    async Task RefreshOnce()
    {
        var accounts = await Providers.Discover(settings);
        if (exiting) return;
        readings = accounts.Select(a => readings.FirstOrDefault(r => r.Account.Key == a.Key) ?? new Reading(a, null, "Refreshing…", DateTimeOffset.UtcNow)).ToList();
        Render();
        foreach (var account in accounts)
        {
            if (IsFresh(readings.FirstOrDefault(r => r.Account.Key == account.Key))) continue;
            var fetched = await Providers.Fetch(account);
            if (exiting) return;
            int index = readings.FindIndex(r => r.Account.Key == account.Key);
            if (index < 0 || settings.IsRemoved(account)) continue;
            var old = readings[index];
            if (fetched.Error != null && old.Weekly != null) fetched = old with { Error = fetched.Error, ConnectionIssue = fetched.ConnectionIssue, RateLimited = fetched.RateLimited };
            else if (fetched.ResetError != null && old.Resets?.Grants != null && fetched.Resets?.Count == old.Resets.Count) fetched = fetched with { Resets = old.Resets };
            readings[index] = fetched; Render();
        }
        settings.TrySave();
    }
    // After sleep the timer may lag; opening the panel catches up on old readings.
    void RefreshIfStale()
    {
        var limit = TimeSpan.FromMilliseconds(2 * AppTiming.UsageRefreshMilliseconds);
        if (readings.Any(r => r.Error == null && DateTimeOffset.UtcNow - r.Updated > limit)) _ = Refresh();
    }
    // Repeated refreshes reuse recent readings; failed readings always retry.
    static bool IsFresh(Reading? reading) => reading is { Weekly: not null, Error: null }
        && DateTimeOffset.UtcNow - reading.Updated < TimeSpan.FromMilliseconds(AppTiming.UsageFreshMilliseconds);
    void Render()
    {
        if (exiting) return;
        var ordered = Ordered();
        var selected = ordered.Where(r => settings.For(r.Account) is { Show: true, Tray: true }).Take(AccountRules.MaxTrayAccounts).ToList();
        tray.Update(selected, settings); hover.UpdateEntries(selected, settings);
        panel.UpdateRows(ordered, settings, busy);
        if (navigation.ActivePage is AccountsDialog accounts) accounts.UpdateConnections(ordered);
        if (navigation.ActivePage is AccountDetailsDialog details && ordered.FirstOrDefault(r => r.Account.Key == details.AccountKey) is { } reading)
            details.UpdateReading(reading, settings, busy);
    }
    void OpenDetails(Account account)
    {
        if (navigation.ActivePage != null || Ordered().FirstOrDefault(r => r.Account.Key == account.Key) is not { } reading) return;
        var page = new AccountDetailsDialog(reading, settings);
        page.RefreshRequested += async () => await Refresh();
        HideHover(); navigation.Show(page);
    }
    void EditAccounts(bool showGeneral = true)
    {
        if (navigation.ActivePage is { } active) { active.Activate(); return; }
        HideHover();
        navigation.Show(new AccountsDialog(Ordered(), settings, () => _ = Refresh(), changed: Render,
            updates: updates, installUpdate: () => { if (updates.Install()) lifetime.Shutdown(); }, showGeneral: showGeneral, signIn: signIn));
    }
    public void Dispose()
    {
        if (exiting) return;
        exiting = true; timer.Stop(); updateTimer.Stop(); hoverTimer.Stop(); signIn.Cancel();
        navigation.Shutdown(); panel.Shutdown(); hover.Close(); tray.Dispose();
    }
}
