namespace Usage;

internal static class AppTiming
{
    public const int PaceRefreshMilliseconds = 60_000;
    public const int UpdateCheckMilliseconds = 6 * 60 * 60 * 1000;
    public const int UsageRefreshTicks = 5;
    public const int HoverPollMilliseconds = 100, HoverSuppressMilliseconds = 1000;
}
