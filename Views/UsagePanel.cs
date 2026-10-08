namespace Pace;

public sealed class UsagePanel : WidgetWindow
{
    readonly SectionList rows = new();
    readonly ScrollViewport viewport;
    readonly IconButton refresh = new("refresh", "Refresh usage");
    readonly IconButton settingsButton = new("settings", "Settings");
    readonly MotionTween entrance = new();
    PixelPoint entranceTarget;
    bool closing;
    public bool EditingAccounts { get; set; }
    public long LastAutoHide { get; private set; }
    public event Action? RefreshRequested, SettingsRequested, ManageAccountsRequested;
    public event Action<Account>? AccountRequested;
    public event Action<string>? AddAccountRequested;
    public UsagePanel()
    {
        Title = "Pace";
        viewport = new(rows);
        var layout = new DockPanel();
        var footer = new FooterBar("Pace", null, refresh, settingsButton);
        DockPanel.SetDock(footer, Dock.Bottom);
        layout.Children.Add(footer); layout.Children.Add(viewport);
        SetBody(layout);
        refresh.Click += (_, _) => RefreshRequested?.Invoke();
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
        refresh.IsEnabled = !loading;
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
            entranceTarget = PopupPlacement.BottomRight(AnchorArea, new Size(Width, Height), RenderScaling);
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
            var pointer = DesktopIntegration.Pointer;
            var screen = pointer is { } p ? Screens.ScreenFromPoint(p) : Screens.Primary;
            if (screen != null) AnchorArea = screen.WorkingArea;
        }
        closing = false;
        entrance.Dispose();
        Place();
        entranceTarget = Position;
        bool animate = !IsVisible && Motion.Enabled;
        FrameOpacity = animate ? 0 : 1;
        Position = new(entranceTarget.X, entranceTarget.Y + (animate && newSession ? Motion.SlideOffset(FrameOpacity, RenderScaling) : 0));
        Show(); Activate();
        // Returning from a page restores native focus, not keyboard navigation.
        // Clear retained cues before showing the reused footer again.
        FocusCue.HideForPointer((Control)Content!);
        refresh.SuppressFocusOutline = settingsButton.SuppressFocusOutline = false;
        if (animate) entrance.Start(newSession ? Motion.FadeMilliseconds : Motion.NavigationFadeMilliseconds, t =>
        {
            FrameOpacity = t;
            Position = new PixelPoint(entranceTarget.X, entranceTarget.Y + (newSession ? Motion.SlideOffset(FrameOpacity, RenderScaling) : 0));
        }, ease: newSession ? Motion.EaseOut : Motion.Linear);
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
            Position = new(entranceTarget.X, entranceTarget.Y + (!navigation ? Motion.SlideOffset(FrameOpacity, RenderScaling) : 0));
        }, () =>
        {
            // Stay transparent while hidden so a reshow never presents a stale opaque frame.
            Hide(); closing = false;
            refresh.SuppressFocusOutline = settingsButton.SuppressFocusOutline = false;
        }, navigation ? Motion.Linear : Motion.EaseOut);
    }
    public void Shutdown() { entrance.Dispose(); Hide(); }
}
