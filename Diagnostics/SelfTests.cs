using System.Text.Json;

namespace Pace;

internal static class SelfTests
{
    public static async Task Run(string path)
    {
        var checks = new List<string>();
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            checks.Add(label);
        }
        try
        {
            var now = DateTimeOffset.Parse("2026-10-06T12:00:00Z");
            SettingsChecks.Run(Check);
            ClientChecks.Run(Check);
            await LaunchChecks.Run(Check);
            PaceChecks.Run(Check, now);
            ProviderChecks.Run(Check, now);
            await UpdateChecks.Run(Check);
            await InterfaceChecks.Run(Check);
            Write(new { Passed = true, Checks = checks });
        }
        catch (Exception e)
        {
            Write(new { Passed = false, Error = e.Message, Stack = e.StackTrace, Checks = checks });
            Environment.ExitCode = 1;
        }
        void Write(object result) => File.WriteAllText(path, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }
}
