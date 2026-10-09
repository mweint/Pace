namespace Pace;

// Synthetic fixtures only. Preview and offline tests never discover real accounts.
internal static class SampleData
{
    public static List<Reading> ScreenshotReadings()
    {
        var now = DateTimeOffset.UtcNow;
        var demo = Readings(now).Take(3).ToList();
        demo[0] = demo[0] with { Weekly = new(36, now.AddDays(3.64), TimeSpan.FromDays(7)) };
        demo[1] = demo[1] with { Weekly = new(41, now.AddDays(4.1), TimeSpan.FromDays(7)), Resets = null };
        demo[2] = demo[2] with
        {
            Account = demo[2].Account with { Label = "Personal" },
            Weekly = new(30, now.AddDays(4), TimeSpan.FromDays(7)),
            Limits = [new("session", "Five-hour", new(68, now.AddHours(3), TimeSpan.FromHours(5))), new("model:Fable", "Fable · Weekly", new(28, now.AddDays(4), TimeSpan.FromDays(7)))]
        };
        return demo;
    }

    public static List<Reading> Readings(DateTimeOffset? at = null)
    {
        var now = at ?? DateTimeOffset.UtcNow;
        var demo = new List<Reading>();
        foreach (var (service, label, used, days) in new[]
        {
            ("Codex", "Personal", 57d, 3.64), ("Codex", "Work", 24d, 4.1),
            ("Claude", "Claude personal", 43d, 4d), ("Claude", "Claude work", 31d, 4.8)
        })
            demo.Add(new(new(label, service, label, ""), new(used, now.AddDays(days), TimeSpan.FromDays(7)), null, now));
        demo[0] = demo[0] with { Resets = new(2, [new(1, now.AddDays(16)), new(1, now.AddDays(23))]) };
        demo[1] = demo[1] with { Resets = new(1, [new(1, now.AddDays(2))]) };
        demo[2] = demo[2] with { Limits = [new("session", "Five-hour", new(100, now.AddMinutes(4), TimeSpan.FromHours(5))), new("model:Fable", "Fable · Weekly", new(34, now.AddDays(4), TimeSpan.FromDays(7)))] };
        demo[3] = demo[3] with { Limits = [new("session", "Five-hour", new(18, now.AddHours(3), TimeSpan.FromHours(5))), new("model:Fable", "Fable · Weekly", new(67, now.AddDays(4.8), TimeSpan.FromDays(7)))] };
        return demo;
    }
    public static Reading Detail(Reading reading) => reading with
    {
        Resets = new(1, [new(1, reading.Updated.AddDays(3))]),
        Limits = [new("weekly", "Weekly", reading.Weekly!), new("session", "Five-hour", new(100, reading.Updated.AddMinutes(4), TimeSpan.FromHours(5))), new("fable", "Fable · Weekly", new(12, reading.Updated.AddDays(4), TimeSpan.FromDays(7)))]
    };
}
