namespace Pace;

public abstract class PageDialog : WidgetWindow
{
    readonly MotionTween fade;
    PixelPoint fadeAnchor;
    bool allowClose;
    public bool IsClosing { get; private set; }
    public bool CloseAllRequested { get; private set; }
    public bool Navigating { get; set; }
    protected Control? NavigationFocus { get; set; }
    public event Action<bool>? LeaveRequested;
    protected PageDialog()
    {
        fade = new(this);
        Closing += (_, e) =>
        {
            if (allowClose || PreviewMode) return;
            e.Cancel = true;
            if (!IsClosing) LeaveRequested?.Invoke(false);
        };
        Deactivated += (_, _) =>
        {
            if (!PreviewMode && !KeepOpen && !Navigating && !IsClosing) DismissAll();
        };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && !e.Handled) { LeaveRequested?.Invoke(false); e.Handled = true; }
        };
        Closed += (_, _) => fade.Dispose();
    }
    public void OpenPage(PopupAnchor anchor)
    {
        Anchor = anchor; Place();
        fadeAnchor = Position;
        FrameOpacity = Motion.Enabled ? 0 : 1;
        MoveToFrame();
        Show(); Activate(); NavigationFocus?.Focus(NavigationMethod.Unspecified);
        fade.Start(Motion.NavigationFadeMilliseconds, t =>
        {
            FrameOpacity = t;
            MoveToFrame();
        }, () => Navigating = false, Motion.EaseOut);
    }
    public void DismissAll() { if (!IsClosing) LeaveRequested?.Invoke(true); }
    public virtual bool SaveBeforeLeave() => true;
    public void ExitPage(bool all, Action completed)
    {
        IsClosing = true; CloseAllRequested = all;
        foreach (var button in ((Control)Content!).GetVisualDescendants().OfType<IconButton>()) button.SuppressFocusOutline = true;
        double from = FrameOpacity;
        fade.Start(all ? Motion.FadeMilliseconds : Motion.NavigationFadeMilliseconds, t =>
        {
            FrameOpacity = from * (1 - t);
            if (!all) MoveToFrame();
        }, () => { allowClose = true; Close(); completed(); }, Motion.EaseOut);
    }
    void MoveToFrame() => Position = Anchor.Slide(fadeAnchor, Motion.SlideOffset(FrameOpacity, RenderScaling));
}
