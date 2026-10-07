using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Usage;

public static class ServiceMark
{
    public static void Draw(Graphics g, string service, float x, float y)
    {
        using var pen = new Pen(service == "Claude" ? Palette.Claude : Palette.Text, 1.5f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        if (service == "Claude")
        {
            for (int i = 0; i < 6; i++)
            {
                double angle = i * Math.PI / 6;
                float dx = (float)Math.Cos(angle) * 7, dy = (float)Math.Sin(angle) * 7;
                g.DrawLine(pen, x + 7 - dx, y + 7 - dy, x + 7 + dx, y + 7 + dy);
            }
        }
        else
        {
            g.DrawLines(pen, new PointF[] { new(x + 1, y + 3), new(x + 5, y + 7), new(x + 1, y + 11) });
            g.DrawLine(pen, x + 8, y + 11, x + 14, y + 11);
        }
    }
}
