namespace Pace;

public enum ScreenEdge { Bottom, Top, Left, Right }

// Where tray popups open: against the taskbar or panel edge, beside the tray icon
// when its position is known, otherwise in that edge's trailing corner.
public readonly record struct PopupAnchor(PixelRect Area, PixelRect Screen, PixelRect? Icon = null)
{
    public bool IsEmpty => Area.Width == 0;
    public ScreenEdge Edge => Icon is { } icon ? IconEdge(icon) : PanelEdge();

    public static PopupAnchor For(Avalonia.Platform.Screen screen, PixelRect? icon = null) => new(screen.WorkingArea, screen.Bounds, icon);
    // A point on a panel that reserves space: on the screen but outside its work area.
    public static bool OnPanel(PixelRect screen, PixelRect area, PixelPoint point) => screen.Contains(point) && !area.Contains(point);

    public PixelPoint Place(Size size, double scale = 1)
    {
        int width = (int)Math.Ceiling(size.Width * scale), height = (int)Math.Ceiling(size.Height * scale);
        int inset = (int)Math.Ceiling(UiMetrics.ScreenInset * scale);
        int left = Area.X + inset, top = Area.Y + inset;
        int right = Math.Max(left, Area.Right - width - inset), bottom = Math.Max(top, Area.Bottom - height - inset);
        var centre = Icon?.Center;
        return Edge switch
        {
            ScreenEdge.Top => new(centre is { } t ? Math.Clamp(t.X - width / 2, left, right) : right, top),
            ScreenEdge.Left => new(left, centre is { } l ? Math.Clamp(l.Y - height / 2, top, bottom) : bottom),
            ScreenEdge.Right => new(right, centre is { } r ? Math.Clamp(r.Y - height / 2, top, bottom) : bottom),
            _ => new(centre is { } b ? Math.Clamp(b.X - width / 2, left, right) : right, bottom)
        };
    }

    // Entering and leaving frames travel toward the edge they belong to.
    public PixelPoint Slide(PixelPoint target, int offset) => Edge switch
    {
        ScreenEdge.Top => new(target.X, target.Y - offset),
        ScreenEdge.Left => new(target.X - offset, target.Y),
        ScreenEdge.Right => new(target.X + offset, target.Y),
        _ => new(target.X, target.Y + offset)
    };

    ScreenEdge IconEdge(PixelRect icon)
    {
        var c = icon.Center;
        // An icon on the taskbar sits outside the work area; otherwise (an auto-hidden
        // taskbar or overflow flyout) use the nearest work-area edge.
        if (c.Y >= Area.Bottom) return ScreenEdge.Bottom;
        if (c.Y < Area.Y) return ScreenEdge.Top;
        if (c.X >= Area.Right) return ScreenEdge.Right;
        if (c.X < Area.X) return ScreenEdge.Left;
        var distances = new[] { (Area.Bottom - c.Y, ScreenEdge.Bottom), (c.Y - Area.Y, ScreenEdge.Top), (c.X - Area.X, ScreenEdge.Left), (Area.Right - c.X, ScreenEdge.Right) };
        return distances.MinBy(d => d.Item1).Item2;
    }

    // Without an icon position, the edge where a panel reserves space; bottom if none does.
    ScreenEdge PanelEdge()
    {
        var reserved = new[] { (Screen.Bottom - Area.Bottom, ScreenEdge.Bottom), (Area.Y - Screen.Y, ScreenEdge.Top), (Area.X - Screen.X, ScreenEdge.Left), (Screen.Right - Area.Right, ScreenEdge.Right) };
        var widest = reserved.MaxBy(r => r.Item1);
        return widest.Item1 > 0 ? widest.Item2 : ScreenEdge.Bottom;
    }
}
