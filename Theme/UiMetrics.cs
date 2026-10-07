namespace Usage;
// Logical pixels at 96 DPI. Component-specific field coordinates stay with the
// component; shared surfaces, spacing, and dimensions are defined once here.
public static class UiMetrics
{
    public const int PanelWidth = 388, HoverWidth = 250;
    public const int OuterInset = 12, CardGap = 8, ScreenInset = 8, WindowBorderWidth = 1;
    public const int ContentWidth = PanelWidth - 2 * OuterInset - 2 * WindowBorderWidth;
    public const int AccountHeight = 94, EditorHeight = 120;
    public const int DetailIdentityHeight = 54, DetailInfoLineHeight = 22;
    public const int ToolbarHeight = 46, AccountsToolbarHeight = 58;
    public const int IconButtonSize = 30, IconSize = 14, TextButtonHeight = 34;
    public const int AutoSaveMilliseconds = 300, HoverDelayMilliseconds = 450;
    public const int HoverRowHeight = 34, HoverVerticalInset = 16;
    public const int SlideDistance = 18;
    public const int TooltipMaxWidth = 300;
    public const int DetailDelayMilliseconds = 1000, DetailDurationMilliseconds = 10000;
}

public static class PopupPlacement
{
    public static Point BottomRight(Rectangle area, Size size) => new(Math.Max(area.Left, area.Right - size.Width - UiMetrics.ScreenInset), Math.Max(area.Top, area.Bottom - size.Height - UiMetrics.ScreenInset));
}
