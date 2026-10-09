namespace Pace;

// Shared native window shell; content windows stay separate.
public class WidgetWindow : Window
{
    public bool PreviewMode { get; set; }
    internal bool KeepOpen { get; set; }
    public WidgetWindow()
    {
        Width = UiMetrics.PanelWidth;
        CanResize = false;
        WindowDecorations = WindowDecorations.None;
        ShowInTaskbar = false;
        Topmost = true;
        // The window surface is transparent and the content paints the theme background,
        // so FrameOpacity blends the whole frame with the desktop during fades.
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Background = Brushes.Transparent;
        // Opacity layers inherit this, so a fading frame is composited once instead of
        // each overlapping painted surface accumulating its own alpha.
        RenderOptions.SetRequiresFullOpacityHandling(this, true);
        FontFamily = Palette.FontFamily; FontSize = Palette.BodySize;
        RenderOptions.SetBitmapInterpolationMode(this, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
        TextOptions.SetTextHintingMode(this, TextHintingMode.Strong);
        // Subpixel (ClearType-style) text: grayscale antialiasing renders Pace Sans visibly thinner.
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);
        Icon = Palette.ApplicationIcon;
        WindowStartupLocation = WindowStartupLocation.Manual;
        AddHandler(PointerPressedEvent, (_, _) =>
        {
            if (Content is Control root) FocusCue.HideForPointer(root);
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }
    protected void SetBody(Control content)
    {
        Content = new Border { BorderThickness = new Thickness(UiMetrics.WindowBorderWidth),
            BorderBrush = Palette.Brush(Palette.WindowBorder), Background = Palette.Brush(Palette.Background), Child = content };
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty && change.NewValue is Visual frame)
            RenderOptions.SetRequiresFullOpacityHandling(frame, true);
    }
    // Whole-frame opacity used by every fade. Window.Opacity stays 1.
    public double FrameOpacity
    {
        get => (Content as Visual)?.Opacity ?? 1;
        set { if (Content is Visual frame) frame.Opacity = value; }
    }
    public PopupAnchor Anchor { get; set; }
    public void Place()
    {
        var screen = Screens.ScreenFromPoint(Anchor.IsEmpty ? Position : Anchor.Area.Center) ?? Screens.Primary;
        if (Anchor.IsEmpty && screen != null) Anchor = PopupAnchor.For(screen);
        if (!Anchor.IsEmpty) Position = Anchor.Place(new Size(Width, Height), screen?.Scaling ?? RenderScaling);
    }
    protected double AvailableHeight => (!Anchor.IsEmpty ? Anchor.Area.Height : Screens.Primary?.WorkingArea.Height ?? 900) / RenderScaling - UiMetrics.ScreenHeightReserve;
}
