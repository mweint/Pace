using System.Text.Json;

namespace Usage;

public sealed record Reading(Account Account, Window? Weekly, string? Error, DateTimeOffset Updated, BankedResets? Resets = null, string? ResetError = null, List<UsageLimit>? Limits = null);
