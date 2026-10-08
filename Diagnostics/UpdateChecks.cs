using System.Net;
using System.Text.Json;

namespace Usage;

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
    public static void Run(Action<bool, string> check)
    {
        check(AppUpdates.IsNewer("v0.2.0", "0.1.0") && !AppUpdates.IsNewer("v0.1.0", "0.1.0") &&
            !AppUpdates.IsNewer("v0.0.9", "0.1.0") && !AppUpdates.IsNewer("v0.2.0-preview", "0.1.0"),
            "Updates accept only newer stable versions");
        var settings = new Settings();
        int saves = 0;
        using var handler = new ReleaseHandler();
        using var http = new HttpClient(handler);
        var updates = new AppUpdates(settings, http, () => saves++);
        updates.Check().GetAwaiter().GetResult();
        check(updates.Notify && updates.Available?.Version == "v99.1.0" && !updates.Busy, "A compatible release enables the update notification");
        updates.Dismiss();
        updates.Check().GetAwaiter().GetResult();
        check(!updates.Notify && updates.Available != null && saves == 1, "Dismiss suppresses one version while keeping Update available");
        handler.Version = "v99.2.0";
        updates.Check().GetAwaiter().GetResult();
        check(updates.Notify, "A newer release notifies after the previous version was dismissed");
        handler.Fail = true;
        updates.Check().GetAwaiter().GetResult();
        check(!updates.Busy && updates.Status.Contains("Try again"), "Update check failure leaves a retryable state");
        handler.Fail = false; handler.Host = "example.com";
        updates.Check().GetAwaiter().GetResult();
        check(updates.Available == null && !updates.Notify, "Updates reject packages outside the official release repository");
        string json = JsonSerializer.Serialize(new Settings { RelativeResetTime = true, AutomaticUpdates = true, DismissedUpdateVersion = "v99.1.0" });
        var restored = JsonSerializer.Deserialize<Settings>(json)!;
        check(restored.RelativeResetTime && restored.AutomaticUpdates && restored.DismissedUpdateVersion == "v99.1.0" &&
            !JsonSerializer.Deserialize<Settings>("{}")!.AutomaticUpdates, "General preferences persist and automatic updates default off");
        using var dialog = new AccountsDialog([], settings, () => { }, persist: () => { }, updates: updates, showGeneral: true);
        dialog.Show();
        Application.DoEvents();
        check(dialog.Controls.OfType<GeneralSettingsView>().Single().Visible && !dialog.AccountList.Visible,
            "Settings starts on General when opened from the overview");
        var time = dialog.Controls.OfType<GeneralSettingsView>().Single().Controls.OfType<SettingsSection>().First();
        var buttons = time.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<Button>().ToList();
        buttons.Single(button => button.Text == "Time remaining").PerformClick();
        check(settings.RelativeResetTime, "Time remaining applies from General without a Save button");
        buttons.Single(button => button.Text == "When it resets").PerformClick();
        check(!settings.RelativeResetTime, "Clock reset times can be restored from General");
        var generalBounds = dialog.Bounds;
        var footer = dialog.Controls.OfType<Panel>().Single(panel => panel.Dock == DockStyle.Bottom);
        int footerTop = footer.Top;
        check(footer.Height == dialog.LogicalToDeviceUnits(UiMetrics.ToolbarHeight) && !footer.Controls.OfType<FlowLayoutPanel>().Any(),
            "The shared Settings footer uses one centered text row");
        dialog.SelectTab(false);
        check(dialog.Bounds == generalBounds && footer.Top == footerTop,
            "Switching Settings tabs keeps window bounds and shared footer fixed");
        check(!dialog.Controls.OfType<GeneralSettingsView>().Single().Visible && dialog.AccountList.Visible,
            "Accounts remains available in the Settings tabs");
        var tabs = dialog.Controls.OfType<TabStrip>().Single();
        tabs.AccessibilityObject.GetChild(0)!.DoDefaultAction();
        check(tabs.SelectedIndex == 0 && dialog.Controls.OfType<GeneralSettingsView>().Single().Visible,
            "Themed tabs expose selectable General and Accounts pages to accessibility tools");
        check(dialog.Bounds == generalBounds, "Returning to General preserves the Settings window size");
        var accounts = Enumerable.Range(0, 3).Select(index => new Reading(new Account("sample" + index, "Codex", "Sample", ""), null, null, DateTimeOffset.UtcNow)).ToList();
        using var three = new AccountsDialog(accounts, new Settings(), () => { }, persist: () => { });
        three.Show(); Application.DoEvents();
        var viewport = three.Controls.OfType<ScrollViewport>().Single();
        check(!viewport.Overflow && !three.AccountList.AutoScroll && three.AccountList.Controls.Cast<Control>().All(control => control.Bottom <= viewport.Height),
            "Three account rows fit completely without a native or themed scrollbar");
        var fixedBounds = three.Bounds;
        three.SelectTab(true); three.SelectTab(false);
        check(three.Bounds == fixedBounds && !viewport.Overflow, "Tab switching preserves the full three-account viewport");
        using var longer = new AccountsDialog(Enumerable.Range(0, 5).Select(index => new Reading(new Account("overflow" + index, "Codex", "Sample", ""), null, null, DateTimeOffset.UtcNow)).ToList(), new Settings(), () => { }, persist: () => { });
        longer.Show(); Application.DoEvents();
        var scrolling = longer.Controls.OfType<ScrollViewport>().Single();
        typeof(Control).GetMethod("OnMouseWheel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(scrolling, [new MouseEventArgs(MouseButtons.None, 0, 0, 0, -120)]);
        check(scrolling.Overflow && !longer.AccountList.AutoScroll && longer.AccountList.Top < 0,
            "Longer account lists scroll with the wheel using the themed viewport");
    }
}
