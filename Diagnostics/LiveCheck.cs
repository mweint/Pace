using System.Text.Json;

namespace Usage;

internal static class LiveCheck
{
    // Intentionally omit identities, paths, credentials and response bodies.
    public static void Run(string path)
    {
        var readings = Providers.Discover(Settings.Load()).Select(account => Providers.Fetch(account).GetAwaiter().GetResult());
        var results = readings.Select(reading => new { reading.Account.Service, HasWeekly = reading.Weekly != null, reading.Error, Used = reading.Weekly?.Used, Reset = reading.Weekly?.Reset, BankedResets = reading.Resets?.Count, Expiries = reading.Resets?.Grants?.Select(grant => new { grant.Count, grant.Expires }), reading.ResetError });
        File.WriteAllText(path, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
    }
}
