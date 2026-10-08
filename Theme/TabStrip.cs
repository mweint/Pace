namespace Pace;

internal sealed class TabStrip : PaintedPanel
{
    readonly string[] labels;
    readonly Button[] buttons;
    readonly MotionTween animation;
    double indicatorPosition;
    int selected;
    public event Action<int>? SelectionChanged;
    public int SelectedIndex
    {
        get => selected;
        set
        {
            if (value == selected || value < 0 || value >= labels.Length) return;
            double from = indicatorPosition;
            selected = value;
            animation.Start(Motion.TabMilliseconds, t => { indicatorPosition = from + (selected - from) * t; InvalidateVisual(); });
            SelectionChanged?.Invoke(selected);
        }
    }
    public TabStrip(params string[] labels)
    {
        animation = new(this);
        this.labels = labels;
        Focusable = true;
        FocusAdorner = null;
        Height = UiMetrics.TabStripHeight;
        buttons = labels.Select((label, index) =>
        {
            var button = new Button { Focusable = false, FocusAdorner = null, Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                Template = ControlSurface.Template<Button>(),
                Cursor = new Cursor(StandardCursorType.Hand) };
            Avalonia.Automation.AutomationProperties.SetName(button, label);
            button.Click += (_, _) => { Focus(NavigationMethod.Pointer); SelectedIndex = index; };
            return button;
        }).ToArray();
        Children.AddRange(buttons);
        DetachedFromVisualTree += (_, _) => animation.Dispose();
    }
    internal double TabWidth(int index) => Palette.TextWidth(labels[index], Palette.TitleSize, true) + 2 * UiMetrics.TabTextInset;
    Rect TabBounds(int index)
    {
        double left = UiMetrics.ContentInset;
        for (int i = 0; i < index; i++) left += TabWidth(i) + UiMetrics.TabGap;
        return new(left, 0, TabWidth(index), Height - UiMetrics.TabIndicatorHeight);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var button in buttons) button.Measure(new Size(availableSize.Width, Height));
        return new(availableSize.Width, Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        for (int i = 0; i < buttons.Length; i++) buttons[i].Arrange(TabBounds(i));
        return finalSize;
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        int next = e.Key switch { Key.Left => Math.Max(0, selected - 1), Key.Right => Math.Min(labels.Length - 1, selected + 1), Key.Home => 0, Key.End => labels.Length - 1, _ => -1 };
        if (next >= 0) { SelectedIndex = next; e.Handled = true; }
        base.OnKeyDown(e);
    }
    protected override void DrawSurface(DrawingContext context)
    {
        for (int i = 0; i < labels.Length; i++)
        {
            var bounds = TabBounds(i);
            var ink = i == selected ? Palette.Text : Palette.Muted;
            var text = Palette.Format(labels[i], ink, Palette.TitleSize, true);
            context.DrawText(text, new Point(bounds.X + UiMetrics.TabTextInset, Math.Round((bounds.Height - text.Height) / 2)));
        }
        int lower = Math.Clamp((int)Math.Floor(indicatorPosition), 0, labels.Length - 1), upper = Math.Min(lower + 1, labels.Length - 1);
        var first = TabBounds(lower); var last = TabBounds(upper);
        double amount = indicatorPosition - lower;
        context.FillRectangle(Palette.Brush(Palette.SelectionBorder), new Rect(first.X + (last.X - first.X) * amount, Height - UiMetrics.TabIndicatorHeight, first.Width + (last.Width - first.Width) * amount, UiMetrics.TabIndicatorHeight));
    }
}
