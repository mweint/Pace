using System.Drawing.Drawing2D;

namespace Usage;

public sealed class UsagePanel : WidgetForm
{
    readonly FlowLayoutPanel rows = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(UiMetrics.OuterInset, 0, UiMetrics.OuterInset, UiMetrics.OuterInset)
    };
    readonly IconButton refresh = new("refresh", "Refresh usage");
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
    public event Action<Account>? AccountRequested;
    public UsagePanel()
    {
        Text = "Pace";
        BackColor = Palette.Background;
        ForeColor = Palette.Text;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(UiMetrics.PanelWidth, 400);
        rows.Padding = new Padding(UiMetrics.OuterInset);
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 80,
            Padding = new Padding(0, 8, 8, 0),
            WrapContents = false
        };
        toolbar.BackColor = footer.BackColor;
        var name = new Label
        {
            Text = "Pace",
            AutoSize = true,
            Location = new Point(14, 13),
            Font = Palette.BarFont(),
            ForeColor = Palette.Muted
        };
        footer.Controls.Add(name);
        refresh.Click += (_, _) => RefreshRequested?.Invoke();
        var settings = new IconButton("accounts", "Manage accounts");
        settings.Click += (_, _) => SettingsRequested?.Invoke();
        toolbar.Controls.AddRange([refresh, settings]);
        foreach (Control button in toolbar.Controls)
            button.BackColor = footer.BackColor;
        footer.Controls.Add(toolbar);
        Controls.Add(rows);
        Controls.Add(footer);
        entrance.Tick += (_, _) =>
        {
            double progress = Math.Clamp((Environment.TickCount64 - entranceStart) / animationMilliseconds, 0, 1);
            double eased = slideAnimation ? Motion.EaseOut(progress) : Motion.NavigationEase(progress);
            shownAmount = animationFrom + (animationTo - animationFrom) * eased;
            Opacity = shownAmount;
            Location = new Point(entranceTarget.X, entranceTarget.Y + (slideAnimation ? (int)((1 - shownAmount) * UiMetrics.SlideDistance * DeviceDpi / 96f) : 0));
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
            if (!Visible)
                entrance.Stop();
        };
        Deactivate += (_, _) =>
        {
            if (IsClosing || OwnedForms.Length != 0)
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
        int scaleHeight = (int)(UiMetrics.AccountHeight * DeviceDpi / 96f);
        var existing = rows.Controls.OfType<AccountRow>().ToList();
        bool sameAccounts = existing.Count == visible.Count && existing.Count == rows.Controls.Count && existing.Select(r => r.Reading.Account.Key).SequenceEqual(visible.Select(r => r.Account.Key));
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
                existing[i].UpdateReading(r, alias);
                continue;
            }

            var row = new AccountRow(r, alias)
            {
                Width = ClientSize.Width - 2 * (UiMetrics.OuterInset + UiMetrics.WindowBorderWidth),
                Height = scaleHeight,
                Margin = new Padding(0, 0, 0, UiMetrics.CardGap),
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
            rows.Controls.Add(new Label { Text = "No accounts selected. Open Accounts to add or select a sign-in.", Width = 365, Height = 90, ForeColor = Palette.Muted });
        // Card gaps belong between cards; the outer bottom inset matches the sides.
        if (rows.Controls.Count > 0)
        {
            var last = rows.Controls[rows.Controls.Count - 1];
            last.Margin = new Padding(last.Margin.Left, last.Margin.Top, last.Margin.Right, 0);
        }

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

    public void OpenNearTray(bool newSession = true)
    {
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
        Location = new Point(entranceTarget.X, entranceTarget.Y + (animate ? (newSession ? (int)(UiMetrics.SlideDistance * DeviceDpi / 96f) : 0) : 0));
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
        if (!Visible || IsClosing)
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
