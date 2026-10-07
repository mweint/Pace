namespace Usage;

public sealed record ResetGrant(int Count, DateTimeOffset? Expires);
public sealed record BankedResets(int Count, List<ResetGrant>? Grants)
{
    public const int ExpiryWarningDays = 7;
    public int Available(DateTimeOffset now) => Math.Max(0, Count - (Grants?.Where(g => g.Expires <= now).Sum(g => g.Count) ?? 0));
    public bool ExpiringSoon(DateTimeOffset now) => Grants?.Any(g => g.Expires > now && g.Expires <= now.AddDays(ExpiryWarningDays)) == true;
    public string Details(DateTimeOffset now)
    {
        var lines = new List<string>();
        if (Grants == null)
            lines.Add("Expiry dates unavailable");
        else
        {
            var active = Grants.Where(g => g.Expires == null || g.Expires > now).GroupBy(g => g.Expires).Select(group => new ResetGrant(group.Sum(g => g.Count), group.Key)).OrderBy(g => g.Expires ?? DateTimeOffset.MaxValue).ToList();
            foreach (var grant in active)
                lines.Add((grant.Expires is { } expiry ? expiry.ToLocalTime().ToString("M/d/yy · h:mm tt", System.Globalization.CultureInfo.InvariantCulture) : "Expiry not provided") + (grant.Count > 1 ? $" ×{grant.Count}" : ""));
            if (active.Sum(g => g.Count) < Available(now))
                lines.Add("Additional expiry dates unavailable");
        }

        return string.Join("\n", lines);
    }
}
