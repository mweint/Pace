namespace Usage;

public sealed class AccountDetailsDialog : PageDialog
{
    readonly FlowLayoutPanel content = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(UiMetrics.OuterInset)
    };
    readonly IconButton refresh = new("refresh", "Refresh usage");
    public string AccountKey
    {
        get;
    }

    public event Action? RefreshRequested;
    public AccountDetailsDialog(Reading reading, Settings settings)
    {
        AccountKey = reading.Account.Key;
        Text = "Pace · " + settings.DisplayName(reading.Account);
        ClientSize = new Size(UiMetrics.PanelWidth, UiMetrics.AccountHeight + UiMetrics.ToolbarHeight);
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = UiMetrics.ToolbarHeight,
            BackColor = Palette.Footer
        };
        var back = new IconButton("back", "Back to usage")
        {
            BackColor = Palette.Footer
        };
        back.SetBounds(8, 8, UiMetrics.IconButtonSize, UiMetrics.IconButtonSize);
        back.Click += (_, _) => Close();
        var caption = new Label
        {
            Text = "Account",
            Left = 44,
            Top = 13,
            Width = 180,
            Height = 24,
            Font = Palette.BarFont(),
            ForeColor = Palette.Muted
        };
        refresh.BackColor = Palette.Footer;
        refresh.SetBounds(UiMetrics.PanelWidth - UiMetrics.IconButtonSize - UiMetrics.OuterInset, 8, UiMetrics.IconButtonSize, UiMetrics.IconButtonSize);
        refresh.Click += (_, _) => RefreshRequested?.Invoke();
        footer.Controls.AddRange([back, caption, refresh]);
        Controls.Add(content);
        Controls.Add(footer);
        NavigationFocus = back;
        UpdateReading(reading, settings, false);
    }

    public void UpdateReading(Reading reading, Settings settings, bool loading)
    {
        if (IsClosing || IsDisposed)
            return;
        refresh.Enabled = !loading;
        content.SuspendLayout();
        foreach (Control old in content.Controls.Cast<Control>().ToArray())
        {
            content.Controls.Remove(old);
            old.Dispose();
        }

        int width = UiMetrics.ContentWidth;
        var identity = new Panel
        {
            Width = width,
            Height = UiMetrics.DetailIdentityHeight,
            Margin = new Padding(0, 0, 0, UiMetrics.CardGap)
        };
        identity.Paint += (_, e) => ServiceMark.Draw(e.Graphics, reading.Account.Service, 2, 4);
        identity.Controls.Add(new Label { Text = settings.DisplayName(reading.Account), Left = 26, Top = 0, Width = width - 26, Height = 25, Font = Palette.AccountFont(), ForeColor = Palette.Text, AutoEllipsis = true });
        identity.Controls.Add(new Label { Text = reading.Account.Label, Left = 26, Top = 27, Width = width - 26, Height = 22, Font = Palette.BodyFont(), ForeColor = Palette.Muted, AutoEllipsis = true });
        content.Controls.Add(identity);
        var limits = reading.Limits ?? (reading.Weekly is { } weekly ? [new UsageLimit("weekly", "Weekly", weekly)] : new List<UsageLimit>());
        foreach (var limit in limits)
        {
            var sectionReading = reading with
            {
                Weekly = limit.Window,
                Resets = null,
                ResetError = null
            };
            content.Controls.Add(new AccountRow(sectionReading, limit.Name, showServiceMark: false) { Width = width, Height = UiMetrics.AccountHeight, Margin = new Padding(0, 0, 0, UiMetrics.CardGap) });
        }

        void Info(string text, Color color)
        {
            int lines = text.Split('\n').Length;
            var label = new Label
            {
                Text = text,
                Width = width,
                Font = Palette.BodyFont(),
                ForeColor = color,
                Margin = Padding.Empty
            };
            label.Height = Math.Max(lines * UiMetrics.DetailInfoLineHeight, TextRenderer.MeasureText(text, label.Font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak).Height);
            content.Controls.Add(label);
        }

        if (reading.Resets is { } bank)
        {
            var now = DateTimeOffset.UtcNow;
            Info($"Banked resets · {bank.Available(now)}", bank.ExpiringSoon(now) ? Palette.ResetWarning : Palette.Text);
            if (bank.Available(now) > 0)
                Info(bank.Details(now), bank.ExpiringSoon(now) ? Palette.ResetWarning : Palette.Muted);
        }

        if (reading.ResetError is { } resetError)
            Info(resetError, Palette.ResetWarning);
        if (reading.Error is { } error)
            Info(error, Palette.ResetWarning);
        Info($"Updated {reading.Updated.ToLocalTime():M/d · h:mm tt}", Palette.Muted);
        int height = content.Controls.Cast<Control>().Sum(c => c.Height + c.Margin.Vertical) + 2 * UiMetrics.OuterInset + UiMetrics.ToolbarHeight + Padding.Vertical;
        var area = AnchorArea;
        ClientSize = new Size(ClientSize.Width, Math.Min(height, area.Height - 60));
        content.ResumeLayout();
        if (Visible)
            Place();
    }
}
