using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Usage;

public sealed class TrayHover : WidgetForm
{
    List<(Reading Reading, string Name)> entries = [];
    Rectangle? iconAnchor;
    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var p = base.CreateParams;
            p.ExStyle |= 0x08000000;
            return p;
        }
    }

    public TrayHover()
    {
        Font = Palette.AccountFont();
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(UiMetrics.HoverWidth, 100);
    }

    public void UpdateEntries(List<Reading> readings, Settings settings)
    {
        entries = readings.Select(r => (r, settings.DisplayName(r.Account))).ToList();
        SizeContent();
        if (Visible && iconAnchor is { } anchor)
            Place(anchor);
        Invalidate();
    }

    public void Open(Point anchor)
        => Open(new Rectangle(anchor, Size.Empty));

    public void Open(Rectangle anchor)
    {
        // Create the native window before positioning so first-show defaults cannot move it.
        _ = Handle;
        iconAnchor = anchor;
        Place(anchor);
        Show();
        // Showing on a different-DPI monitor may update the window's DPI.
        SizeContent();
        Place(anchor);
    }

    void SizeContent() => ClientSize = new Size(
        (int)(UiMetrics.HoverWidth * DeviceDpi / (float)UiMetrics.BaseDpi),
        (int)((2 * UiMetrics.HoverInset + UiMetrics.ServiceIconSize + (Math.Max(1, entries.Count) - 1) * UiMetrics.HoverRowHeight) * DeviceDpi / (float)UiMetrics.BaseDpi));

    void Place(Rectangle anchor)
    {
        var area = Screen.FromRectangle(anchor).WorkingArea;
        int gap = (int)Math.Round(UiMetrics.ScreenInset * DeviceDpi / (float)UiMetrics.BaseDpi);
        int x = anchor.Left + anchor.Width / 2 - Width / 2;
        int y = Math.Min(anchor.Top, area.Bottom) - Height - gap;
        Location = new Point(Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - Width)),
            Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - Height - gap)));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = DeviceDpi / (float)UiMetrics.BaseDpi;
        g.ScaleTransform(scale, scale);
        float width = Width / scale;
        using var valueFont = Palette.BodyFont();
        using var text = new SolidBrush(Palette.Text);
        using var nameFormat = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        if (entries.Count == 0)
        {
            g.DrawString("No tray accounts selected", valueFont, text, UiMetrics.HoverInset, UiMetrics.HoverInset);
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var (reading, name) = entries[i];
            float y = UiMetrics.HoverInset + i * UiMetrics.HoverRowHeight;
            ServiceMark.Draw(g, reading.Account.Service, UiMetrics.HoverInset, y);
            var now = DateTimeOffset.UtcNow;
            using var status = new SolidBrush(reading.Weekly is { } weekly && reading.Error == null ? Palette.Status(weekly, now) : Palette.Status((Pace?)null));
            string value = reading.Weekly is { } limit && reading.Error == null ? PaceMath.HoverSummary(limit, now) : "Unavailable";
            using var valueFormat = new StringFormat(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Far,
                FormatFlags = StringFormatFlags.NoWrap
            };
            float valueWidth = (float)Math.Ceiling(g.MeasureString(value, valueFont, int.MaxValue, valueFormat).Width) + UiMetrics.InlineGap;
            float valueLeft = width - UiMetrics.HoverInset - valueWidth;
            float nameLeft = UiMetrics.HoverInset + UiMetrics.ServiceIconSize + UiMetrics.CardGap;
            float rowHeight = Math.Max(Font.GetHeight(UiMetrics.BaseDpi), valueFont.GetHeight(UiMetrics.BaseDpi));
            g.DrawString(name, Font, text, new RectangleF(nameLeft, y, Math.Max(1, valueLeft - nameLeft - UiMetrics.CardGap), rowHeight), nameFormat);
            g.DrawString(value, valueFont, status, new RectangleF(valueLeft, y, valueWidth, rowHeight), valueFormat);
        }
    }
}
