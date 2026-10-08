namespace Usage;

public sealed class AccountDetailsDialog : PageDialog
{
    readonly SectionList content = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = Padding.Empty
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
        ClientSize = new Size(UiMetrics.PanelWidth, UiMetrics.ToolbarHeight);
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
        back.SetBounds(UiMetrics.ToolbarInset, UiMetrics.ToolbarInset, UiMetrics.IconButtonSize, UiMetrics.IconButtonSize);
        back.Click += (_, _) => Close();
        var caption = new Label
        {
            Text = "Account",
            Left = UiMetrics.ToolbarLabelLeft,
            Top = UiMetrics.ToolbarTextTop,
            Width = 180,
            Height = UiMetrics.ToolbarLabelHeight,
            Font = Palette.BarFont(),
            ForeColor = Palette.Muted
        };
        refresh.BackColor = Palette.Footer;
        refresh.SetBounds(UiMetrics.PanelWidth - UiMetrics.IconButtonSize - UiMetrics.OuterInset, UiMetrics.ToolbarInset, UiMetrics.IconButtonSize, UiMetrics.IconButtonSize);
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

        int width = ClientSize.Width - Padding.Horizontal;
        var identity = new Panel
        {
            Width = width - 2 * UiMetrics.ContentInset,
            Margin = new Padding(UiMetrics.ContentInset)
        };
        int textLeft = UiMetrics.ServiceIconSize + UiMetrics.CardGap;
        var displayName = new Label { Text = settings.DisplayName(reading.Account), Left = textLeft, Width = identity.Width - textLeft, Font = Palette.AccountFont(), ForeColor = Palette.Text, AutoEllipsis = true };
        displayName.Height = (int)Math.Ceiling(displayName.Font.GetHeight(UiMetrics.BaseDpi));
        var email = new Label { Text = reading.Account.Label, Left = textLeft, Top = displayName.Bottom + UiMetrics.InlineGap, Width = identity.Width - textLeft, Font = Palette.BodyFont(), ForeColor = Palette.Muted, AutoEllipsis = true };
        email.Height = (int)Math.Ceiling(email.Font.GetHeight(UiMetrics.BaseDpi));
        identity.Height = email.Bottom;
        identity.Paint += (_, e) => ServiceMark.Draw(e.Graphics, reading.Account.Service, 0, (displayName.Height - UiMetrics.ServiceIconSize) / 2f);
        identity.Controls.AddRange([displayName, email]);
        content.Controls.Add(identity);
        var limits = reading.Limits ?? (reading.Weekly is { } weekly ? [new UsageLimit("weekly", "Weekly", weekly)] : new List<UsageLimit>());
        foreach (var limit in limits.OrderBy(limit => limit.Window == reading.Weekly ? 0 : 1))
        {
            var sectionReading = reading with
            {
                Weekly = limit.Window,
                Limits = [limit],
                Resets = null,
                ResetError = null
            };
            content.Controls.Add(new AccountRow(sectionReading, limit.Name, showServiceMark: false,
                compactDetail: limit.Window != reading.Weekly) { Width = width, RelativeResetTime = settings.RelativeResetTime });
        }

        var information = new InfoSection { Width = width };
        void Info(string text, Color color) => information.Add(text, color);

        if (reading.Resets is { } bank)
        {
            var now = DateTimeOffset.UtcNow;
            Info($"Banked resets · {bank.Available(now)}", bank.ExpiringSoon(now) ? Palette.Warning : Palette.Text);
            if (bank.Available(now) > 0)
                Info(bank.Details(now), bank.ExpiringSoon(now) ? Palette.Warning : Palette.Muted);
        }

        if (reading.ResetError is { } resetError)
            Info(resetError, Palette.Warning);
        var warning = LimitWarning.Summary(reading, DateTimeOffset.UtcNow);
        if (warning.Length > 0)
            Info(warning, Palette.Warning);
        if (reading.Error is { } error)
            Info(error, Palette.Warning);
        Info($"Updated {reading.Updated.ToLocalTime():M/d · h:mm tt}", Palette.Muted);
        content.Controls.Add(information);
        int height = content.Controls.Cast<Control>().Sum(c => c.Height + c.Margin.Vertical) + UiMetrics.ToolbarHeight + Padding.Vertical;
        var area = AnchorArea;
        ClientSize = new Size(ClientSize.Width, Math.Min(height, area.Height - 60));
        content.ResumeLayout();
        if (Visible)
            Place();
    }
}
