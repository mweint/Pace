namespace Pace;

public sealed class TrayHover : WidgetWindow
{
    readonly HoverContent content = new();
    PixelRect? iconAnchor;
    public TrayHover()
    {
        Width = UiMetrics.HoverWidth;
        ShowActivated = false;
        Focusable = false;
        Opened += (_, _) => DesktopIntegration.PreventActivation(this);
        // Native hover paints at the window's inset, rather than inside the
        // border padding used by the navigable pages.
        Content = content;
    }
    public void UpdateEntries(List<Reading> readings, Settings settings)
    {
        content.Entries = readings.Select(r => (r, settings.DisplayName(r.Account))).ToList();
        Height = 2 * UiMetrics.HoverInset + UiMetrics.ServiceIconSize + (Math.Max(1, readings.Count) - 1) * UiMetrics.HoverRowHeight;
        content.InvalidateVisual();
        if (IsVisible && iconAnchor is { } anchor) PlaceAt(anchor);
    }
    public void Open(PixelRect anchor) { iconAnchor = anchor; PlaceAt(anchor); Show(); PlaceAt(anchor); }
    void PlaceAt(PixelRect anchor)
    {
        var screen = Screens.ScreenFromPoint(anchor.Position) ?? Screens.Primary;
        if (screen == null) return;
        double scale = screen.Scaling;
        var area = screen.WorkingArea;
        int width = (int)Math.Ceiling(Width * scale), height = (int)Math.Ceiling(Height * scale), gap = (int)Math.Ceiling(UiMetrics.ScreenInset * scale);
        Position = new(Math.Clamp(anchor.X + anchor.Width / 2 - width / 2, area.X, Math.Max(area.X, area.Right - width)),
            Math.Clamp(Math.Min(anchor.Y, area.Bottom) - height - gap, area.Y, Math.Max(area.Y, area.Bottom - height - gap)));
    }
    sealed class HoverContent : PaintedPanel
    {
        public List<(Reading Reading, string Name)> Entries { get; set; } = [];
        protected override void DrawSurface(DrawingContext context)
        {
            SectionStyle.DrawWindowFrame(context, Bounds.Size);
            if (Entries.Count == 0) { context.DrawText(Palette.Format("No tray accounts selected", Palette.Text), new Point(UiMetrics.HoverInset, UiMetrics.HoverInset)); return; }
            for (int i = 0; i < Entries.Count; i++)
            {
                var (reading, name) = Entries[i];
                double y = UiMetrics.HoverInset + i * UiMetrics.HoverRowHeight;
                ServiceMark.Draw(context, reading.Account.Service, new Point(UiMetrics.HoverInset, y), TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
                var now = DateTimeOffset.UtcNow;
                var value = Palette.Format(reading.Weekly is { } limit && reading.Error == null ? PaceMath.HoverSummary(limit, now) : "Unavailable",
                    reading.Weekly is { } weekly && reading.Error == null ? Palette.Status(weekly, now) : Palette.Muted);
                double valueWidth = Math.Ceiling(value.Width) + UiMetrics.InlineGap;
                double valueLeft = Bounds.Width - UiMetrics.HoverInset - valueWidth, nameLeft = UiMetrics.HoverInset + UiMetrics.ServiceIconSize + UiMetrics.CardGap;
                var title = Palette.Line(name, Palette.Text, valueLeft - nameLeft - UiMetrics.CardGap, Palette.AccountSize, true);
                context.DrawText(title, new Point(nameLeft, y));
                context.DrawText(value, new Point(Bounds.Width - UiMetrics.HoverInset - value.Width, y));
            }
        }
    }
}
