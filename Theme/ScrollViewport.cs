using Avalonia.Interactivity;

namespace Pace;

// Full-height content, custom square 4px thumb; no native scrollbars or hover expansion.
internal sealed class ScrollViewport : PaintedPanel
{
    public Control Content { get; }
    double offset, extent, dragStart, dragOffset;
    bool dragging;
    public bool Overflow => extent > Bounds.Height;
    public double Offset => offset;
    public ScrollViewport(Control content)
    {
        Content = content; Children.Add(content); ClipToBounds = true;
        AddHandler(GotFocusEvent, (_, e) =>
        {
            if (e.Source is Control focused && focused.TranslatePoint(default, this) is { } position)
            {
                if (position.Y < 0) SetOffset(offset + position.Y);
                else if (position.Y + focused.Bounds.Height > Bounds.Height) SetOffset(offset + position.Y + focused.Bounds.Height - Bounds.Height);
            }
        }, RoutingStrategies.Bubble);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        Content.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        extent = Content.DesiredSize.Height;
        if (extent > availableSize.Height)
        {
            Content.Measure(new Size(Math.Max(1, availableSize.Width - UiMetrics.ScrollbarWidth - UiMetrics.ScrollbarGap), double.PositiveInfinity));
            extent = Content.DesiredSize.Height;
        }
        return new(availableSize.Width, Math.Min(extent, availableSize.Height));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        offset = Math.Clamp(offset, 0, Math.Max(0, extent - finalSize.Height));
        Content.Arrange(new Rect(0, -offset, Math.Max(1, finalSize.Width - (extent > finalSize.Height ? UiMetrics.ScrollbarWidth + UiMetrics.ScrollbarGap : 0)), extent));
        return finalSize;
    }
    public void SetOffset(double value)
    {
        double scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        offset = Math.Clamp(Math.Truncate(value * scale) / scale, 0, Math.Max(0, extent - Bounds.Height));
        InvalidateArrange(); InvalidateVisual();
    }
    internal Rect Thumb
    {
        get
        {
            double track = Math.Max(1, Bounds.Height - 2 * UiMetrics.InlineGap);
            double scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
            double height = Math.Clamp(Math.Truncate(track * Bounds.Height / Math.Max(1, extent) * scale) / scale, Math.Min(track, UiMetrics.ScrollbarMinThumb), track);
            double top = UiMetrics.InlineGap + Math.Truncate((track - height) * offset / Math.Max(1, extent - Bounds.Height) * scale) / scale;
            return new(Bounds.Width - UiMetrics.ScrollbarWidth, top, UiMetrics.ScrollbarWidth, height);
        }
    }
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        SetOffset(offset - e.Delta.Y * UiMetrics.ScrollWheelDistance);
        e.Handled = true;
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        if (!Overflow || point.X < Bounds.Width - UiMetrics.ScrollbarWidth - UiMetrics.ScrollbarGap || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (!Thumb.Contains(point)) SetOffset(offset + (point.Y < Thumb.Top ? -Bounds.Height : Bounds.Height));
        dragging = true; dragStart = point.Y; dragOffset = offset;
        e.Pointer.Capture(this); e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (dragging) SetOffset(dragOffset + (e.GetPosition(this).Y - dragStart) * Math.Max(0, extent - Bounds.Height) / Math.Max(1, Bounds.Height - 2 * UiMetrics.InlineGap - Thumb.Height));
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e) { dragging = false; e.Pointer.Capture(null); }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { dragging = false; base.OnPointerCaptureLost(e); }
    protected override void DrawSurface(DrawingContext context)
    {
        if (Overflow) context.FillRectangle(Palette.Brush(Palette.ScrollThumb), Thumb);
    }
}
