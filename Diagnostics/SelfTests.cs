using System.Text.Json;

namespace Usage;

internal static class SelfTests
{
    public static void Run(string path)
    {
        var checks = new List<string>();
        void Check(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException(label);
            checks.Add(label);
        }

        try
        {
            var now = DateTimeOffset.Parse("2026-10-06T12:00:00Z");
            ThemeChecks.Run(Check);
            SettingsChecks.Run(Check);
            UpdateChecks.Run(Check);
            OnboardingChecks.Run(Check);
            ClientChecks.Run(Check);
            ConnectionChecks.Run(Check);
            NameEditingChecks.Run(Check);
            LifecycleChecks.Run(Check);
            PaceChecks.Run(Check, now);
            LimitWarningChecks.Run(Check, now);
            var limits = ProviderChecks.Run(Check, now);
            ViewChecks.Run(Check, now, limits);
            Write(new
            {
                Passed = true,
                Checks = checks
            });
        }
        catch (Exception e)
        {
            Write(new
            {
                Passed = false,
                Error = e.Message,
                Stack = e.StackTrace,
                Checks = checks
            });
            Environment.ExitCode = 1;
        }

        void Write(object result) => File.WriteAllText(path, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }
}
