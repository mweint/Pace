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
        BackColor = Palette.Card;
        AccessibleRole = AccessibleRole.CheckButton;
    }

    protected override void OnClick(EventArgs e)
    {
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

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        float scale = DeviceDpi / 96f;
        g.ScaleTransform(scale, scale);
        using var track = new SolidBrush(Checked ? Palette.Below : Palette.ToggleOff);
        g.FillRectangle(track, 0, 7, 24, 12);
        using var knob = new SolidBrush(Checked ? Palette.Background : Palette.Text);
        g.FillRectangle(knob, Checked ? 14 : 2, 9, 8, 8);
        using var ink = new SolidBrush(Palette.Text);
        g.DrawString(Text, Font, ink, 31, 5);
        if (Focused && ShowFocusCues)
        {
            using var edge = new Pen(Palette.Muted);
            g.DrawRectangle(edge, 0, 5, Width / scale - 2, 17);
        }
    }
}
