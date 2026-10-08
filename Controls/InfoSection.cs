namespace Usage;

// Account-level information forms a section, using the common inset and divider.
internal sealed class InfoSection : Panel, IThemedSection
{
    bool arranging, showSeparator;
    public bool ShowSeparator
    {
        get => showSeparator;
        set { if (showSeparator != value) { showSeparator = value; Invalidate(); } }
    }

    public InfoSection()
    {
        DoubleBuffered = true;
        SectionStyle.Apply(this);
        Font = Palette.BodyFont();
        SizeChanged += (_, _) => Arrange();
        DpiChangedAfterParent += (_, _) => Arrange();
    }

    public void Add(string text, Color color)
    {
        Controls.Add(new Label { Text = text, ForeColor = color, Margin = Padding.Empty });
        Arrange();
    }

    void Arrange()
    {
        if (arranging)
            return;
        arranging = true;
        try
        {
            int inset = LogicalToDeviceUnits(UiMetrics.ContentInset);
            int width = Math.Max(1, ClientSize.Width - 2 * inset);
            int bottom = inset;
            foreach (Label label in Controls)
            {
                int height = Math.Max(label.Text.Split('\n').Length * LogicalToDeviceUnits(UiMetrics.DetailInfoLineHeight),
                    TextRenderer.MeasureText(label.Text, Font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak).Height);
                label.SetBounds(inset, bottom, width, height);
                bottom = label.Bottom;
            }
            Height = bottom + inset;
        }
        finally { arranging = false; }
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
