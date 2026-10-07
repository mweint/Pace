namespace Usage;

public class WidgetForm : Form
{
    public WidgetForm()
    {
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Palette.Background;
        ForeColor = Palette.Text;
        Font = Palette.BodyFont();
        AutoScaleMode = AutoScaleMode.Dpi;
        ShowInTaskbar = false;
        TopMost = true;
        Padding = new Padding(UiMetrics.WindowBorderWidth);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var edge = new Pen(Palette.WindowBorder);
        e.Graphics.DrawRectangle(edge, 0, 0, Width - 1, Height - 1);
    }
}
