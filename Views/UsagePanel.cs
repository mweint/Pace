using System.Drawing.Drawing2D;

namespace Usage;

public sealed class UsagePanel : WidgetForm
{
    readonly SectionList rows = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = Padding.Empty
    };
    readonly IconButton refresh = new("refresh", "Refresh usage");
    readonly IconButton settingsButton = new("settings", "Settings");
    public void UpdateNotification(bool available) { settingsButton.Notification = available; settingsButton.Invalidate(); }
    readonly Panel footer = new()
    {
        Dock = DockStyle.Bottom,
        Height = UiMetrics.ToolbarHeight,
        BackColor = Palette.Footer
    };
    readonly System.Windows.Forms.Timer entrance = new()
    {
        Interval = Motion.FrameMilliseconds
    };
    Point entranceTarget;
    Screen? anchorScreen;
    internal Rectangle AnchorArea => (anchorScreen ?? Screen.FromControl(this)).WorkingArea;

    long entranceStart;
    double shownAmount = 1, animationFrom, animationTo;
    double animationMilliseconds = Motion.FadeMilliseconds;
    bool slideAnimation = true;
    public bool IsClosing
    {
        get; private set;
    }
    internal bool EditingAccounts
    {
        get; set;
    }
    public long LastAutoHide
    {
        get; private set;
    }

    public event Action? RefreshRequested;
    public event Action? SettingsRequested;
    public event Action? ManageAccountsRequested;
    public event Action<Account>? AccountRequested;
    public event Action<string>? AddAccountRequested;
    public UsagePanel()
    {
        Text = "Pace";
        BackColor = Palette.Background;
        ForeColor = Palette.Text;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(UiMetrics.PanelWidth, 400);
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 2 * (UiMetrics.IconButtonSize + UiMetrics.InlineGap) + UiMetrics.ToolbarInset + UiMetrics.InlineGap,
            Padding = new Padding(0, UiMetrics.ToolbarInset, UiMetrics.ToolbarInset, 0),
            WrapContents = false
        };
        toolbar.BackColor = footer.BackColor;
        var name = new Label
        {
            Text = "Pace",
            AutoSize = true,
            Location = new Point(UiMetrics.ContentInset, UiMetrics.ToolbarTextTop),
            Font = Palette.BarFont(),
            ForeColor = Palette.Muted
        };
        footer.Controls.Add(name);
        refresh.Click += (_, _) => RefreshRequested?.Invoke();
        var settings = settingsButton;
        settings.Click += (_, _) => SettingsRequested?.Invoke();
        toolbar.Controls.AddRange([refresh, settings]);
        foreach (Control button in toolbar.Controls)
            button.BackColor = footer.BackColor;
        footer.Controls.Add(toolbar);
        Controls.Add(rows);
        Controls.Add(footer);
        entrance.Tick += (_, _) =>
        {
            if (IsDisposed || Disposing)
                return;
            double progress = Math.Clamp((Environment.TickCount64 - entranceStart) / animationMilliseconds, 0, 1);
            double eased = slideAnimation ? Motion.EaseOut(progress) : Motion.NavigationEase(progress);
            shownAmount = animationFrom + (animationTo - animationFrom) * eased;
            Opacity = shownAmount;
            Location = new Point(entranceTarget.X, entranceTarget.Y + (slideAnimation ? (int)((1 - shownAmount) * UiMetrics.SlideDistance * DeviceDpi / (float)UiMetrics.BaseDpi) : 0));
            if (progress >= 1)
            {
                entrance.Stop();
                if (IsClosing)
                {
                    Hide();
                    IsClosing = false;
                    Opacity = 1;
                }
            }
        };
        VisibleChanged += (_, _) =>
        {
            if (!IsDisposed && !Disposing && !Visible)
                entrance.Stop();
        };
        Deactivate += (_, _) =>
        {
            if (IsDisposed || Disposing || IsClosing || OwnedForms.Length != 0)
                return;
            if (!EditingAccounts)
            {
                LastAutoHide = Environment.TickCount64;
                Dismiss();
            }
        };
        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Dismiss();
            }
        };
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
                Dismiss();
        };
    }

    public void UpdateRows(List<Reading> readings, Settings settings, bool loading)
    {
        refresh.Enabled = !loading;
        rows.SuspendLayout();
        rows.AutoScroll = false;
        var visible = readings.Where(r => settings.For(r.Account).Show).ToList();
        var existing = rows.Controls.OfType<AccountRow>().ToList();
        bool sameAccounts = existing.Count == visible.Count && existing.Count == rows.Controls.Count && existing.Select(r => r.Reading.Account.Key).SequenceEqual(visible.Select(r => r.Account.Key));
        if (visible.Count == 0)
            sameAccounts = rows.Controls.Count == 1 && rows.Controls[0] is EmptyAccountsView empty && empty.HasAccounts == (readings.Count > 0);
        if (!sameAccounts)
            foreach (Control old in rows.Controls.Cast<Control>().ToArray())
            {
                rows.Controls.Remove(old);
                old.Dispose();
            }

        for (int i = 0; i < visible.Count; i++)
        {
            var r = visible[i];
            string alias = settings.DisplayName(r.Account);
            if (sameAccounts)
            {
                existing[i].RelativeResetTime = settings.RelativeResetTime;
                existing[i].UpdateReading(r, alias, settings.For(r.Account));
                continue;
            }

            var row = new AccountRow(r, alias, showSupplementalLimits: true, preference: settings.For(r.Account))
            {
                RelativeResetTime = settings.RelativeResetTime,
                Width = ClientSize.Width - 2 * (UiMetrics.OuterInset + UiMetrics.WindowBorderWidth),
                Cursor = Cursors.Hand,
                TabStop = true
            };
            row.Click += (_, _) => AccountRequested?.Invoke(row.Reading.Account);
            row.KeyDown += (_, e) =>
            {
                if (e.KeyCode is Keys.Enter or Keys.Space)
                {
                    e.Handled = true;
                    AccountRequested?.Invoke(row.Reading.Account);
                }
            };
            rows.Controls.Add(row);
        }

        if (visible.Count == 0)
        {
            if (!sameAccounts)
            {
                var empty = new EmptyAccountsView(readings.Count > 0);
                empty.AddRequested += service => AddAccountRequested?.Invoke(service);
                empty.ManageRequested += () => ManageAccountsRequested?.Invoke();
                rows.Controls.Add(empty);
            }
            ((EmptyAccountsView)rows.Controls[0]).UpdateLoading(loading);
        }
        // Card gaps belong between cards; the outer bottom inset matches the sides.
        if (rows.Controls.Count > 0)
        {
            var last = rows.Controls[rows.Controls.Count - 1];
            last.Margin = new Padding(last.Margin.Left, last.Margin.Top, last.Margin.Right, 0);
        }

        FitContent();
    }

    void FitContent()
    {
        int contentHeight = rows.Controls.Cast<Control>().Sum(c => c.Height + c.Margin.Vertical);
        int targetHeight = footer.Height + Padding.Vertical + contentHeight + rows.Padding.Vertical;
        ClientSize = new Size(ClientSize.Width, Math.Min(targetHeight, AnchorArea.Height - 60));
        bool needsScroll = targetHeight > ClientSize.Height;
        foreach (Control row in rows.Controls)
            row.Width = ClientSize.Width - Padding.Horizontal - rows.Padding.Horizontal - (needsScroll ? SystemInformation.VerticalScrollBarWidth : 0);
        rows.AutoScroll = needsScroll;
        rows.ResumeLayout();
        if (Visible)
        {
            var area = AnchorArea;
            entranceTarget = PopupPlacement.BottomRight(area, Size);
            if (!entrance.Enabled)
                Location = entranceTarget;
        }
    }

    public void UpdateSignIn(bool pending, string message)
    {
        rows.SuspendLayout();
        rows.Controls.OfType<EmptyAccountsView>().FirstOrDefault()?.UpdateSignIn(pending, message);
        FitContent();
    }

    public void OpenNearTray(bool newSession = true)
    {
        if (IsDisposed || Disposing)
            return;
        SetClosingFocus(false);
        if (anchorScreen == null || (newSession && !Visible))
            anchorScreen = Screen.FromPoint(Cursor.Position);
        var area = AnchorArea;
        _ = Handle;
        entranceTarget = PopupPlacement.BottomRight(area, Size);
        bool animate = !Visible && Motion.Enabled;
        entrance.Stop();
        IsClosing = false;
        shownAmount = animate ? 0 : 1;
        Opacity = shownAmount;
        Location = new Point(entranceTarget.X, entranceTarget.Y + (animate ? (newSession ? (int)(UiMetrics.SlideDistance * DeviceDpi / (float)UiMetrics.BaseDpi) : 0) : 0));
        Show();
        Activate();
        if (animate)
            StartTransition(1, navigation: !newSession);
    }

    void StartTransition(double target, bool navigation = false)
    {
        slideAnimation = !navigation;
        animationMilliseconds = navigation ? Motion.NavigationFadeMilliseconds : Motion.FadeMilliseconds;
        animationFrom = shownAmount;
        animationTo = target;
        entranceStart = Environment.TickCount64;
        entrance.Start();
    }

    public void Dismiss(bool navigation = false)
    {
        if (IsDisposed || Disposing || !Visible || IsClosing)
            return;
        SetClosingFocus(true);
        if (!Motion.Enabled)
        {
            Hide();
            return;
        }

        IsClosing = true;
        StartTransition(0, navigation);
    }

    void SetClosingFocus(bool closing)
    {
        foreach (var button in footer.Controls.OfType<FlowLayoutPanel>().SelectMany(p => p.Controls.OfType<IconButton>()))
        {
            button.SuppressFocusOutline = closing;
            button.Invalidate();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            entrance.Dispose();
        base.Dispose(disposing);
    }
}
