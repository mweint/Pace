using System.Drawing.Drawing2D;

namespace Usage;

public sealed class TrayApp : ApplicationContext
{
    readonly NativeTrayIcon tray = new()
    {
        Visible = true
    };
    readonly UsagePanel panel = new();
    readonly TrayHover hover = new();
    readonly System.Windows.Forms.Timer hoverTimer = new()
    {
        Interval = AppTiming.HoverPollMilliseconds
    };
    Point hoverAnchor;
    Rectangle hoverIconBounds;
    long hoverStarted;
    long suppressHoverUntil;
    bool hoverPending;
    readonly System.Windows.Forms.Timer timer = new()
    {
        Interval = AppTiming.PaceRefreshMilliseconds
    };
    readonly Settings settings = Settings.Load();
    readonly AppUpdates updates;
    readonly System.Windows.Forms.Timer updateTimer = new() { Interval = AppTiming.UpdateCheckMilliseconds };
    List<Reading> readings = [];
    bool busy;
    bool signingIn;
    bool exiting;
    PageDialog? activePage;
    int ticks;
    public TrayApp()
    {
        updates = new(settings);
        updates.Changed += () => { if (!exiting) panel.UpdateNotification(updates.Notify); };
        updateTimer.Tick += async (_, _) => await updates.Check();
        updateTimer.Start();
        tray.Icon = TrayDrawing.Icon([]);
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show Pace", null, (_, _) =>
        {
            if (activePage == null)
                panel.OpenNearTray();
        });
        menu.Items.Add("Refresh", null, async (_, _) => await Refresh());
        menu.Items.Add("Settings…", null, (_, _) => EditAccounts());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => ExitThread());
        tray.ContextMenuStrip = menu;
        tray.MouseMove += (_, _) =>
        {
            if (Environment.TickCount64 < suppressHoverUntil || panel.Visible || panel.EditingAccounts)
                return;
            if (!hoverPending)
            {
                hoverStarted = Environment.TickCount64;
                hoverAnchor = Cursor.Position;
                hoverIconBounds = tray.Bounds ?? new Rectangle(hoverAnchor, Size.Empty);
                hoverPending = true;
            }
        };
        hoverTimer.Tick += (_, _) =>
        {
            if (!hoverPending)
                return;
            var pointer = Cursor.Position;
            bool nearIcon = Math.Abs(pointer.X - hoverAnchor.X) <= 22 && Math.Abs(pointer.Y - hoverAnchor.Y) <= 22;
            if (panel.Visible || panel.EditingAccounts || (!nearIcon && !hover.Bounds.Contains(pointer)))
            {
                hover.Hide();
                hoverPending = false;
                return;
            }

            if (!hover.Visible && Environment.TickCount64 - hoverStarted >= UiMetrics.HoverDelayMilliseconds)
                hover.Open(hoverIconBounds);
        };
        hoverTimer.Start();
        menu.Opening += (_, _) =>
        {
            hover.Hide();
            hoverPending = false;
            suppressHoverUntil = Environment.TickCount64 + AppTiming.HoverSuppressMilliseconds;
        };
        tray.MouseClick += (_, e) =>
        {
            hover.Hide();
            hoverPending = false;
            suppressHoverUntil = Environment.TickCount64 + AppTiming.HoverSuppressMilliseconds;
            if (e.Button != MouseButtons.Left)
                return;
            if (activePage != null)
            {
                activePage.DismissAll();
                panel.Dismiss();
                return;
            }

            // Windows deactivates the popup before delivering the tray click. Treat that click as close.
            if (TrayToggle.ShouldClose(panel.Visible, panel.LastAutoHide, Environment.TickCount64))
                panel.Dismiss();
            else
                panel.OpenNearTray();
        };
        panel.RefreshRequested += async () => await Refresh();
        panel.SettingsRequested += () => EditAccounts();
        panel.ManageAccountsRequested += () => EditAccounts(false);
        panel.AccountRequested += OpenDetails;
        panel.AddAccountRequested += async service => await AddAccount(service);
        timer.Tick += async (_, _) =>
        {
            Render();
            if (++ticks % AppTiming.UsageRefreshTicks == 0)
                await Refresh();
        };
        timer.Start();
        _ = Refresh();
        _ = updates.Check();
        panel.OpenNearTray();
    }

    List<Reading> Ordered() => readings.Where(r => !settings.IsRemoved(r.Account)).OrderBy(r => settings.Accounts.FindIndex(p => p.Key == r.Account.Key)).ToList();
    async Task AddAccount(string service)
    {
        if (exiting || signingIn)
            return;
        signingIn = true;
        panel.UpdateSignIn(true, "Complete sign-in in your browser…");
        try
        {
            await SignIn.Begin(service, settings);
            if (exiting)
                return;
            await Refresh();
            if (exiting)
                return;
            panel.UpdateSignIn(false, Ordered().Count == 0 ? "No sign-in found. Please try again." : "");
            if (!panel.Visible && activePage == null)
                panel.OpenNearTray(newSession: false);
        }
        catch (Exception e)
        {
            if (!exiting)
                panel.UpdateSignIn(false, e is InvalidOperationException ? e.Message : "Sign-in failed. Please try again.");
        }
        finally
        {
            signingIn = false;
        }
    }

    async Task Refresh()
    {
        if (exiting || busy)
            return;
        busy = true;
        try
        {
            var accounts = Providers.Discover(settings);
            foreach (var a in accounts)
                settings.For(a);
            readings = accounts.Select(a => readings.FirstOrDefault(r => r.Account.Key == a.Key) ?? new Reading(a, null, "Refreshing…", DateTimeOffset.UtcNow)).ToList();
            Render();
            // Sequential requests avoid unnecessary bursts against subscription endpoints.
            foreach (var a in accounts)
            {
                var fetched = await Providers.Fetch(a);
                if (exiting)
                    return;
                var old = readings.First(r => r.Account.Key == a.Key);
                if (fetched.Error != null && old.Weekly != null)
                    fetched = old with
                    {
                        Error = fetched.Error,
                        ConnectionIssue = fetched.ConnectionIssue
                    };
                else if (fetched.ResetError != null && old.Resets?.Grants != null && fetched.Resets?.Count == old.Resets.Count)
                    fetched = fetched with
                    {
                        Resets = old.Resets
                    };
                readings[readings.FindIndex(r => r.Account.Key == a.Key)] = fetched;
                Render();
            }

            settings.Save();
        }
        finally
        {
            busy = false;
            Render();
        }
    }

    void Render()
    {
        if (exiting || panel.IsDisposed || panel.Disposing)
            return;
        var ordered = Ordered();
        var selected = ordered.Where(r => settings.For(r.Account) is { Show: true, Tray: true }).Take(AccountRules.MaxTrayAccounts).ToList();
        var old = tray.Icon;
        tray.Icon = TrayDrawing.Icon(selected);
        old?.Dispose();
        hover.UpdateEntries(selected, settings);
        panel.UpdateRows(ordered, settings, busy);
        if (activePage is AccountsDialog accountsDialog)
            accountsDialog.UpdateConnections(ordered);
        if (activePage is AccountDetailsDialog details && ordered.FirstOrDefault(reading => reading.Account.Key == details.AccountKey) is { } detailReading)
            details.UpdateReading(detailReading, settings, busy);
    }

    void OpenDetails(Account account)
    {
        if (activePage != null)
            return;
        var reading = Ordered().FirstOrDefault(item => item.Account.Key == account.Key);
        if (reading == null)
            return;
        using var dialog = new AccountDetailsDialog(reading, settings);
        dialog.RefreshRequested += async () => await Refresh();
        ShowPage(dialog);
    }

    void EditAccounts(bool showGeneral = true)
    {
        if (activePage != null)
        {
            activePage.Activate();
            return;
        }

        using var dialog = new AccountsDialog(Ordered(), settings, () =>
        {
            _ = Refresh();
        }, changed: Render, updates: updates, installUpdate: () =>
        {
            if (updates.Install()) ExitThread();
        }, showGeneral: showGeneral);
        ShowPage(dialog);
    }

    void ShowPage(PageDialog dialog)
    {
        if (exiting)
            return;
        panel.EditingAccounts = true;
        if (!panel.Visible)
            panel.OpenNearTray();
        hover.Hide();
        hoverPending = false;
        activePage = dialog;
        try
        {
            PageNavigation.Show(panel, dialog);
        }
        finally
        {
            activePage = null;
            panel.EditingAccounts = false;
            if (!exiting && !panel.IsDisposed && !dialog.CloseAllRequested)
                panel.OpenNearTray(newSession: false);
            Render();
        }
    }

    protected override void ExitThreadCore()
    {
        if (exiting)
            return;
        exiting = true;
        updateTimer.Stop();
        updateTimer.Dispose();
        timer.Stop();
        timer.Dispose();
        hoverTimer.Stop();
        hoverTimer.Dispose();
        hover.Dispose();
        tray.Visible = false;
        tray.Icon?.Dispose();
        tray.Dispose();
        activePage?.Dispose();
        panel.Dispose();
        base.ExitThreadCore();
    }
}
