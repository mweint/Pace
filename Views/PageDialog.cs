namespace Usage;
// Accounts and details remain separate native windows. This class owns their
// shared placement, navigation animation, dismissal, and animation resources.
public abstract class PageDialog : WidgetForm
{
    readonly System.Windows.Forms.Timer fade = new()
    {
        Interval = Motion.FrameMilliseconds
    };
    long fadeStarted;
    Point fadeAnchor;
    double fadeFrom, fadeTo = 1;
    bool closing, allowClose;
    public bool CloseAllRequested
    {
        get; private set;
    }
    protected bool IsClosing => closing;
    protected Control? NavigationFocus
    {
        get; set;
    }
    protected Rectangle AnchorArea => Owner is UsagePanel panel ? panel.AnchorArea : Screen.FromControl(this).WorkingArea;

    protected PageDialog()
    {
        StartPosition = FormStartPosition.Manual;
        KeyPreview = true;
        fade.Tick += (_, _) => Animate();
    }

    protected override void OnShown(EventArgs e)
    {
        Place();
        BringToFront();
        Activate();
        ActiveControl = NavigationFocus;
        OnPageShown();
        if (Owner != null && Motion.Enabled)
        {
            Opacity = 0;
            MoveToFrame();
            StartFade(1);
        }

        base.OnShown(e);
    }

    protected virtual void OnPageShown()
    {
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!allowClose && Owner != null && e.CloseReason == CloseReason.UserClosing && Motion.Enabled)
        {
            e.Cancel = true;
            if (!closing)
            {
                closing = true;
                SuppressFocusOutlines(this);
                StartFade(0);
            }
        }

        base.OnFormClosing(e);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (Owner != null && !closing)
            DismissAll();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            e.Handled = true;
            Close();
        }

        base.OnKeyDown(e);
    }

    public void DismissAll()
    {
        CloseAllRequested = true;
        Close();
    }

    protected void Place()
    {
        fadeAnchor = PopupPlacement.BottomRight(AnchorArea, Size);
        MoveToFrame();
    }

    void StartFade(double target)
    {
        fadeFrom = Opacity;
        fadeTo = target;
        fadeStarted = Environment.TickCount64;
        fade.Start();
    }

    void Animate()
    {
        bool dismissing = closing && CloseAllRequested;
        double duration = dismissing ? Motion.FadeMilliseconds : Motion.NavigationFadeMilliseconds;
        double progress = Math.Clamp((Environment.TickCount64 - fadeStarted) / duration, 0, 1);
        double eased = dismissing ? Motion.EaseOut(progress) : Motion.NavigationEase(progress);
        Opacity = fadeFrom + (fadeTo - fadeFrom) * eased;
        if (!CloseAllRequested)
            MoveToFrame();
        if (progress < 1)
            return;
        fade.Stop();
        if (closing)
        {
            allowClose = true;
            Close();
        }
    }

    void MoveToFrame() => Location = new Point(fadeAnchor.X, fadeAnchor.Y + Motion.NavigationOffset(Opacity, DeviceDpi));
    static void SuppressFocusOutlines(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is IconButton button)
            {
                button.SuppressFocusOutline = true;
                button.Invalidate();
            }

            SuppressFocusOutlines(control);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            fade.Dispose();
        base.Dispose(disposing);
    }
}
