using System.Globalization;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Pace;

public static class Palette
{
    // Embedded Inter, renamed Pace Sans; never substituted with installed fonts.
    public static readonly FontFamily FontFamily = new("avares://Pace/Assets/Fonts#Pace Sans");
    public static readonly FontFamily HeadingFontFamily = new("avares://Pace/Assets/Fonts#Pace Sans SemiBold");
    // Body text, account names, and titles (tabs, toolbars, section headings).
    public const double BodySize = 12, AccountSize = 14, TitleSize = 14;
    public static readonly Color Background = Color.FromRgb(20, 23, 29);
    public static Color SectionBackground => Background;
    public static readonly Color InteractionSurface = Color.FromRgb(29, 33, 41);
    public static readonly Color Text = Color.FromRgb(238, 241, 246);
    public static readonly Color Muted = Color.FromRgb(154, 164, 180);
    public static readonly Color Above = Color.FromRgb(255, 161, 104), Below = Color.FromRgb(111, 210, 178), OnPace = Color.FromRgb(154, 193, 255);
    public static readonly Color TrayOnPace = Color.FromRgb(79, 166, 255);
    public static readonly Color Footer = Color.FromRgb(26, 30, 37), Action = Color.FromRgb(40, 46, 57);
    public static readonly Color InputBorder = Color.FromRgb(51, 58, 70), WindowBorder = Color.FromRgb(61, 69, 83);
    public static readonly Color InputHoverBorder = Color.FromRgb(88, 98, 114);
    public static readonly Color Track = Color.FromRgb(57, 64, 77), BarDivider = Color.FromArgb(110, 20, 23, 29);
    public static readonly Color ToggleOff = Color.FromRgb(66, 74, 89), Claude = Color.FromRgb(224, 162, 124);
    public static readonly Color Disabled = Color.FromRgb(75, 83, 96), Hover = Color.FromRgb(47, 54, 66);
    public static Color Warning => Above;
    public static Color ScrollThumb => Muted;
    public static Color FocusBorder => Muted;
    public static Color SelectionBorder => OnPace;
    public static Color ButtonHover => Hover;
    public static Color ButtonPressed => Hover;
    public static Color ToggleOn => Below;
    public static Color ToggleTrack(bool enabled, bool selected) => !enabled ? Disabled : selected ? ToggleOn : ToggleOff;
    public static Color ToggleKnob(bool selected) => selected ? Background : Text;
    public static Color ControlText(bool enabled) => enabled ? Text : Muted;
    public static Color IconInk(bool enabled, bool selected) => selected ? OnPace : enabled ? Muted : Disabled;
    public static Color InputOutline(bool editing, bool active) => editing ? FocusBorder : active ? InputHoverBorder : InputBorder;
    public static Color Status(PaceGap? p) => Status(PaceMath.Classify(p));
    public static Color Status(UsageWindow window, DateTimeOffset now) => Status(PaceMath.Classify(window, now));
    public static Color Status(PaceState state) => state switch
    {
        PaceState.Exhausted or PaceState.Above => Above,
        PaceState.Below => Below,
        PaceState.OnPace => OnPace,
        _ => Muted
    };

    static readonly Dictionary<Color, IBrush> brushes = [];
    public static IBrush Brush(Color color)
    {
        if (!brushes.TryGetValue(color, out var brush))
            brushes[color] = brush = new ImmutableSolidColorBrush(color);
        return brush;
    }

    static readonly Typeface Body = new(FontFamily), Heading = new(HeadingFontFamily);
    public static FormattedText Format(string text, Color color, double size = BodySize, bool heading = false) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, heading ? Heading : Body, size, Brush(color));
    // One line, ellipsized to the available width.
    public static FormattedText Line(string text, Color color, double width, double size = BodySize, bool heading = false)
    {
        var line = Format(text, color, size, heading);
        line.MaxTextWidth = Math.Max(1, width);
        line.MaxLineCount = 1;
        line.Trimming = TextTrimming.CharacterEllipsis;
        return line;
    }
    public static double TextHeight(bool heading = false, double size = BodySize) => Math.Ceiling(Format("Ag", Text, size, heading).Height);
    public static double TextWidth(string text, double size = BodySize, bool heading = false) =>
        Math.Ceiling(Format(text, Text, size, heading).WidthIncludingTrailingWhitespace);

    public static TextBlock Label(string text, bool heading = false, Color? color = null, double? size = null) => new()
    {
        Text = text, FontFamily = heading ? HeadingFontFamily : FontFamily,
        FontSize = size ?? BodySize, Foreground = Brush(color ?? Muted), Padding = new Thickness(UiMetrics.TextInset, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis
    };
    public static FilledButton Button(string text) => new(text);
    public static FilledButton ServiceButton(string service) => new("Add " + service) { Service = service };
    public static WindowIcon ApplicationIcon => new(AssetLoader.Open(new Uri("avares://Pace/Assets/Icons/pace.ico")));
    public static void ConfigureInput(TextBox input)
    {
        input.FontFamily = HeadingFontFamily;
        input.FontSize = AccountSize;
        input.Foreground = Brush(Text);
        input.Background = Brush(SectionBackground);
        input.BorderThickness = new Thickness(0);
        input.CornerRadius = new CornerRadius(0);
        input.Padding = new Thickness(0);
        input.MinHeight = input.MinWidth = 0;
        input.SelectionBrush = Brush(InteractionSurface);
        input.CaretBrush = Brush(Text);
        input.VerticalContentAlignment = VerticalAlignment.Center;
        input.Resources["TextControlBackground"] = Brush(SectionBackground);
        input.Resources["TextControlBackgroundPointerOver"] = Brush(SectionBackground);
        input.Resources["TextControlBackgroundFocused"] = Brush(SectionBackground);
        input.Resources["TextControlBorderBrushFocused"] = Brush(SectionBackground);
    }
}
