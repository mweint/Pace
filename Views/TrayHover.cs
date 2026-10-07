using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Usage;

public sealed class TrayHover : WidgetForm
{
    List<(Reading Reading, string Name)> entries = [];
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
        ClientSize = new Size((int)(UiMetrics.HoverWidth * DeviceDpi / 96f), (int)((UiMetrics.HoverVerticalInset + Math.Max(1, entries.Count) * UiMetrics.HoverRowHeight) * DeviceDpi / 96f));
        Invalidate();
    }

    public void Open(Point anchor)
    {
        // Create the native window before positioning so first-show defaults cannot move it.
        _ = Handle;
        var area = Screen.FromPoint(anchor).WorkingArea;
        Location = new Point(Math.Clamp(anchor.X - Width / 2, area.Left, Math.Max(area.Left, area.Right - Width)), Math.Clamp(anchor.Y - Height - 12, area.Top, Math.Max(area.Top, area.Bottom - Height)));
        Show();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = DeviceDpi / 96f;
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
            g.DrawString("No tray accounts selected", valueFont, text, 14, 13);
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var (reading, name) = entries[i];
            float y = 11 + i * UiMetrics.HoverRowHeight;
            ServiceMark.Draw(g, reading.Account.Service, 14, y + 3);
            var pace = reading.Weekly is { } w && reading.Error == null ? PaceMath.Calculate(w, DateTimeOffset.UtcNow) : null;
            using var status = new SolidBrush(Palette.Status(pace));
            string value = PaceMath.HoverSummary(pace);
            using var valueFormat = new StringFormat(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Far,
                FormatFlags = StringFormatFlags.NoWrap
            };
            float valueWidth = (float)Math.Ceiling(g.MeasureString(value, valueFont, int.MaxValue, valueFormat).Width) + 4;
            float valueLeft = width - 14 - valueWidth;
            g.DrawString(name, Font, text, new RectangleF(38, y, Math.Max(1, valueLeft - 38 - UiMetrics.CardGap), 24), nameFormat);
            g.DrawString(value, valueFont, status, new RectangleF(valueLeft, y, valueWidth, 24), valueFormat);
        }
    }
}
