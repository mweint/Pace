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
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(AppTiming.PaceRefreshMilliseconds) };
    readonly DispatcherTimer updateTimer = new() { Interval = TimeSpan.FromMilliseconds(AppTiming.UpdateCheckMilliseconds) };
    readonly DispatcherTimer hoverTimer = new() { Interval = TimeSpan.FromMilliseconds(AppTiming.HoverPollMilliseconds) };
    List<Reading> readings = [];
    Task? refreshing;
    bool busy, refreshQueued, signingIn, exiting, hoverPending;
    long hoverStarted, suppressHoverUntil;
    PixelPoint hoverAnchor;
    PixelRect hoverBounds;
    int ticks;
    public TrayApp(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        this.lifetime = lifetime;
        navigation = new(panel);
        updates = new(settings);
        updates.Changed += () => { if (!exiting) panel.UpdateNotification(updates.Notify); };
        navigation.Changed += Render;
        tray.Update([]);
        tray.Clicked += () =>
        {
            HideHover();
            if (navigation.ActivePage != null) navigation.DismissAll();
            else if (TrayToggle.ShouldClose(panel.IsVisible, panel.LastAutoHide, Environment.TickCount64)) panel.Dismiss();
            else panel.OpenNearTray();
        };
        tray.MenuSelected += command =>
        {
            HideHover();
            switch (command)
            {
                case 1: if (navigation.ActivePage is { } page) page.Activate(); else panel.OpenNearTray(); break;
                case 2: _ = Refresh(); break;
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
        timer.Tick += async (_, _) => { Render(); if (++ticks % AppTiming.UsageRefreshTicks == 0) await Refresh(); };
        updateTimer.Tick += async (_, _) => await updates.Check();
        timer.Start(); updateTimer.Start(); hoverTimer.Start();
        _ = Refresh(); _ = updates.Check();
        panel.OpenNearTray();
    }
    void HideHover() { hover.Hide(); hoverPending = false; suppressHoverUntil = Environment.TickCount64 + AppTiming.HoverSuppressMilliseconds; }
    List<Reading> Ordered() => readings.Where(r => !settings.IsRemoved(r.Account)).OrderBy(r => settings.Accounts.FindIndex(p => p.Key == r.Account.Key)).ToList();
    async Task AddAccount(string service)
    {
        if (exiting || signingIn) return;
        signingIn = true; panel.UpdateSignIn(true, "Complete sign-in in your browser…");
        try
        {
            await SignIn.Begin(service, settings);
            if (exiting) return;
            await Refresh();
            if (exiting) return;
            panel.UpdateSignIn(false, Ordered().Count == 0 ? "No sign-in found. Please try again." : "");
            if (!panel.IsVisible && navigation.ActivePage == null) panel.OpenNearTray(false);
        }
        catch (Exception e) { if (!exiting) panel.UpdateSignIn(false, e is InvalidOperationException ? e.Message : "Sign-in failed. Please try again."); }
        finally { signingIn = false; }
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
        try
        {
            do { refreshQueued = false; await RefreshOnce(); }
            while (refreshQueued && !exiting);
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
            var fetched = await Providers.Fetch(account);
            if (exiting) return;
            int index = readings.FindIndex(r => r.Account.Key == account.Key);
            if (index < 0 || settings.IsRemoved(account)) continue;
            var old = readings[index];
            if (fetched.Error != null && old.Weekly != null) fetched = old with { Error = fetched.Error, ConnectionIssue = fetched.ConnectionIssue };
            else if (fetched.ResetError != null && old.Resets?.Grants != null && fetched.Resets?.Count == old.Resets.Count) fetched = fetched with { Resets = old.Resets };
            readings[index] = fetched; Render();
        }
        settings.TrySave();
    }
    void Render()
    {
        if (exiting) return;
        var ordered = Ordered();
        var selected = ordered.Where(r => settings.For(r.Account) is { Show: true, Tray: true }).Take(AccountRules.MaxTrayAccounts).ToList();
        tray.Update(selected); hover.UpdateEntries(selected, settings);
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
            updates: updates, installUpdate: () => { if (updates.Install()) lifetime.Shutdown(); }, showGeneral: showGeneral));
    }
    public void Dispose()
    {
        if (exiting) return;
        exiting = true; timer.Stop(); updateTimer.Stop(); hoverTimer.Stop();
        navigation.Shutdown(); panel.Shutdown(); hover.Close(); tray.Dispose();
    }
}
