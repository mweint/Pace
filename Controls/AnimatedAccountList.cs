namespace Pace;

public sealed class AnimatedAccountList : SectionList
{
    readonly MotionTween motion;
    AccountEditorRow? dragging;
    Point dragStart;
    int dragOriginalIndex;
    TopLevel? dragWindow;
    IPointer? dragPointer;
    bool moving;
    public event Action? Reordered;
    public void Watch(AccountEditorRow row)
    {
        row.Grip.AddHandler(PointerPressedEvent, (_, e) =>
        {
            if (!e.GetCurrentPoint(row.Grip).Properties.IsLeftButtonPressed) return;
            dragging = row; dragStart = e.GetPosition(this); moving = false;
            dragOriginalIndex = Children.IndexOf(row);
            dragPointer = e.Pointer;
            dragWindow = TopLevel.GetTopLevel(this);
            dragWindow?.AddHandler(KeyDownEvent, CancelDrag, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            e.Pointer.Capture(row.Grip); e.Handled = true;
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        row.Grip.AddHandler(PointerMovedEvent, (_, e) =>
        {
            if (dragging != row) return;
            var point = e.GetPosition(this);
            if (!moving && Math.Abs(point.X - dragStart.X) + Math.Abs(point.Y - dragStart.Y) <= UiMetrics.DragThreshold) return;
            moving = row.Dragging = true;
            int target = Children.OfType<AccountEditorRow>().Where(other => other != row)
                .TakeWhile(other => point.Y > other.Bounds.Y + other.Bounds.Height / 2).Count();
            MoveRow(row, target);
            if (Parent is ScrollViewport viewport && row.TranslatePoint(default, viewport) is { } position)
            {
                double pointerY = position.Y + e.GetPosition(row).Y;
                if (pointerY < UiMetrics.IconButtonSize) viewport.SetOffset(viewport.Offset - UiMetrics.InlineGap);
                else if (pointerY > viewport.Bounds.Height - UiMetrics.IconButtonSize) viewport.SetOffset(viewport.Offset + UiMetrics.InlineGap);
            }
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        row.Grip.AddHandler(PointerReleasedEvent, (_, e) =>
        {
            if (dragging != row) return;
            var viewport = Parent as ScrollViewport;
            bool inside = viewport == null ? new Rect(Bounds.Size).Contains(e.GetPosition(this)) : new Rect(viewport.Bounds.Size).Contains(e.GetPosition(viewport));
            FinishDrag(inside); e.Pointer.Capture(null); e.Handled = true;
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        row.Grip.PointerCaptureLost += (_, _) => FinishDrag(false);
        row.KeyDown += (_, e) =>
        {
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Alt) || e.Key is not (Key.Up or Key.Down)) return;
            MoveRow(row, Math.Clamp(Children.IndexOf(row) + (e.Key == Key.Up ? -1 : 1), 0, Children.Count - 1));
            Reordered?.Invoke(); e.Handled = true;
        };
    }
    void FinishDrag(bool commit)
    {
        if (dragging == null) return;
        dragWindow?.RemoveHandler(KeyDownEvent, CancelDrag);
        dragWindow = null;
        if (!commit) MoveRow(dragging, dragOriginalIndex);
        dragging.Dragging = false; dragging = null;
        dragPointer = null;
        FinishMotion();
        Reordered?.Invoke();
    }
    void CancelDrag(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || dragging == null) return;
        var pointer = dragPointer;
        FinishDrag(false);
        pointer?.Capture(null);
        e.Handled = true;
    }
    public void MoveRow(AccountEditorRow row, int index)
    {
        int previous = Children.IndexOf(row);
        if (previous < 0 || previous == index) return;
        var old = Children.ToDictionary(c => c, c => c.Bounds.Y + (c.RenderTransform is TranslateTransform t ? t.Y : 0));
        FinishMotion();
        Children.Move(previous, index);
        InvalidateArrange();
        double top = 0;
        var shifts = new Dictionary<Control, double>();
        foreach (var child in Children)
        {
            shifts[child] = old[child] - top;
            child.RenderTransform = new TranslateTransform(0, shifts[child]);
            top += child.DesiredSize.Height;
        }
        motion.Start(Motion.ReorderMilliseconds, t =>
        {
            foreach (var child in Children)
                if (shifts.TryGetValue(child, out double shift)) child.RenderTransform = new TranslateTransform(0, shift * (1 - t));
        }, FinishMotion);
    }
    public void FinishMotion()
    {
        motion.Dispose();
        foreach (var child in Children) child.RenderTransform = null;
    }
    public AnimatedAccountList()
    {
        motion = new(this);
        DetachedFromVisualTree += (_, _) => { FinishDrag(false); FinishMotion(); };
    }
}
