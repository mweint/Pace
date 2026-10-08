namespace Usage;

// Shared tab navigation: labels and an active indicator, without button surfaces.
internal sealed class TabStrip : Control
{
    readonly string[] labels;
    int selected;
    readonly System.Windows.Forms.Timer animation = new() { Interval = Motion.FrameMilliseconds };
    double indicatorPosition, fromPosition;
    long started;
    public event Action<int>? SelectionChanged;
    public int SelectedIndex
    {
        get => selected;
        set
        {
            if (selected == value || value < 0 || value >= labels.Length) return;
            fromPosition = indicatorPosition;
            selected = value;
            started = Environment.TickCount64;
            if (Motion.Enabled && IsHandleCreated) animation.Start();
            else indicatorPosition = selected;
            Invalidate();
            SelectionChanged?.Invoke(selected);
        }
    }
    public TabStrip(params string[] labels)
    {
        this.labels = labels;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        Font = Palette.BarFont();
        BackColor = Palette.SectionBackground;
        Height = UiMetrics.TabStripHeight;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.PageTabList;
        AccessibleName = "Settings tabs";
        animation.Tick += (_, _) =>
        {
            double progress = Math.Clamp((Environment.TickCount64 - started) / Motion.TabMilliseconds, 0, 1);
            indicatorPosition = fromPosition + (selected - fromPosition) * Motion.EaseOut(progress);
            Invalidate();
            if (progress >= 1) animation.Stop();
        };
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        Focus();
        for (int index = 0; index < labels.Length; index++)
            if (TabBounds(index).Contains(e.Location)) { SelectedIndex = index; break; }
    }
    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right or Keys.Home or Keys.End || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Left) SelectedIndex = Math.Max(0, selected - 1);
        else if (e.KeyCode == Keys.Right) SelectedIndex = Math.Min(labels.Length - 1, selected + 1);
        else if (e.KeyCode == Keys.Home) SelectedIndex = 0;
        else if (e.KeyCode == Keys.End) SelectedIndex = labels.Length - 1;
        else return;
        e.Handled = true;
    }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override AccessibleObject CreateAccessibilityInstance() => new TabsAccessibility(this);
    sealed class TabsAccessibility(TabStrip owner) : ControlAccessibleObject(owner)
    {
        public override int GetChildCount() => owner.labels.Length;
        public override AccessibleObject? GetChild(int index) => index >= 0 && index < owner.labels.Length ? new TabAccessibility(owner, index, this) : null;
        public override AccessibleObject? GetSelected() => GetChild(owner.selected);
    }
    sealed class TabAccessibility(TabStrip owner, int index, AccessibleObject parent) : AccessibleObject
    {
        public override string? Name { get => owner.labels[index]; set { } }
        public override AccessibleRole Role => AccessibleRole.PageTab;
        public override AccessibleObject Parent => parent;
        public override string DefaultAction => "Select";
        public override AccessibleStates State => AccessibleStates.Selectable | AccessibleStates.Focusable | (owner.selected == index ? AccessibleStates.Selected : AccessibleStates.None);
        public override void DoDefaultAction() { owner.Focus(); owner.SelectedIndex = index; }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        int indicator = LogicalToDeviceUnits(UiMetrics.TabIndicatorHeight);
        for (int index = 0; index < labels.Length; index++)
        {
            var bounds = TabBounds(index);
            TextRenderer.DrawText(e.Graphics, labels[index], Font, bounds, index == selected ? Palette.Text : Palette.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            if (index != selected) continue;
            if (Focused && ShowFocusCues)
            {
                using var edge = new Pen(Palette.FocusBorder, UiMetrics.BorderWidth);
                e.Graphics.DrawRectangle(edge, bounds.Left + UiMetrics.FocusInset, UiMetrics.FocusInset,
                    bounds.Width - 2 * UiMetrics.FocusInset, Height - 2 * UiMetrics.FocusInset - indicator);
            }
        }
        int lower = Math.Clamp((int)Math.Floor(indicatorPosition), 0, labels.Length - 1);
        int upper = Math.Min(lower + 1, labels.Length - 1);
        var first = TabBounds(lower); var last = TabBounds(upper);
        float amount = (float)(indicatorPosition - lower);
        using var ink = new SolidBrush(Palette.SelectionBorder);
        e.Graphics.FillRectangle(ink, first.Left + (last.Left - first.Left) * amount, Height - indicator,
            first.Width + (last.Width - first.Width) * amount, indicator);
    }
    Rectangle TabBounds(int index)
    {
        int left = LogicalToDeviceUnits(UiMetrics.ContentInset);
        for (int tab = 0; tab < index; tab++) left += TabWidth(tab) + LogicalToDeviceUnits(UiMetrics.TabGap);
        return new Rectangle(left, 0, TabWidth(index), Height - LogicalToDeviceUnits(UiMetrics.TabIndicatorHeight));
    }
    int TabWidth(int index) => TextRenderer.MeasureText(labels[index], Font, Size.Empty, TextFormatFlags.NoPadding).Width + LogicalToDeviceUnits(2 * UiMetrics.TabTextInset);
    protected override void Dispose(bool disposing)
    {
        if (disposing) animation.Dispose();
        base.Dispose(disposing);
    }
}
