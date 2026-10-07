namespace Usage;

public static class Motion
{
    public const int FrameMilliseconds = 15;
    public const double FadeMilliseconds = 260, ReorderMilliseconds = 160;
    public const double NavigationFadeMilliseconds = 200;
    public const bool NavigationSlideEnabled = true;
    public static int NavigationOffset(double opacity, int dpi) => NavigationSlideEnabled ? (int)Math.Round((1 - opacity) * UiMetrics.SlideDistance * dpi / 96d) : 0;
    public static double NavigationEase(double progress) => Math.Clamp(progress, 0, 1);
    public static double EaseOut(double progress) => 1 - Math.Pow(1 - Math.Clamp(progress, 0, 1), 3);
    public static bool Enabled => SystemInformation.IsMenuAnimationEnabled;
}
