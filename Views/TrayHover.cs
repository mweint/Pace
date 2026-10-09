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
        var screen = Screens.ScreenFromPoint(anchor.Center) ?? Screens.Primary;
        if (screen == null) return;
        Position = PopupAnchor.For(screen, anchor).Place(new Size(Width, Height), screen.Scaling);
    }
    sealed class HoverContent : PaintedPanel
    {
        public List<(Reading Reading, string Name)> Entries { get; set; } = [];
        protected override void DrawSurface(DrawingContext context)
        {
            SectionStyle.DrawWindowFrame(context, Bounds.Size);
            if (Entries.Count == 0) { context.DrawText(Palette.Format("No tray accounts selected", Palette.Text), new Point(UiMetrics.HoverInset + UiMetrics.TextInset, UiMetrics.HoverInset)); return; }
            for (int i = 0; i < Entries.Count; i++)
            {
                var (reading, name) = Entries[i];
                double y = UiMetrics.HoverInset + i * UiMetrics.HoverRowHeight;
                ServiceMark.Draw(context, reading.Account.Service, new Point(UiMetrics.HoverInset, y), TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
                var now = DateTimeOffset.UtcNow;
                var value = Palette.Format(PaceMath.HoverSummary(reading, now),
                    reading.Weekly is { } weekly && reading.Error == null ? Palette.Status(weekly, now) : Palette.Muted);
                double valueWidth = Math.Ceiling(value.Width) + UiMetrics.InlineGap;
                double valueLeft = Bounds.Width - UiMetrics.HoverInset - valueWidth, nameLeft = UiMetrics.HoverInset + UiMetrics.ServiceIconSize + UiMetrics.CardGap;
                var title = Palette.Line(name, Palette.Text, valueLeft - nameLeft - UiMetrics.TextInset - UiMetrics.CardGap, Palette.AccountSize, true);
                context.DrawText(title, new Point(nameLeft + UiMetrics.TextInset, y));
                context.DrawText(value, new Point(Bounds.Width - UiMetrics.HoverInset - value.Width, y));
            }
        }
    }
}
