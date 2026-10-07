namespace Usage;

internal static partial class ViewChecks
{
    static void CheckNavigation(Action<bool, string> Check, UsagePanel layout, List<Reading> layoutAccounts, DateTimeOffset now, List<UsageLimit> detailLimits)
    {
        layout.OpenNearTray();
        var panelArea = layout.AnchorArea;
        Check(layout.Left == Math.Max(panelArea.Left, panelArea.Right - layout.Width - 8) && layout.Controls.Cast<Control>().Any(c => c.Dock == DockStyle.Bottom), "Main panel anchors at screen right with controls in the bottom bar");
        Pump(300);
        Check(layout.Visible && layout.Opacity == 1, "Opening completes fully visible after the slide and fade");
        layout.Dismiss();
        if (SystemInformation.IsMenuAnimationEnabled)
        {
            Pump(100);
            Check(layout.Visible && layout.IsClosing && layout.Opacity < 1, "Closing fades before hiding the popup");
        }

        Pump(300);
        Check(!layout.Visible && !layout.IsClosing && layout.Opacity == 1, "Closing finishes hidden and resets opacity for next opening");
        layout.EditingAccounts = true;
        layout.OpenNearTray();
        using var closeAllDialog = new AccountsDialog(layoutAccounts, new Settings(), () =>
        {
        }, persist: () =>
        {
        });
        closeAllDialog.Show(layout);
        Pump(300);
        closeAllDialog.DismissAll();
        layout.Dismiss();
        Pump(300);
        Check(closeAllDialog.CloseAllRequested && !closeAllDialog.Visible && !layout.Visible, "Dismiss-all closes Accounts and the main popup together");
        layout.EditingAccounts = false;
        layout.OpenNearTray();
        Pump(300);
        Check(layout.Visible && layout.Opacity == 1, "Normal view can reopen after closing both views");
        var oldPointer = Cursor.Position;
        try
        {
            var originalArea = layout.AnchorArea;
            var anotherScreen = Screen.AllScreens.FirstOrDefault(screen => screen.WorkingArea != originalArea);
            if (anotherScreen != null)
            {
                Cursor.Position = new Point(anotherScreen.WorkingArea.Left + 40, anotherScreen.WorkingArea.Top + 40);
                layout.UpdateRows(layoutAccounts, new Settings(), false);
                layout.OpenNearTray();
                Pump(300);
                Check(layout.AnchorArea == originalArea && layout.Left == originalArea.Right - layout.Width - 8, "An open popup retains its monitor during refresh and reopening with the pointer on another monitor");
                layout.EditingAccounts = true;
                using var monitorDialog = new AccountsDialog(layoutAccounts, new Settings(), () =>
                {
                }, persist: () =>
                {
                });
                monitorDialog.Show(layout);
                Pump(300);
                Check(Screen.FromControl(monitorDialog).WorkingArea == originalArea, "Accounts opens on the popup session monitor rather than the pointer monitor");
                monitorDialog.Close();
                Pump(300);
                layout.EditingAccounts = false;
                layout.OpenNearTray(newSession: false);
                Pump(300);
                Check(layout.AnchorArea == originalArea, "Returning from Accounts retains the same popup session monitor");
            }
        }
        finally
        {
            Cursor.Position = oldPointer;
            layout.EditingAccounts = false;
        }

        layout.EditingAccounts = true;
        var detailAccount = new Reading(new("details", "Claude", "Demo", ""), detailLimits[0].Window, null, now, Limits: detailLimits);
        using var detailDialog = new AccountDetailsDialog(detailAccount, new Settings());
        detailDialog.Show(layout);
        Pump(300);
        Check(detailDialog.Visible && detailDialog.ClientSize.Width == layout.ClientSize.Width && detailDialog.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<AccountRow>().Count() == 3, "Account details reuse the main width and cards for all returned limits");
        detailDialog.Close();
        Pump(300);
        Check(!detailDialog.Visible && !detailDialog.CloseAllRequested && layout.Visible, "Back from account details keeps the main view available");
        layout.EditingAccounts = false;
        // The destination starts before the source exit completes, in both directions.
        foreach (bool details in new[]
        {
            false,
            true
        }

        )
        {
            layout.OpenNearTray(newSession: false);
            Pump(300);
            var navigationArea = layout.AnchorArea;
            layout.EditingAccounts = true;
            using PageDialog destination = details ? new AccountDetailsDialog(detailAccount, new Settings()) : new AccountsDialog(layoutAccounts, new Settings(), () =>
            {
            }, persist: () =>
            {
            });
            int phase = 0;
            bool enterOverlap = false, exitOverlap = false, enterMovement = false, exitMovement = false;
            using var inspect = new System.Windows.Forms.Timer
            {
                Interval = 65
            };
            inspect.Tick += (_, _) =>
            {
                if (phase == 0)
                {
                    enterOverlap = destination.Visible && layout.Visible && destination.Opacity > 0 && destination.Opacity < 1 && layout.Opacity > 0 && layout.Opacity < 1;
                    enterMovement = destination.Top > PopupPlacement.BottomRight(navigationArea, destination.Size).Y && layout.Top == PopupPlacement.BottomRight(navigationArea, layout.Size).Y;
                    phase++;
                    inspect.Interval = 200;
                }
                else if (phase == 1)
                {
                    phase++;
                    destination.Close();
                    inspect.Interval = 65;
                }
                else
                {
                    exitOverlap = destination.Visible && layout.Visible && destination.Opacity > 0 && destination.Opacity < 1 && layout.Opacity > 0 && layout.Opacity < 1;
                    exitMovement = destination.Top > PopupPlacement.BottomRight(navigationArea, destination.Size).Y && layout.Top == PopupPlacement.BottomRight(navigationArea, layout.Size).Y;
                    inspect.Stop();
                }
            };
            inspect.Start();
            PageNavigation.Show(layout, destination);
            if (Motion.Enabled)
                Check(enterOverlap && exitOverlap, "Both separate windows crossfade simultaneously on entry and return");
            if (Motion.Enabled && Motion.NavigationSlideEnabled)
                Check(enterMovement && exitMovement, "The page slides up on entry and down on Back while the overview fades in place");
            layout.EditingAccounts = false;
            Pump(40);
            Check(!destination.Visible && layout.Visible && layout.Opacity == 1 && layout.AnchorArea == navigationArea, "Crossfade ends with only the overview on its original monitor");
        }
    }
}
