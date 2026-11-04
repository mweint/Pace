namespace Pace;

public enum ConnectionIssue { None, CredentialsMissing, AccountChanged, SignInRejected, SignInExpired }

public sealed record Reading(Account Account, UsageWindow? Weekly, string? Error, DateTimeOffset Updated, BankedResets? Resets = null, string? ResetError = null, List<UsageLimit>? Limits = null, ConnectionIssue ConnectionIssue = ConnectionIssue.None, bool RateLimited = false)
{
    public const string RefreshingMessage = "Refreshing…";
    public bool NeedsReconnect => ConnectionIssue != ConnectionIssue.None;
    // A failed or stale reading, unlike the placeholder shown while an account's first reading loads.
    public bool HasProblem => Error is not null and not RefreshingMessage;
    // A rate-limited reading waits for its cooldown; retrying sooner cannot help.
    public bool CanRetry => Error != null && !RateLimited;
    public string ConnectionReason => ConnectionIssue switch
    {
        ConnectionIssue.CredentialsMissing => "Sign-in missing",
        ConnectionIssue.AccountChanged => "Account changed",
        ConnectionIssue.SignInRejected => "Sign-in rejected",
        ConnectionIssue.SignInExpired => "Sign-in expired",
        _ => ""
    };
}
