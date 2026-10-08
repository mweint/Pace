using System.Text.Json;

namespace Usage;

internal static class ConnectionChecks
{
    public static void Run(Action<bool, string> check)
    {
        var account = new Account("demo", "Codex", "Demo account", Path.Combine(Path.GetTempPath(), "pace-missing-" + Guid.NewGuid().ToString("N"), "auth.json"));
        var settings = new Settings();
        Providers.RememberAccounts([account], settings);
        var preference = settings.For(account);
        preference.Alias = "Work";
        preference.Tray = false;
        var restored = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(settings))!;
        var retained = Providers.RememberAccounts([], restored);
        check(retained.Single() == account && restored.For(account).Alias == "Work" && !restored.For(account).Tray, "Disconnected accounts retain identity, names and visibility across settings reload");
        var missing = Providers.Fetch(account).GetAwaiter().GetResult();
        check(missing.ConnectionIssue == ConnectionIssue.CredentialsMissing && missing.NeedsReconnect, "Missing credentials require reconnect without making a usage request");
        var ready = missing with { Error = null, ConnectionIssue = ConnectionIssue.None };
        using var host = new WidgetForm { ClientSize = new Size(UiMetrics.PanelWidth, 160) };
        using var row = new AccountEditorRow(ready, preference, () => { });
        host.Controls.Add(row);
        host.Show();
        Application.DoEvents();
        var reconnect = row.Controls.OfType<Button>().Single(b => b.Text == "Reconnect");
        check(!reconnect.Visible, "Healthy accounts do not show a reconnect action");
        row.UpdateConnection(ready with { Error = "Service rate limit" });
        check(!reconnect.Visible, "Ordinary stale usage and rate limits do not prompt reconnect");
        row.UpdateConnection(missing);
        check(reconnect.Visible && row.Controls.OfType<Label>().Any(l => l.Visible && l.Text == "Sign-in missing"), "Authentication problems show reconnect with a short reason");
        row.UpdateConnection(ready);
        check(!reconnect.Visible, "Successful authentication clears the reconnect action");
        check(row.Grip.Left > 2 && row.Grip.Right < row.NameEditor.Left && row.Grip.Top + row.Grip.Height / 2 == row.NameEditor.Top + row.NameEditor.Height / 2, "Drag grip aligns with the name without covering the editor or card outline");
        check(row.Edited().RememberedAccount == account, "Editing account preferences preserves remembered connection metadata");
        restored.RemoveAccount(account);
        check(Providers.RememberAccounts([account], restored).Count == 0, "Explicitly removed accounts are not restored as disconnected accounts");
    }
}
