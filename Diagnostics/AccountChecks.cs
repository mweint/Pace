using System.Text.Json;

namespace Usage;

internal static partial class ViewChecks
{
    static void CheckAccounts(Action<bool, string> Check, UsagePanel layout, List<Reading> layoutAccounts)
    {
        var editorSettings = new Settings();
        int saved = 0;
        using var accountsDialog = new AccountsDialog(layoutAccounts, editorSettings, () =>
        {
        }, persist: () => saved++);
        layout.EditingAccounts = true;
        layout.OpenNearTray();
        accountsDialog.Show(layout);
        Application.DoEvents();
        if (SystemInformation.IsMenuAnimationEnabled)
        {
            Pump(30);
            Check(accountsDialog.Opacity > 0 && accountsDialog.Opacity < 1 && layout.Visible && !layout.IsClosing, "Accounts fades in while the main view stays behind it");
        }

        Check(accountsDialog.ClientSize.Width == layout.ClientSize.Width && accountsDialog.Controls.OfType<Panel>().Any(p => p.Dock == DockStyle.Bottom && p.Controls.OfType<IconButton>().Any()), "Accounts matches main width and uses shared bottom navigation");
        var editable = accountsDialog.AccountList.Controls.OfType<AccountEditorRow>().First();
        Check(!editable.NameInput.Focused && editable.NameInput.SelectionLength == 0 && editable.NameInput.ReadOnly, "Accounts opens with names unselected and outside editing mode");
        var animatedList = accountsDialog.AccountList;
        animatedList.MoveRow(editable, 2);
        int startingTop = editable.Top;
        Pump(80);
        Check(editable.Top > startingTop && editable.Top < animatedList.TargetTop(editable), "Reordered rows animate toward their new slots");
        animatedList.FinishMotion();
        Check(animatedList.Controls.GetChildIndex(editable) == 2, "Animated reorder updates the actual saved row order");
        animatedList.MoveRow(editable, 0);
        animatedList.FinishMotion();
        editable.NameEditor.BeginEditing();
        editable.NameInput.Text = "Saved name";
        editable.NameEditor.FinishEditing(true);
        editable.PanelToggle.Checked = false;
        long saveDeadline = Environment.TickCount64 + 450;
        while (Environment.TickCount64 < saveDeadline)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        Check(saved > 0 && editorSettings.Accounts.First().Alias == "Saved name" && !editorSettings.Accounts.First().Show, "Accounts applies names and visibility automatically while open");
        var removeRow = animatedList.Controls.OfType<AccountEditorRow>().Last();
        string removeKey = removeRow.Preference.Key;
        var removedAccount = layoutAccounts.Single(r => r.Account.Key == removeKey).Account;
        int beforeRemoval = animatedList.Controls.Count;
        removeRow.RemoveButton.Visible = true;
        removeRow.RemoveButton.PerformClick();
        Check(animatedList.Controls.Count == beforeRemoval - 1 && !editorSettings.Accounts.Any(p => p.Key == removeKey), "Remove action drops the account row and its preferences immediately");
        var persistedRemoval = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(editorSettings))!;
        Check(persistedRemoval.IsRemoved(removedAccount), "Removed accounts remain excluded after settings reload");
        persistedRemoval.RestoreAccount(removedAccount);
        Check(!persistedRemoval.IsRemoved(removedAccount), "Adding the same account again can restore monitoring");
        accountsDialog.Close();
        if (SystemInformation.IsMenuAnimationEnabled)
        {
            Pump(60);
            Check(accountsDialog.Visible && accountsDialog.Opacity < 1 && layout.Visible, "Accounts fades out to reveal the main view on return");
            Pump(200);
        }

        layout.EditingAccounts = false;
        Check(saved > 0 && editorSettings.Accounts.First().Alias == "Saved name" && !editorSettings.Accounts.First().Show, "Closing accounts preserves edits without a Save button");
    }
}
