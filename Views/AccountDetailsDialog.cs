namespace Pace;

public sealed class AccountDetailsDialog : PageDialog
{
    readonly SectionList content = new();
    readonly IconButton refresh = new("refresh", "Refresh usage");
    (Reading Reading, string Name, bool Relative) shown;
    public string AccountKey { get; }
    public event Action? RefreshRequested;
    public AccountDetailsDialog(Reading reading, Settings settings)
    {
        AccountKey = reading.Account.Key;
        Title = "Pace · " + settings.DisplayName(reading.Account);
        var layout = new DockPanel();
        var back = new IconButton("back", "Back to usage");
        back.Click += (_, _) => Close(); NavigationFocus = back;
        var footer = new FooterBar("Account", back, refresh);
        DockPanel.SetDock(footer, Dock.Bottom);
        layout.Children.Add(footer); layout.Children.Add(new ScrollViewport(content));
        SetBody(layout);
        refresh.Click += (_, _) => RefreshRequested?.Invoke();
        UpdateReading(reading, settings, false);
    }
    public void UpdateReading(Reading reading, Settings settings, bool loading)
    {
        if (IsClosing) return;
        refresh.IsEnabled = !loading;
        var shown = (reading, settings.DisplayName(reading.Account), settings.RelativeResetTime);
        if (shown == this.shown)
        {
            // Same data: repaint relative times without rebuilding (keeps hover and tooltips).
            foreach (var row in content.Children.OfType<AccountRow>()) row.InvalidateVisual();
            return;
        }
        this.shown = shown;
        content.Children.Clear();
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.CardGap };
        header.Children.Add(new ServiceIcon(reading.Account.Service));
        var names = new StackPanel { Spacing = UiMetrics.InlineGap };
        names.Children.Add(Palette.Label(settings.DisplayName(reading.Account), true, Palette.Text, Palette.AccountSize));
        names.Children.Add(Palette.Label(reading.Account.Label));
        header.Children.Add(names);
        content.Children.Add(SectionStyle.Identity(header));
        var limits = reading.Limits ?? (reading.Weekly is { } weekly ? [new UsageLimit("weekly", "Weekly", weekly)] : new List<UsageLimit>());
        foreach (var limit in limits.OrderBy(limit => limit.Window == reading.Weekly ? 0 : 1))
        {
            var sectionReading = reading with { Weekly = limit.Window, Limits = [limit], Resets = null, ResetError = null };
            content.Children.Add(new AccountRow(sectionReading, limit.Name, showServiceMark: false, compactDetail: limit.Window != reading.Weekly) { RelativeResetTime = settings.RelativeResetTime });
        }
        var information = new InfoSection();
        var now = DateTimeOffset.UtcNow;
        if (reading.Resets is { } bank)
        {
            information.Add($"Banked resets · {bank.Available(now)}", bank.ExpiringSoon(now) ? Palette.Warning : Palette.Text);
            if (bank.Available(now) > 0) information.Add(bank.Details(now), bank.ExpiringSoon(now) ? Palette.Warning : Palette.Muted);
        }
        if (reading.ResetError is { } resetError) information.Add(resetError, Palette.Warning);
        string warning = LimitWarning.Summary(reading, now);
        if (warning.Length > 0) information.Add(warning, Palette.Warning);
        if (reading.Error is { } error) information.Add(error, Palette.Warning);
        information.Add($"Updated {reading.Updated.ToLocalTime():M/d · h:mm tt}", Palette.Muted);
        content.Children.Add(information);
        content.Measure(new Size(Width - 2 * UiMetrics.WindowBorderWidth, double.PositiveInfinity));
        Height = Math.Min(content.DesiredSize.Height + UiMetrics.ToolbarHeight + 2 * UiMetrics.WindowBorderWidth, AvailableHeight);
        if (IsVisible) Place();
    }
}
