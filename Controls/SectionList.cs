namespace Usage;

// Every account/limit list follows the same surface and separator policy.
public class SectionList : FlowLayoutPanel
{
    bool arranging;
    public SectionList()
    {
        DoubleBuffered = true;
        BackColor = Palette.SectionBackground;
        Padding = Padding.Empty;
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        AutoScroll = true;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        if (arranging)
            return;
        arranging = true;
        try
        {
            bool first = true;
            SectionGroup previousGroup = SectionGroup.None;
            foreach (Control control in Controls)
            {
                if (control is not IThemedSection section)
                    continue;
                control.Width = Math.Max(1, ClientSize.Width - Padding.Horizontal -
                    (VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
                section.ShowSeparator = !first && (section.Group == SectionGroup.None || section.Group != previousGroup);
                previousGroup = section.Group;
                first = false;
            }
            base.OnLayout(levent);
        }
        finally { arranging = false; }
    }
}
