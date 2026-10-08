using System.Drawing.Drawing2D;

namespace Usage;

public sealed class AccountRow : Control, IThemedSection
{
    readonly AccountDetailTip detail;
    readonly ResetBadge resetBadge = new();
    public Reading Reading
    {
        get; private set;
    }
    public string Alias
    {
        get; private set;
    }

    readonly bool showServiceMark;
    readonly bool showSupplementalLimits;
    readonly bool compactDetail;
    SectionGroup IThemedSection.Group => compactDetail ? SectionGroup.SecondaryLimits : SectionGroup.None;
    Preference? preference;
    public bool RelativeResetTime { get; set; }
    bool showSeparator;
    public bool ShowSeparator { get => showSeparator; set { if (showSeparator != value) { showSeparator = value; Invalidate(); } } }
    public AccountRow(Reading reading, string alias, bool showServiceMark = true, bool showSupplementalLimits = false, Preference? preference = null, bool compactDetail = false)
    {
        this.showServiceMark = showServiceMark;
        this.showSupplementalLimits = showSupplementalLimits;
        this.preference = preference;
        this.compactDetail = compactDetail;
        detail = new AccountDetailTip(this);
        Reading = reading;
        Alias = alias;
        DoubleBuffered = true;
        SectionStyle.Apply(this);
        resetBadge.BackColor = BackColor;
        Controls.Add(resetBadge);
        resetBadge.Click += (_, e) => OnClick(e);
        UpdateReading(reading, alias);
    }

    public void UpdateReading(Reading reading, string alias, Preference? preference = null)
    {
        Reading = reading;
        Alias = alias;
        if (preference != null)
            this.preference = preference;
        using var title = TitleFont();
        using var body = Palette.BodyFont();
        Height = (int)Math.Ceiling(LayoutFor(title, body).Height * DeviceDpi / (float)UiMetrics.BaseDpi);
        AccessibleName = $"{reading.Account.Service}, {alias}, {reading.Weekly?.Used:0}% used, {(reading.Weekly is { } w ? PaceMath.Summary(w, DateTimeOffset.UtcNow) : PaceMath.Summary(null))}";
        var details = new List<string>();
        if (reading.Weekly is { } resetWindow)
        {
            details.Add(PaceMath.ResetCountdown(resetWindow.Reset, DateTimeOffset.UtcNow));
            AccessibleName += ", " + PaceMath.ResetLabel(resetWindow.Reset, DateTimeOffset.UtcNow);
        }
        if (reading.Error != null)
            details.Add($"{reading.Account.Service} · {reading.Account.Label}\n{reading.Error}\nLast successful reading: {reading.Updated.ToLocalTime():g}");
        var warning = showSupplementalLimits
            ? LimitWarning.Summary(LimitWarning.Hidden(reading, this.preference, DateTimeOffset.UtcNow)) : "";
        if (warning.Length > 0)
        {
            details.Add(warning + "\nClick for account details");
            AccessibleName += ", " + warning;
        }
        detail.Text = string.Join("\n", details);
        resetBadge.UpdateBank(reading.Resets, reading.Error != null || reading.ResetError != null);
        Invalidate();
    }

    Font TitleFont() => compactDetail ? Palette.DetailLimitFont() : Palette.AccountFont();

    AccountRowLayout LayoutFor(Font title, Font body) => AccountRowLayout.Create(Width * (float)UiMetrics.BaseDpi / DeviceDpi,
        title, body, Reading.Weekly != null, showSupplementalLimits ? CompactLimitBars.For(Reading, preference).Count : 0, compactDetail);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float s = DeviceDpi / (float)UiMetrics.BaseDpi;
        g.ScaleTransform(s, s);
        using var title = TitleFont();
        using var small = Palette.BodyFont();
        var layout = LayoutFor(title, small);
        using var fg = new SolidBrush(Palette.Text);
        using var muted = new SolidBrush(Palette.Muted);
        using var nameFormat = new StringFormat(StringFormat.GenericTypographic)
        {
            FormatFlags = StringFormatFlags.NoWrap
        };
        using var rightFormat = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Far,
            FormatFlags = StringFormatFlags.NoWrap
        };
        using var countdownFormat = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        SectionStyle.DrawSeparator(g, Width / s, ShowSeparator);
        if (showServiceMark)
            ServiceMark.Draw(g, Reading.Account.Service, layout.Header.Left, layout.Header.Top + (layout.Header.Height - UiMetrics.ServiceIconSize) / 2);
        float titleX = layout.Header.Left + (showServiceMark ? UiMetrics.ServiceIconSize + UiMetrics.CardGap : 0);
        bool hasWarning = showSupplementalLimits && LimitWarning.Hidden(Reading, preference, DateTimeOffset.UtcNow).Count > 0;
        float nameWidth = Math.Max(0, layout.Header.Right - titleX - 171 -
            (hasWarning ? UiMetrics.WarningDotSize + UiMetrics.InlineGap : 0));
        string displayedName = FitName(g, Alias, title, nameFormat, nameWidth);
        g.DrawString(displayedName, title, fg, titleX, AccountRowLayout.TextTop(layout.Header, title), nameFormat);
        if (hasWarning)
        {
            using var warning = new SolidBrush(Palette.Warning);
            float dotX = titleX + g.MeasureString(displayedName, title, int.MaxValue, nameFormat).Width + UiMetrics.InlineGap;
            g.FillEllipse(warning, dotX, layout.Header.Top + (layout.Header.Height - UiMetrics.WarningDotSize) / 2,
                UiMetrics.WarningDotSize, UiMetrics.WarningDotSize);
        }
        var now = DateTimeOffset.UtcNow;
        if (Reading.Weekly is not { } w)
        {
            g.DrawString(Reading.Error ?? "Refreshing…", small, muted, layout.Error);
            return;
        }

        var pace = PaceMath.Calculate(w, now);
        using var accent = new SolidBrush(Palette.Status(w, now));
        g.DrawString($"{w.Used:0}% · {PaceMath.Compact(w, now)}", small, accent, new RectangleF(layout.Header.Right - 163, AccountRowLayout.TextTop(layout.Header, small), 163, layout.Header.Height), rightFormat);
        float x = layout.Bar.Left, y = layout.Bar.Top, bw = layout.Bar.Width, bh = layout.Bar.Height;
        using var track = new SolidBrush(Palette.Track);
        g.FillRectangle(track, x, y, bw, bh);
        g.FillRectangle(accent, x, y, bw * (float)Math.Clamp(w.Used / 100, 0, 1), bh);
        int segments = AccountRowLayout.SegmentCount(w.Period);
        using var divider = new Pen(Palette.BarDivider, UiMetrics.BorderWidth);
        for (int i = 1; i < segments; i++)
            g.DrawLine(divider, x + bw * i / segments, y, x + bw * i / segments, y + bh);
        if (pace != null)
        {
            float tick = x + bw * (float)(pace.Expected / 100);
            using var marker = new SolidBrush(Palette.Text);
            float markerX = Math.Clamp(tick - UiMetrics.PaceMarkerWidth / 2f, x, x + bw - UiMetrics.PaceMarkerWidth);
            g.FillRectangle(marker, markerX, y - UiMetrics.PaceMarkerOverhang,
                UiMetrics.PaceMarkerWidth, bh + 2 * UiMetrics.PaceMarkerOverhang);
        }

        var resetText = RelativeResetTime ? PaceMath.ResetCountdown(w.Reset, now) : PaceMath.ResetLabel(w.Reset, now);
        float metaY = AccountRowLayout.TextTop(layout.Metadata, small);
        string? status = null;
        if (Reading.Error != null)
            status = Reading.Error.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || Reading.Error.StartsWith("Retry") ? "Stale · rate limited" : "Stale · hover for details";
        else if (PaceMath.Classify(w, now) != PaceState.Exhausted && PaceMath.AheadLabel(pace) is { } aheadLabel)
            status = aheadLabel;
        float statusWidth = status == null ? 0 : g.MeasureString(status, small, int.MaxValue, rightFormat).Width;
        float badgeSpace = resetBadge.Visible ? UiMetrics.ResetBadgeWidth + UiMetrics.InlineGap : 0;
        float resetWidth = Math.Max(0, layout.Metadata.Width - statusWidth - badgeSpace - (status == null ? 0 : UiMetrics.CardGap));
        g.DrawString(resetText, small, muted, new RectangleF(layout.Metadata.Left, metaY, resetWidth, layout.Metadata.Height), countdownFormat);
        if (resetBadge.Visible)
        {
            float textWidth = Math.Min(resetWidth, g.MeasureString(resetText, small).Width);
            resetBadge.SetBounds((int)((layout.Metadata.Left + textWidth + UiMetrics.InlineGap) * s), (int)(layout.Metadata.Top * s), (int)(UiMetrics.ResetBadgeWidth * s), (int)(layout.Metadata.Height * s));
        }
        if (status != null)
            g.DrawString(status, small, Reading.Error == null ? accent : muted,
                new RectangleF(layout.Metadata.Right - statusWidth, metaY, statusWidth, layout.Metadata.Height), rightFormat);
        if (showSupplementalLimits)
            CompactLimitBars.Draw(g, Reading, layout, preference);
    }

    internal static string FitName(Graphics graphics, string name, Font font, StringFormat format, float width)
    {
        if (graphics.MeasureString(name, font, int.MaxValue, format).Width <= width)
            return name;
        if (graphics.MeasureString("…", font, int.MaxValue, format).Width > width)
            return "";
        var starts = System.Globalization.StringInfo.ParseCombiningCharacters(name);
        int low = 0, high = starts.Length;
        while (low < high)
        {
            int count = (low + high + 1) / 2;
            int end = count == starts.Length ? name.Length : starts[count];
            if (graphics.MeasureString(name[..end] + "…", font, int.MaxValue, format).Width <= width)
                low = count;
            else
                high = count - 1;
        }
        return name[..(low == starts.Length ? name.Length : starts[low])] + "…";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            detail.Dispose();
        base.Dispose(disposing);
    }
}
