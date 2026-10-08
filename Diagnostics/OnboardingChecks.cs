namespace Usage;

internal static class OnboardingChecks
{
    public static void Run(Action<bool, string> check)
    {
        using var panel = new UsagePanel();
        var settings = new Settings();
        panel.UpdateRows([], settings, true);
        var container = panel.Controls.OfType<FlowLayoutPanel>().Single();
        var empty = container.Controls.OfType<EmptyAccountsView>().Single();
        var buttons = empty.Controls.OfType<Button>().ToList();
        check(!empty.HasAccounts && buttons.Count == 2 && buttons.All(b => !b.Enabled), "First-time account actions wait for discovery");
        panel.UpdateRows([], settings, false);
        panel.Show();
        Application.DoEvents();
        check(ReferenceEquals(empty, container.Controls[0]) && buttons.All(b => b.Enabled), "Empty view is reused after discovery and enables sign-in");
        string? requested = null;
        panel.AddAccountRequested += service => requested = service;
        buttons.Single(b => b.Text == "Add Claude").PerformClick();
        check(requested == "Claude", "First-time buttons request the selected service sign-in");
        panel.UpdateSignIn(true, "Complete sign-in in your browser…");
        panel.UpdateRows([], settings, false);
        check(buttons.All(b => !b.Enabled), "Refresh cannot re-enable buttons during sign-in");
        panel.UpdateSignIn(false, "Sign-in failed. Please try again.");
        check(buttons.All(b => b.Enabled), "Failed sign-in allows retry without leaving the empty view");
        check(empty.Controls.Cast<Control>().All(c => c.Font.Name.StartsWith("Pace Sans")), "First-time labels and buttons use packaged theme fonts");
        check(empty.BackColor == Palette.Background && buttons.All(b => b.Image != null && b.Width == buttons[0].Width), "First-time service actions use shared icon buttons with equal widths and no nested card");
        var account = new Account("demo", "Codex", "Demo", "");
        settings.For(account).Show = false;
        panel.UpdateRows([new(account, null, null, DateTimeOffset.UtcNow)], settings, false);
        empty = container.Controls.OfType<EmptyAccountsView>().Single();
        check(empty.HasAccounts && empty.Controls.OfType<Button>().Single().Text == "Manage accounts", "Hidden accounts lead to account management instead of duplicate sign-in");
        bool manageRequested = false, settingsRequested = false;
        panel.ManageAccountsRequested += () => manageRequested = true;
        panel.SettingsRequested += () => settingsRequested = true;
        empty.Controls.OfType<Button>().Single().PerformClick();
        check(manageRequested && !settingsRequested, "Manage accounts requests the Accounts tab rather than General settings");
        settings.For(account).Show = true;
        panel.UpdateRows([new(account, null, null, DateTimeOffset.UtcNow)], settings, false);
        check(container.Controls.OfType<AccountRow>().Count() == 1 && !container.Controls.OfType<EmptyAccountsView>().Any(), "Detected visible accounts replace onboarding with the normal overview");
    }
}
