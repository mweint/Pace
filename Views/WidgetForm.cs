namespace Usage;

public class WidgetForm : Form
{
    public WidgetForm()
    {
        DoubleBuffered = true;
        Icon = Palette.ApplicationIcon;
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
        using var edge = new Pen(Palette.WindowBorder, UiMetrics.WindowBorderWidth);
        e.Graphics.DrawRectangle(edge, 0, 0, Width - UiMetrics.WindowBorderWidth, Height - UiMetrics.WindowBorderWidth);
    }
}
