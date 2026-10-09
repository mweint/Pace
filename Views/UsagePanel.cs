namespace Pace;

public sealed class UsagePanel : WidgetWindow
{
    readonly SectionList rows = new();
    readonly ScrollViewport viewport;
    readonly IconButton refresh = new("refresh", "Refresh usage");
    readonly IconButton settingsButton = new("settings", "Settings");
    readonly MotionTween entrance;
    PixelPoint entranceTarget;
    bool closing;
    public bool EditingAccounts { get; set; }
    // The tray icon's screen bounds, or the pointer when the desktop does not report them.
    public Func<PixelRect?>? LocateTray { get; set; }
    public long LastAutoHide { get; private set; }
    public event Action? RefreshRequested, SettingsRequested, ManageAccountsRequested;
    public event Action<Account>? AccountRequested;
    public event Action<string>? AddAccountRequested;
    public UsagePanel()
    {
        entrance = new(this);
        Title = "Pace";
        viewport = new(rows);
        var layout = new DockPanel();
        var footer = new FooterBar("Pace", null, refresh, settingsButton);
        DockPanel.SetDock(footer, Dock.Bottom);
        layout.Children.Add(footer); layout.Children.Add(viewport);
        SetBody(layout);
        // A click while refreshing would only queue a duplicate request.
        refresh.Click += (_, _) => { if (!refresh.Spinning) RefreshRequested?.Invoke(); };
        settingsButton.Click += (_, _) => SettingsRequested?.Invoke();
        Deactivated += (_, _) =>
        {
            if (PreviewMode || KeepOpen || EditingAccounts || closing) return;
            LastAutoHide = Environment.TickCount64; Dismiss();
        };
        Closing += (_, e) => { e.Cancel = true; Dismiss(); };
        KeyDown += (_, e) => { if (e.Key == Key.Escape) { Dismiss(); e.Handled = true; } };
        Closed += (_, _) => entrance.Dispose();
        Height = 200;
    }
    public void UpdateNotification(bool available) { settingsButton.Notification = available; settingsButton.InvalidateVisual(); }
    public void UpdateRows(List<Reading> readings, Settings settings, bool loading)
    {
        refresh.Spinning = loading;
        var visible = readings.Where(r => settings.For(r.Account).Show).ToList();
        var existing = rows.Children.OfType<AccountRow>().ToList();
        bool same = existing.Count == visible.Count && existing.Select(r => r.Reading.Account.Key).SequenceEqual(visible.Select(r => r.Account.Key));
        if (visible.Count == 0)
        {
            if (rows.Children.Count != 1 || rows.Children[0] is not EmptyAccountsView empty || empty.HasAccounts != (readings.Count > 0))
            {
                rows.Children.Clear();
                var view = new EmptyAccountsView(readings.Count > 0);
                view.AddRequested += service => AddAccountRequested?.Invoke(service);
                view.ManageRequested += () => ManageAccountsRequested?.Invoke();
                rows.Children.Add(view);
            }
            ((EmptyAccountsView)rows.Children[0]).UpdateLoading(loading);
        }
        else
        {
            if (!same) rows.Children.Clear();
            for (int i = 0; i < visible.Count; i++)
            {
                var reading = visible[i];
                if (same)
                {
                    existing[i].RelativeResetTime = settings.RelativeResetTime;
                    existing[i].UpdateReading(reading, settings.DisplayName(reading.Account), settings.For(reading.Account));
                }
                else
                {
                    var row = new AccountRow(reading, settings.DisplayName(reading.Account), showSupplementalLimits: true, preference: settings.For(reading.Account)) { RelativeResetTime = settings.RelativeResetTime };
                    row.Activated += account => AccountRequested?.Invoke(account);
                    rows.Children.Add(row);
                }
            }
        }
        FitContent();
    }
    void FitContent()
    {
        rows.Measure(new Size(Width - 2 * UiMetrics.WindowBorderWidth, double.PositiveInfinity));
        Height = Math.Min(rows.DesiredSize.Height + UiMetrics.ToolbarHeight + 2 * UiMetrics.WindowBorderWidth, AvailableHeight);
        if (IsVisible)
        {
            entranceTarget = Anchor.Place(new Size(Width, Height), RenderScaling);
            if (!entrance.IsRunning) Position = entranceTarget;
        }
    }
    public void UpdateSignIn(bool pending, string message)
    {
        rows.Children.OfType<EmptyAccountsView>().FirstOrDefault()?.UpdateSignIn(pending, message);
        FitContent();
    }
    public void OpenNearTray(bool newSession = true)
    {
        if (newSession && !IsVisible)
        {
            var icon = LocateTray?.Invoke();
            var screen = (icon is { } i ? Screens.ScreenFromPoint(i.Center) : null) ?? Screens.Primary;
            if (screen != null) Anchor = PopupAnchor.For(screen, icon);
        }
        closing = false;
        entrance.Dispose();
        Place();
        entranceTarget = Position;
        bool animate = !IsVisible && Motion.Enabled;
        FrameOpacity = animate ? 0 : 1;
        Position = Anchor.Slide(entranceTarget, animate && newSession ? Motion.SlideOffset(FrameOpacity, RenderScaling) : 0);
        Show(); Activate();
        // Returning from a page restores native focus, not keyboard navigation.
        // Clear retained cues before showing the reused footer again.
        FocusCue.HideForPointer((Control)Content!);
        refresh.SuppressFocusOutline = settingsButton.SuppressFocusOutline = false;
        if (animate) entrance.Start(newSession ? Motion.FadeMilliseconds : Motion.NavigationFadeMilliseconds, t =>
        {
            FrameOpacity = t;
            Position = Anchor.Slide(entranceTarget, newSession ? Motion.SlideOffset(FrameOpacity, RenderScaling) : 0);
        }, ease: newSession ? Motion.EaseOut : Motion.Lead);
    }
    public void Dismiss(bool navigation = false)
    {
        if (!IsVisible || closing) return;
        closing = true;
        double from = FrameOpacity;
        refresh.SuppressFocusOutline = settingsButton.SuppressFocusOutline = true;
        FocusCue.HideForPointer((Control)Content!);
        entrance.Start(navigation ? Motion.NavigationFadeMilliseconds : Motion.FadeMilliseconds, t =>
        {
            FrameOpacity = from * (1 - t);
            Position = Anchor.Slide(entranceTarget, !navigation ? Motion.SlideOffset(FrameOpacity, RenderScaling) : 0);
        }, () =>
        {
            // Stay transparent while hidden so a reshow never presents a stale opaque frame.
            Hide(); closing = false;
            refresh.SuppressFocusOutline = settingsButton.SuppressFocusOutline = false;
        }, navigation ? Motion.Trail : Motion.EaseOut);
    }
    public void Shutdown() { entrance.Dispose(); Hide(); }
}
