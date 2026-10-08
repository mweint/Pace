using System.Reflection;

namespace Usage;

internal static class NameEditingChecks
{
    public static void Run(Action<bool, string> check)
    {
        var account = new Account("demo-name", "Codex", "Demo", "");
        using var host = new WidgetForm { ClientSize = new Size(UiMetrics.PanelWidth, 180) };
        using var row = new AccountEditorRow(new(account, null, null, DateTimeOffset.UtcNow), new Preference { Key = account.Key, Alias = "Original" }, () => { });
        host.Controls.Add(row);
        host.Show();
        Application.DoEvents();
        int commits = 0;
        row.EditedChanged += () => commits++;
        row.NameEditor.BeginEditing();
        row.NameInput.Text = "Draft";
        check(commits == 0 && row.Edited().Alias == "Original", "Typing a name keeps the draft separate from saved preferences");
        var keyHandler = row.NameInput.GetType().GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
        bool consumed = (bool)keyHandler.Invoke(row.NameInput, [Message.Create(row.NameInput.Handle, 0x100, IntPtr.Zero, IntPtr.Zero), Keys.Escape])!;
        check(consumed && row.NameInput.ReadOnly && row.NameInput.Text == "Original" && commits == 0, "Escape cancels name editing before the page can handle it as Back");
        row.NameEditor.BeginEditing();
        row.NameInput.Text = "Renamed";
        consumed = (bool)keyHandler.Invoke(row.NameInput, [Message.Create(row.NameInput.Handle, 0x100, IntPtr.Zero, IntPtr.Zero), Keys.Enter])!;
        check(consumed && row.NameInput.ReadOnly && row.Edited().Alias == "Renamed" && commits == 1, "Enter commits the name once and returns to display mode");
        row.NameEditor.BeginEditing();
        row.NameInput.Text = "Confirmed";
        row.NameEditor.Controls.OfType<IconButton>().Single().PerformClick();
        check(row.NameInput.ReadOnly && row.Edited().Alias == "Confirmed" && commits == 2, "The themed checkmark commits the edited name");
        row.PanelToggle.Focus();
        typeof(Control).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(row.PanelToggle, [new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)]);
        check(!row.ShouldRevealRemoval(false) && row.ShouldRevealRemoval(true), "Mouse-focused toggles do not keep removal visible after hover leaves");
        typeof(Control).GetMethod("OnPreviewKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(row.PanelToggle, [new PreviewKeyDownEventArgs(Keys.Tab)]);
        check(row.ShouldRevealRemoval(false), "Keyboard navigation keeps the removal action available");

        var settings = new Settings();
        var readings = Enumerable.Range(0, 5).Select(i => new Reading(new("capacity-" + i, "Codex", "Demo", ""), null, null, DateTimeOffset.UtcNow)).ToList();
        using var accounts = new AccountsDialog(readings, settings, () => { }, persist: () => { });
        accounts.Show();
        Application.DoEvents();
        var rows = accounts.AccountList.Controls.OfType<AccountEditorRow>().ToList();
        check(!rows[4].TrayToggle.Enabled && rows[4].TrayToggle.AccessibleDescription == "Tray full", "A fifth account stays in the panel but cannot exceed four tray slots");
        rows[0].TrayToggle.Checked = false;
        check(rows[4].TrayToggle.Enabled && rows[4].TrayToggle.Text == "Tray", "Freeing a tray slot immediately enables another account");
    }
}
