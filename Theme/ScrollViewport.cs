namespace Usage;

// Clips full-height content and draws a compact themed scrollbar only on overflow.
internal sealed class ScrollViewport : Panel
{
    readonly Control content;
    int offset, extent, dragStart, dragOffset;
    bool arranging, dragging;
    public bool Overflow => extent > ClientSize.Height;
    public ScrollViewport(Control content)
    {
        this.content = content;
        DoubleBuffered = true;
        BackColor = Palette.SectionBackground;
        content.Dock = DockStyle.None;
        if (content is ScrollableControl scrollable) scrollable.AutoScroll = false;
        Controls.Add(content);
        Watch(content);
        content.Layout += (_, _) => Arrange();
    }
    void Watch(Control control)
    {
        control.MouseWheel += Wheel;
        control.Enter += (_, _) =>
        {
            if (!Overflow || !control.IsHandleCreated) return;
            var position = PointToClient(control.PointToScreen(Point.Empty));
            if (position.Y < 0) SetOffset(offset + position.Y);
            else if (position.Y + control.Height > Height) SetOffset(offset + position.Y + control.Height - Height);
        };
        control.ControlAdded += (_, e) => { if (e.Control != null) Watch(e.Control); };
        foreach (Control child in control.Controls) Watch(child);
    }
    void Wheel(object? sender, MouseEventArgs e)
    {
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
        SetOffset(offset - e.Delta * LogicalToDeviceUnits(UiMetrics.ScrollWheelDistance) / 120);
    }
    protected override void OnMouseWheel(MouseEventArgs e) { Wheel(this, e); base.OnMouseWheel(e); }
    protected override void OnLayout(LayoutEventArgs levent) { base.OnLayout(levent); Arrange(); }
    void Arrange()
    {
        if (arranging || content == null || content.IsDisposed) return;
        arranging = true;
        try
        {
            extent = content.Controls.Cast<Control>().Sum(child => child.Height + child.Margin.Vertical);
            int width = Math.Max(1, ClientSize.Width - (Overflow ? LogicalToDeviceUnits(UiMetrics.ScrollbarWidth + UiMetrics.ScrollbarGap) : 0));
            content.Size = new Size(width, Math.Max(1, extent));
            content.PerformLayout();
            extent = content.Controls.Cast<Control>().Sum(child => child.Height + child.Margin.Vertical);
            content.Height = Math.Max(1, extent);
            SetOffset(offset);
        }
        finally { arranging = false; }
    }
    void SetOffset(int value)
    {
        offset = Math.Clamp(value, 0, Math.Max(0, extent - ClientSize.Height));
        content.Location = new Point(0, -offset);
        Invalidate();
    }
    Rectangle Thumb
    {
        get
        {
            int inset = LogicalToDeviceUnits(UiMetrics.InlineGap);
            int track = Math.Max(1, Height - 2 * inset);
            int height = Math.Clamp(track * Height / Math.Max(1, extent), Math.Min(track, LogicalToDeviceUnits(UiMetrics.ScrollbarMinThumb)), track);
            int top = inset + (track - height) * offset / Math.Max(1, extent - Height);
            int width = LogicalToDeviceUnits(UiMetrics.ScrollbarWidth);
            return new Rectangle(Width - width, top, width, height);
        }
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Overflow || e.Button != MouseButtons.Left || e.X < Width - LogicalToDeviceUnits(UiMetrics.ScrollbarWidth + UiMetrics.ScrollbarGap)) return;
        if (!Thumb.Contains(e.Location)) SetOffset(offset + (e.Y < Thumb.Top ? -Height : Height));
        dragging = true; dragStart = e.Y; dragOffset = offset; Capture = true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragging) SetOffset(dragOffset + (e.Y - dragStart) * Math.Max(0, extent - Height) / Math.Max(1, Height - 2 * LogicalToDeviceUnits(UiMetrics.InlineGap) - Thumb.Height));
    }
    protected override void OnMouseUp(MouseEventArgs e) { dragging = false; Capture = false; base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!Overflow) return;
        using var ink = new SolidBrush(Palette.ScrollThumb);
        e.Graphics.FillRectangle(ink, Thumb);
    }
}
