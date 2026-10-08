namespace Pace;
// Logical pixels at 96 DPI. Component-specific field coordinates stay with the
// component; shared surfaces, spacing, and dimensions are defined once here.
public static class UiMetrics
{
    public const int BaseDpi = 96;
    public const int BorderWidth = 1, EmphasisBorderWidth = 2;
    public const int FocusInset = 3;
    public const int PanelWidth = 388, HoverWidth = 250;
    public const int OuterInset = 12, CardGap = 8, ScreenInset = 8, WindowBorderWidth = 1;
    public const int ContentWidth = PanelWidth - 2 * OuterInset - 2 * WindowBorderWidth;
    public const int ContentInset = 14, InlineGap = 4;
    // Free-standing text sits slightly inside the edge shared with bars, buttons and switches.
    public const int TextInset = 3;
    public const int WeeklyBarHeight = 8, PaceMarkerOverhang = 2, ResetBadgeHeight = 24;
    public const int ResetLineGap = InlineGap;
    public const int CompactLimitBarHeight = 4, CompactLimitGroupGap = 12, CompactLimitLabelWidth = 46;
    public const int DetailLimitGap = InlineGap, DetailLimitBarHeight = CompactLimitBarHeight;
    public const int DetailInfoLineHeight = 22;
    public const int ToolbarHeight = 46, AccountsToolbarHeight = 58;
    public const int TabStripHeight = 36, TabIndicatorHeight = EmphasisBorderWidth;
    public const int TabTextInset = CardGap, TabGap = ContentInset;
    public const int SettingsContentMaxHeight = 420, ScreenHeightReserve = 60;
    public const int ScrollbarWidth = 4, ScrollbarGap = InlineGap, ScrollbarMinThumb = 28, ScrollWheelDistance = 48;
    public const int IconButtonSize = 30, IconSize = 14, TextButtonHeight = 34;
    // Icon stroke in Lucide's 24-unit viewBox (Lucide's default is 2).
    public const double IconStroke = 2.25;
    public const int TextButtonPadding = 18, ToolbarInset = 8, ToolbarTextTop = 13;
    public const int AccountActionWidth = 96;
    public static double TextButtonWidth(string text) => Palette.TextWidth(text) + 2 * TextButtonPadding;
    public const int ToolbarLabelLeft = ToolbarInset + IconButtonSize + 6;
    public const int NameEditorHeight = IconButtonSize + 2;
    public const int DragThreshold = 5;
    public const int ToggleHeight = 27, ToggleTrackWidth = 24, ToggleTrackHeight = 12;
    public const int ToggleKnobSize = 8, ToggleKnobInset = 2;
    public const int ToggleTextGap = 9;
    public const int ResetBadgeWidth = 42, ResetBadgeIconSize = 12;
    public const int ResetBadgeIconInset = 2, ResetBadgeTextGap = 5;
    public const int PaceMarkerWidth = 3;
    public const int TooltipInset = OuterInset;
    public const int ServiceIconSize = 16;
    public const int WarningDotSize = 6;
    public const int AutoSaveMilliseconds = 300, HoverDelayMilliseconds = 450;
    public const int HoverRowHeight = 34, HoverInset = 14, HoverPointerTolerance = 22;
    public const int SlideDistance = 18;
    public const int TooltipMaxWidth = 300;
    public const int DetailDelayMilliseconds = 1000, DetailDurationMilliseconds = 10000;
}

public static class PopupPlacement
{
    public static PixelPoint BottomRight(PixelRect area, Size size, double scale = 1) => new(
        Math.Max(area.X, area.Right - (int)Math.Ceiling((size.Width + UiMetrics.ScreenInset) * scale)),
        Math.Max(area.Y, area.Bottom - (int)Math.Ceiling((size.Height + UiMetrics.ScreenInset) * scale)));
}
