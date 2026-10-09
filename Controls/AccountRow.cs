namespace Pace;

public sealed class AccountRow : PaintedPanel, IThemedSection
{
    public Reading Reading { get; private set; }
    public string Alias { get; private set; }
    readonly bool showServiceMark, showSupplementalLimits, compactDetail;
    Preference? preference;
    readonly ResetBadge resetBadge = new();
    bool separator;
    public bool RelativeResetTime { get; set; }
    public bool ShowSeparator { get => separator; set { separator = value; InvalidateVisual(); } }
    SectionGroup IThemedSection.Group => compactDetail ? SectionGroup.SecondaryLimits : SectionGroup.None;
    public event Action<Account>? Activated;
    public AccountRow(Reading reading, string alias, bool showServiceMark = true, bool showSupplementalLimits = false, Preference? preference = null, bool compactDetail = false)
    {
        Reading = reading; Alias = alias;
        this.showServiceMark = showServiceMark; this.showSupplementalLimits = showSupplementalLimits;
        this.preference = preference; this.compactDetail = compactDetail;
        Focusable = showServiceMark;
        Cursor = new Cursor(StandardCursorType.Hand);
        Children.Add(resetBadge);
        UpdateReading(reading, alias, preference);
    }
    internal AccountRowLayout LayoutFor(double width) => AccountRowLayout.Create(width, Reading.Weekly != null,
        showSupplementalLimits ? CompactLimitBars.For(Reading, preference).Count : 0, compactDetail);
    public void UpdateReading(Reading reading, string alias, Preference? preference = null)
    {
        Reading = reading; Alias = alias;
        if (preference != null) this.preference = preference;
        var now = DateTimeOffset.UtcNow;
        var tips = new List<string>();
        if (reading.Weekly is { } window) tips.Add(PaceMath.ResetCountdown(window.Reset, now));
        resetBadge.UpdateBank(reading.Resets, reading.Error != null || reading.ResetError != null);

        if (reading.Error != null) tips.Add($"{reading.Account.Service} · {reading.Account.Label}\n{reading.Error}\nLast successful reading: {reading.Updated.ToLocalTime():g}");
        string warning = showSupplementalLimits ? LimitWarning.Summary(LimitWarning.Hidden(reading, this.preference, now)) : "";
        if (warning.Length > 0) tips.Add(warning + "\nClick for account details");
        ThemedToolTip.Set(this, string.Join("\n", tips));
        Avalonia.Automation.AutomationProperties.SetName(this, alias + ", " + (reading.Weekly is { } w ? $"{w.Used:0}% used, {PaceMath.Summary(w, now)}" : reading.Error ?? "Unavailable"));
        InvalidateMeasure(); InvalidateVisual();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        resetBadge.Measure(availableSize);
        return new(availableSize.Width, LayoutFor(availableSize.Width).Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var layout = LayoutFor(finalSize.Width);
        if (Reading.Weekly is { } window)
        {
            var (reset, _) = Metadata(window, layout, DateTimeOffset.UtcNow);
            resetBadge.Arrange(new Rect(layout.Metadata.Left + UiMetrics.TextInset + Math.Ceiling(reset.WidthIncludingTrailingWhitespace) + UiMetrics.InlineGap,
                layout.Metadata.Center.Y - UiMetrics.ResetBadgeHeight / 2d, UiMetrics.ResetBadgeWidth, UiMetrics.ResetBadgeHeight));
        }
        return finalSize;
    }
    // Reset time on the left (trimmed to leave room for the badge and status), status on the right.
    (FormattedText Reset, FormattedText? Status) Metadata(UsageWindow window, AccountRowLayout layout, DateTimeOffset now)
    {
        string? label = StatusLabel(window, now);
        var status = label == null ? null : Palette.Format(label, Reading.Error == null ? Palette.Status(window, now) : Palette.Muted);
        bool hasBank = Reading.Resets is { } bank && bank.Available(now) > 0;
        double reserved = UiMetrics.TextInset + (status == null ? 0 : Math.Ceiling(status.Width) + UiMetrics.CardGap) + (hasBank ? UiMetrics.ResetBadgeWidth + UiMetrics.InlineGap : 0);
        string reset = RelativeResetTime ? PaceMath.ResetCountdown(window.Reset, now) : PaceMath.ResetLabel(window.Reset, now);
        return (Palette.Line(reset, Palette.Muted, layout.Metadata.Width - reserved), status);
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton == MouseButton.Left && new Rect(Bounds.Size).Contains(e.GetPosition(this))) Activated?.Invoke(Reading.Account);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space) { Activated?.Invoke(Reading.Account); e.Handled = true; }
        base.OnKeyDown(e);
    }
    protected override void DrawSurface(DrawingContext context)
    {
        var layout = LayoutFor(Bounds.Width);
        var header = layout.Header;
        SectionStyle.DrawSeparator(context, Bounds.Width, ShowSeparator);
        if (showServiceMark) ServiceMark.Draw(context, Reading.Account.Service, new Point(header.Left, header.Top + (header.Height - UiMetrics.ServiceIconSize) / 2), TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        double titleX = header.Left + (showServiceMark ? UiMetrics.ServiceIconSize + UiMetrics.CardGap : 0);
        var now = DateTimeOffset.UtcNow;
        var w = Reading.Weekly;
        var value = w == null ? null : Palette.Format($"{w.Used:0}% · {PaceMath.Compact(w, now)}", Palette.Status(w, now));
        if (value != null) context.DrawText(value, new Point(header.Right - value.Width, Centered(header, value)));
        bool warning = showSupplementalLimits && LimitWarning.Hidden(Reading, preference, now).Count > 0;
        double reserved = (value == null ? 0 : Math.Ceiling(value.Width) + UiMetrics.CardGap) + (warning ? UiMetrics.WarningDotSize + UiMetrics.InlineGap : 0);
        var title = Palette.Line(Alias, Palette.Text, header.Right - titleX - reserved, compactDetail ? Palette.BodySize : Palette.AccountSize, true);
        context.DrawText(title, new Point(titleX, Centered(header, title)));
        if (warning) context.DrawEllipse(Palette.Brush(Palette.Warning), null, new Point(titleX + title.Width + UiMetrics.InlineGap + UiMetrics.WarningDotSize / 2d, header.Center.Y), UiMetrics.WarningDotSize / 2d, UiMetrics.WarningDotSize / 2d);
        if (w == null)
        {
            var error = Palette.Format(Reading.Error ?? "Refreshing…", Palette.Muted);
            error.MaxTextWidth = layout.Error.Width;
            error.MaxLineCount = 2;
            error.Trimming = TextTrimming.CharacterEllipsis;
            context.DrawText(error, layout.Error.TopLeft + new Point(UiMetrics.TextInset, 0));
            return;
        }
        UsageBar.Draw(context, w, layout.Bar, true);
        var (reset, status) = Metadata(w, layout, now);
        context.DrawText(reset, new Point(layout.Metadata.Left + UiMetrics.TextInset, Centered(layout.Metadata, reset)));
        if (status != null) context.DrawText(status, new Point(layout.Metadata.Right - status.Width, Centered(layout.Metadata, status)));
        if (showSupplementalLimits) CompactLimitBars.Draw(context, Reading, layout, preference);
    }
    static double Centered(Rect area, FormattedText text) => Math.Round(area.Top + (area.Height - text.Height) / 2);
    string? StatusLabel(UsageWindow window, DateTimeOffset now) => Reading.Error != null
        ? Reading.RateLimited ? "Stale · rate limited" : "Stale · hover for details"
        : PaceMath.Classify(window, now) != PaceState.Exhausted ? PaceMath.AheadLabel(PaceMath.Calculate(window, now)) : null;
}
