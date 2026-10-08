namespace Usage;

public sealed class AccountToggle : Control
{
    bool isChecked;
    public event EventHandler? CheckedChanged;
    public bool Checked
    {
        get => isChecked;
        set
        {
            if (isChecked == value)
                return;
            isChecked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public AccountToggle()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.Selectable, true);
        Font = Palette.BodyFont();
        Cursor = Cursors.Hand;
        TabStop = true;
        BackColor = Palette.SectionBackground;
        AccessibleRole = AccessibleRole.CheckButton;
    }

    protected override void OnClick(EventArgs e)
    {
        if (!Enabled)
            return;
        Focus();
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            Checked = !Checked;
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        using var graphics = CreateGraphics();
        float scale = DeviceDpi / (float)UiMetrics.BaseDpi;
        int textWidth = (int)Math.Ceiling(graphics.MeasureString(Text, Font).Width);
        return new Size(LogicalToDeviceUnits(UiMetrics.ToggleTrackWidth + UiMetrics.ToggleTextGap + UiMetrics.InlineGap) + textWidth,
            (int)Math.Ceiling(UiMetrics.ToggleHeight * scale));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        float scale = DeviceDpi / (float)UiMetrics.BaseDpi;
        g.ScaleTransform(scale, scale);
        float trackTop = (UiMetrics.ToggleHeight - UiMetrics.ToggleTrackHeight) / 2;
        using var track = new SolidBrush(Palette.ToggleTrack(Enabled, Checked));
        g.FillRectangle(track, 0, trackTop, UiMetrics.ToggleTrackWidth, UiMetrics.ToggleTrackHeight);
        using var knob = new SolidBrush(Palette.ToggleKnob(Checked));
        g.FillRectangle(knob, Checked ? UiMetrics.ToggleTrackWidth - UiMetrics.ToggleKnobInset - UiMetrics.ToggleKnobSize : UiMetrics.ToggleKnobInset,
            trackTop + UiMetrics.ToggleKnobInset, UiMetrics.ToggleKnobSize, UiMetrics.ToggleKnobSize);
        using var ink = new SolidBrush(Palette.ControlText(Enabled));
        g.DrawString(Text, Font, ink, UiMetrics.ToggleTrackWidth + UiMetrics.ToggleTextGap,
            (UiMetrics.ToggleHeight - Font.GetHeight(UiMetrics.BaseDpi)) / 2);
        if (Focused && ShowFocusCues)
        {
            using var edge = new Pen(Palette.FocusBorder, UiMetrics.BorderWidth);
            g.DrawRectangle(edge, 0, UiMetrics.FocusInset, Width / scale - UiMetrics.EmphasisBorderWidth,
                UiMetrics.ToggleHeight - 2 * UiMetrics.FocusInset);
        }
    }
}
