namespace Usage;

internal sealed class SettingsSection : FlowLayoutPanel, IThemedSection
{
    public bool ShowSeparator { get; set; }
    public SettingsSection(int width)
    {
        SectionStyle.Apply(this);
        DoubleBuffered = true;
        Width = width;
        Padding = new Padding(UiMetrics.ContentInset);
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(width, 0);
        MaximumSize = new Size(width, 0);
    }
    public Label Label(string text, bool heading = false)
    {
        var label = new Label { Text = text, Font = heading ? Palette.BarFont() : Palette.BodyFont(), ForeColor = heading ? Palette.Text : Palette.Muted,
            AutoSize = true, MaximumSize = new Size(Width - Padding.Horizontal, 0), Margin = new Padding(0, 0, 0, UiMetrics.CardGap) };
        Controls.Add(label);
        return label;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var state = e.Graphics.Save();
        float scale = DeviceDpi / (float)UiMetrics.BaseDpi;
        e.Graphics.ScaleTransform(scale, scale);
        SectionStyle.DrawSeparator(e.Graphics, Width / scale, ShowSeparator);
        e.Graphics.Restore(state);
    }
}
