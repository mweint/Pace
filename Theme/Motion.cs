namespace Pace;

public static class Motion
{
    public const int FrameMilliseconds = 15;
    public const double FadeMilliseconds = 260, ReorderMilliseconds = 160;
    public const double NavigationFadeMilliseconds = 200;
    public const double IconFadeMilliseconds = 100;
    public const double TabMilliseconds = 140;
    // Vertical travel, in physical pixels, for a frame at the given opacity.
    public static int SlideOffset(double opacity, double scale) => (int)Math.Round((1 - opacity) * UiMetrics.SlideDistance * scale);
    public static double Linear(double progress) => Math.Clamp(progress, 0, 1);
    public static double EaseOut(double progress) => 1 - Math.Pow(1 - Math.Clamp(progress, 0, 1), 3);
    static bool? overrideEnabled;
    public static bool Enabled
    {
        get => overrideEnabled ?? DesktopIntegration.AnimationsEnabled;
        set => overrideEnabled = value;
    }
}
