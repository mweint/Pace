namespace Pace;

public static class Motion
{
    public const double FadeMilliseconds = 260, ReorderMilliseconds = 160;
    public const double NavigationFadeMilliseconds = 200;
    public const double IconFadeMilliseconds = 100;
    public const double TabMilliseconds = 140;
    public const double SpinMilliseconds = 1400;
    // A retry that needs no request still spins long enough to be seen.
    public const double MinimumSpinMilliseconds = 600;
    // Vertical travel, in physical pixels, for a frame at the given opacity.
    public static int SlideOffset(double opacity, double scale) => (int)Math.Round((1 - opacity) * UiMetrics.SlideDistance * scale);
    public static double Linear(double progress) => Math.Clamp(progress, 0, 1);
    public static double EaseOut(double progress) => 1 - Math.Pow(1 - Math.Clamp(progress, 0, 1), 3);
    // Page handoffs: the window underneath stays opaque while the one on top is translucent,
    // so the desktop never shows through the overlap. It enters in the first half (Lead)
    // or leaves in the second half (Trail).
    public static double Lead(double progress) => EaseOut(2 * progress);
    public static double Trail(double progress) => EaseOut(2 * progress - 1);
    static bool? overrideEnabled;
    public static bool Enabled
    {
        get => overrideEnabled ?? DesktopIntegration.AnimationsEnabled;
        set => overrideEnabled = value;
    }
}
