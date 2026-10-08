namespace Usage;
// Native tooltip timing and placement, with the same typography and colors as the app.
internal sealed class ThemedToolTip : ToolTip
{
    public ThemedToolTip()
    {
        OwnerDraw = true;
        InitialDelay = ReshowDelay = UiMetrics.DetailDelayMilliseconds;
        AutoPopDelay = UiMetrics.DetailDurationMilliseconds;
        UseAnimation = UseFading = false;
        Popup += (_, e) =>
        {
            if (e.AssociatedControl is not { } owner)
                return;
            using var font = Palette.BodyFont();
            using var graphics = Graphics.FromHwnd(owner.Handle);
            int inset = (int)Math.Ceiling(UiMetrics.TooltipInset * owner.DeviceDpi / (double)UiMetrics.BaseDpi);
            int width = (int)Math.Ceiling(UiMetrics.TooltipMaxWidth * owner.DeviceDpi / (double)UiMetrics.BaseDpi);
            var size = graphics.MeasureString(GetToolTip(owner), font, width - 2 * inset);
            e.ToolTipSize = new Size((int)Math.Ceiling(size.Width) + 2 * inset, (int)Math.Ceiling(size.Height) + 2 * inset);
        };
        Draw += (_, e) =>
        {
            using var font = Palette.BodyFont();
            using var background = new SolidBrush(Palette.Background);
            using var text = new SolidBrush(Palette.Text);
            using var border = new Pen(Palette.WindowBorder, UiMetrics.BorderWidth);
            e.Graphics.FillRectangle(background, e.Bounds);
            e.Graphics.DrawRectangle(border, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - UiMetrics.BorderWidth, e.Bounds.Height - UiMetrics.BorderWidth);
            float inset = UiMetrics.TooltipInset * e.Graphics.DpiX / (float)UiMetrics.BaseDpi;
            e.Graphics.DrawString(e.ToolTipText, font, text, new RectangleF(e.Bounds.X + inset, e.Bounds.Y + inset, e.Bounds.Width - 2 * inset, e.Bounds.Height - 2 * inset));
        };
    }
}
