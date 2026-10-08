namespace Pace;

public sealed record UsageLimit(string Key, string Name, UsageWindow Window)
{
    public const string WeeklyKey = "weekly", SessionKey = "session";
    public bool IsSession => Key == SessionKey;
    public bool IsFable => Key.Equals("model:Fable", StringComparison.OrdinalIgnoreCase) || Key == "fable";
}
