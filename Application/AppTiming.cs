namespace Pace;

internal static class AppTiming
{
    public const int PaceRefreshMilliseconds = 60_000;
    public const int UpdateCheckMilliseconds = 6 * 60 * 60 * 1000;
    public const int UsageRefreshMilliseconds = 120_000;
    // A successful reading younger than this is kept instead of asking the service again.
    // Keep it below the automatic refresh interval so timed refreshes are never skipped.
    public const int UsageFreshMilliseconds = 45_000;
    public const int HoverPollMilliseconds = 100, HoverSuppressMilliseconds = 1000;
    public const int TrayReadyPollMilliseconds = 50, TrayReadyChecks = 20;
    // Browser sign-in, with time to create an account or pass a two-factor check.
    public const int SignInTimeoutMilliseconds = 10 * 60 * 1000;
    public const int SignInStopGraceMilliseconds = 3000, SignInOutputGraceMilliseconds = 2000;
    // An expired saved sign-in is renewed through the official CLI, retried at most this often.
    public const int SignInRenewalTimeoutMilliseconds = 30_000, SignInRenewalRetryMilliseconds = 10 * 60 * 1000;
    // Claude Code allows the macOS Keychain the same time to answer.
    public const int KeychainReadTimeoutMilliseconds = 10_000;
}
