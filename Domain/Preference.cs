namespace Pace;

public sealed class Preference
{
    public string Key { get; set; } = "";
    public string Alias { get; set; } = "";
    // Identity and path only: credentials remain in the official client's files.
    public Account? RememberedAccount { get; set; }
    public bool Show { get; set; } = true;
    public bool Tray { get; set; } = true;
    public bool ShowFiveHour { get; set; } = true;
    public bool ShowFable { get; set; } = true;

    // Claude's five-hour and Fable limits can appear as compact bars under the weekly bar.
    public static bool ShowsBar(Preference? preference, Account account, UsageLimit limit) =>
        account.Service == Services.Claude &&
        (limit.IsSession ? preference?.ShowFiveHour != false : limit.IsFable && preference?.ShowFable != false);
}
