namespace Pace;

public enum ConnectionIssue { None, CredentialsMissing, AccountChanged, SignInRejected }

public sealed record Reading(Account Account, UsageWindow? Weekly, string? Error, DateTimeOffset Updated, BankedResets? Resets = null, string? ResetError = null, List<UsageLimit>? Limits = null, ConnectionIssue ConnectionIssue = ConnectionIssue.None)
{
    public bool NeedsReconnect => ConnectionIssue != ConnectionIssue.None;
    public string ConnectionReason => ConnectionIssue switch
    {
        ConnectionIssue.CredentialsMissing => "Sign-in missing",
        ConnectionIssue.AccountChanged => "Account changed",
        ConnectionIssue.SignInRejected => "Sign-in rejected",
        _ => ""
    };
}
