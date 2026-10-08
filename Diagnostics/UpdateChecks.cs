using System.Net;
using System.Text.Json;

namespace Pace;

internal static class UpdateChecks
{
    internal sealed class ReleaseHandler : HttpMessageHandler
    {
        public string Version { get; set; } = "v99.1.0";
        public bool Fail { get; set; }
        public string Host { get; set; } = "github.com";
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Fail) throw new HttpRequestException();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new
            {
                tag_name = Version, draft = false, prerelease = false,
                assets = new[] { new { name = "Pace-win-x64.zip", browser_download_url = $"https://{Host}/mweint/Pace/releases/download/{Version}/Pace-win-x64.zip", digest = "sha256:" + new string('0', 64) } }
            })) });
        }
    }
    public static async Task Run(Action<bool, string> check)
    {
        check(AppUpdates.IsNewer("v0.2.0", "0.1.0") && !AppUpdates.IsNewer("v0.1.0", "0.1.0") &&
            !AppUpdates.IsNewer("v0.0.9", "0.1.0") && !AppUpdates.IsNewer("v0.2.0-preview", "0.1.0"), "Updates accept only newer stable versions");
        var settings = new Settings();
        int saves = 0;
        using var handler = new ReleaseHandler();
        using var http = new HttpClient(handler);
        var updates = new AppUpdates(settings, http, () => saves++);
        await updates.Check();
        if (OperatingSystem.IsWindows())
        {
            check(updates.Notify && updates.Available?.Version == "v99.1.0" && !updates.Busy, "A compatible release enables the update notification");
            updates.Dismiss(); await updates.Check();
            check(!updates.Notify && updates.Available != null && saves == 1, "Dismiss suppresses one version while keeping Update available");
            handler.Version = "v99.2.0"; await updates.Check();
            check(updates.Notify, "A newer release notifies after a dismissed version");
        }
        else check(updates.Available == null, "Non-Windows builds do not install Windows packages");
        handler.Fail = true; await updates.Check();
        check(!updates.Busy && updates.Status.Contains("Try again"), "Update failures leave a retryable state");
        handler.Fail = false; handler.Host = "example.com"; await updates.Check();
        check(updates.Available == null && !updates.Notify, "Updates reject packages outside the official repository");
        var restored = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(new Settings { RelativeResetTime = true, AutomaticUpdates = true, DismissedUpdateVersion = "v99.1.0" }))!;
        check(restored.RelativeResetTime && restored.AutomaticUpdates && restored.DismissedUpdateVersion == "v99.1.0" &&
            !JsonSerializer.Deserialize<Settings>("{}")!.AutomaticUpdates, "General preferences persist and automatic updates default off");
    }
}
