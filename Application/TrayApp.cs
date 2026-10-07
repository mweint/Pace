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
    long hoverStarted;
    long suppressHoverUntil;
    bool hoverPending;
    readonly System.Windows.Forms.Timer timer = new()
    {
        Interval = AppTiming.PaceRefreshMilliseconds
    };
    readonly Settings settings = Settings.Load();
    List<Reading> readings = [];
    bool busy;
    PageDialog? activePage;
    int ticks;
    public TrayApp()
    {
        tray.Icon = TrayDrawing.Icon([]);
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show Pace", null, (_, _) =>
        {
            if (activePage == null)
                panel.OpenNearTray();
        });
        menu.Items.Add("Refresh", null, async (_, _) => await Refresh());
        menu.Items.Add("Accounts…", null, (_, _) => EditAccounts());
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
                hover.Open(hoverAnchor);
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
        panel.SettingsRequested += EditAccounts;
        panel.AccountRequested += OpenDetails;
        timer.Tick += async (_, _) =>
        {
            Render();
            if (++ticks % AppTiming.UsageRefreshTicks == 0)
                await Refresh();
        };
        timer.Start();
        panel.UpdateRows([], settings, true);
        panel.OpenNearTray();
        _ = Refresh();
    }

    List<Reading> Ordered() => readings.Where(r => !settings.IsRemoved(r.Account)).OrderBy(r => settings.Accounts.FindIndex(p => p.Key == r.Account.Key)).ToList();
    async Task Refresh()
    {
        if (busy)
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
                var old = readings.First(r => r.Account.Key == a.Key);
                if (fetched.Error != null && old.Weekly != null)
                    fetched = old with
                    {
                        Error = fetched.Error
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
        if (panel.IsDisposed)
            return;
        var ordered = Ordered();
        var selected = ordered.Where(r => settings.For(r.Account) is { Show: true, Tray: true }).Take(AccountRules.MaxTrayAccounts).ToList();
        var old = tray.Icon;
        tray.Icon = TrayDrawing.Icon(selected);
        old?.Dispose();
        hover.UpdateEntries(selected, settings);
        panel.UpdateRows(ordered, settings, busy);
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

    void EditAccounts()
    {
        if (activePage != null)
        {
            activePage.Activate();
            return;
        }

        using var dialog = new AccountsDialog(Ordered(), settings, () =>
        {
            _ = Refresh();
        }, changed: Render);
        ShowPage(dialog);
    }

    void ShowPage(PageDialog dialog)
    {
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
            if (!dialog.CloseAllRequested)
                panel.OpenNearTray(newSession: false);
            Render();
        }
    }

    protected override void ExitThreadCore()
    {
        timer.Stop();
        timer.Dispose();
        hoverTimer.Stop();
        hoverTimer.Dispose();
        hover.Dispose();
        tray.Visible = false;
        tray.Icon?.Dispose();
        tray.Dispose();
        panel.Dispose();
        base.ExitThreadCore();
    }
}
