using System.Drawing.Drawing2D;

namespace Usage;

public sealed class AccountRow : Control
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
    public AccountRow(Reading reading, string alias, bool showServiceMark = true)
    {
        this.showServiceMark = showServiceMark;
        detail = new AccountDetailTip(this);
        Reading = reading;
        Alias = alias;
        DoubleBuffered = true;
        BackColor = Palette.Card;
        Height = UiMetrics.AccountHeight;
        Controls.Add(resetBadge);
        resetBadge.Click += (_, e) => OnClick(e);
        UpdateReading(reading, alias);
    }

    public void UpdateReading(Reading reading, string alias)
    {
        Reading = reading;
        Alias = alias;
        AccessibleName = $"{reading.Account.Service}, {alias}, {reading.Weekly?.Used:0}% used, {PaceMath.Summary(reading.Weekly is { } w ? PaceMath.Calculate(w, DateTimeOffset.UtcNow) : null)}";
        detail.Text = reading.Error == null ? "" : $"{reading.Account.Service} · {reading.Account.Label}\n{reading.Error}\nLast successful reading: {reading.Updated.ToLocalTime():g}";
        resetBadge.UpdateBank(reading.Resets, reading.Error != null || reading.ResetError != null);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float s = DeviceDpi / 96f;
        g.ScaleTransform(s, s);
        float width = Width / s;
        using var title = Palette.AccountFont();
        using var small = Palette.BodyFont();
        using var fg = new SolidBrush(Palette.Text);
        using var muted = new SolidBrush(Palette.Muted);
        using var nameFormat = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter
        };
        using var rightFormat = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Far
        };
        using var countdownFormat = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        if (showServiceMark)
            ServiceMark.Draw(g, Reading.Account.Service, 14, 15);
        float titleX = showServiceMark ? 38 : 14;
        g.DrawString(Alias, title, fg, new RectangleF(titleX, 11, width - titleX - 171, 22), nameFormat);
        var now = DateTimeOffset.UtcNow;
        if (Reading.Weekly is not { } w)
        {
            g.DrawString(Reading.Error ?? "Refreshing…", small, muted, new RectangleF(14, 42, width - 28, 46));
            return;
        }

        var pace = PaceMath.Calculate(w, now);
        using var accent = new SolidBrush(Palette.Status(pace));
        g.DrawString($"{w.Used:0}% · {PaceMath.Compact(pace)}", small, accent, new RectangleF(width - 177, 13, 163, 22), rightFormat);
        float x = 14, y = 44, bw = width - 28, bh = 8;
        using var track = new SolidBrush(Palette.Track);
        g.FillRectangle(track, x, y, bw, bh);
        g.FillRectangle(accent, x, y, bw * (float)Math.Clamp(w.Used / 100, 0, 1), bh);
        using var day = new Pen(Palette.DayDivider, 1);
        for (int i = 1; i < 7; i++)
            g.DrawLine(day, x + bw * i / 7, y, x + bw * i / 7, y + bh);
        if (pace != null)
        {
            float tick = x + bw * (float)(pace.Expected / 100);
            using var marker = new Pen(Palette.Text, 2);
            g.DrawLine(marker, tick, y - 4, tick, y + bh + 4);
            g.FillPolygon(fg, new PointF[] { new(tick - 3, y - 7), new(tick + 3, y - 7), new(tick, y - 3) });
        }

        var resetText = now >= w.Reset ? "Reset passed · refresh needed" : $"Resets in {PaceMath.Duration(w.Reset - now)}";
        if (resetBadge.Visible)
        {
            // Keep badge and countdown on the existing line, clear of the right status.
            float textWidth = Math.Min(118, g.MeasureString(resetText, small).Width);
            g.DrawString(resetText, small, muted, new RectangleF(14, 66, 118, 20), countdownFormat);
            resetBadge.SetBounds((int)((14 + textWidth + 4) * s), (int)(62 * s), (int)(42 * s), (int)(24 * s));
        }
        else
            g.DrawString(resetText, small, muted, 14, 66);
        if (Reading.Error != null)
        {
            string status = Reading.Error.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || Reading.Error.StartsWith("Retry") ? "Stale · rate limited" : "Stale · hover for details";
            g.DrawString(status, small, muted, new RectangleF(180, 66, width - 194, 20), rightFormat);
        }
        else if (PaceMath.AheadLabel(pace) is { } aheadLabel)
            g.DrawString(aheadLabel, small, accent, new RectangleF(180, 66, width - 194, 20), rightFormat);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            detail.Dispose();
        base.Dispose(disposing);
    }
}
