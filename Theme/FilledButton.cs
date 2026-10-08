namespace Usage;

// Filled text buttons share theme-owned interaction and disabled rendering.
// IconButton deliberately keeps its separate unfilled appearance.
internal sealed class FilledButton : Button
{
    public bool Selected { get; set; }
    bool hovered, pressed;

    protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovered = pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { pressed = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) { pressed = true; Invalidate(); } base.OnKeyDown(e); }
    protected override void OnKeyUp(KeyEventArgs e) { pressed = false; Invalidate(); base.OnKeyUp(e); }
    protected override void OnLostFocus(EventArgs e) { pressed = false; Invalidate(); base.OnLostFocus(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Selected ? Palette.InteractionSurface : Enabled && pressed ? Palette.ButtonPressed : Enabled && hovered ? Palette.ButtonHover : Palette.Action);
        var bounds = ClientRectangle;
        if (Image is { } image)
        {
            int textWidth = TextRenderer.MeasureText(e.Graphics, Text, Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            int left = (Width - image.Width - textWidth) / 2;
            e.Graphics.DrawImage(image, left, (Height - image.Height) / 2, image.Width, image.Height);
            bounds = new Rectangle(left + image.Width, 0, textWidth, Height);
        }
        TextRenderer.DrawText(e.Graphics, Text, Font, bounds, Selected ? Palette.SelectionBorder : Palette.ControlText(Enabled),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        if (Enabled && Focused && ShowFocusCues)
        {
            using var border = new Pen(Palette.FocusBorder, UiMetrics.BorderWidth);
            e.Graphics.DrawRectangle(border, UiMetrics.FocusInset, UiMetrics.FocusInset,
                Width - 2 * UiMetrics.FocusInset - UiMetrics.BorderWidth, Height - 2 * UiMetrics.FocusInset - UiMetrics.BorderWidth);
        }
    }
}
