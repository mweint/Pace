using System.Text.Json;
using System.Text.Json.Nodes;

namespace Usage;

internal static partial class ViewChecks
{
    public static void Run(Action<bool, string> Check, DateTimeOffset now, List<UsageLimit> detailLimits)
    {
        using var editor = new AccountEditorRow(new(new("test", "Claude", "Account", ""), null, null, now), new()
        {
            Key = "test"
        }, () =>
        {
        });
        editor.NameInput.Text = "Work Claude";
        editor.TrayToggle.Checked = false;
        Check(editor.Edited().Alias == "Work Claude" && !editor.Edited().Tray, "Account name and tray selection are directly editable");
        using var layout = new UsagePanel();
        var layoutAccounts = Enumerable.Range(0, 4).Select(i => new Reading(new(i.ToString(), "Codex", "Account", ""), new(57, now.AddDays(3.5), TimeSpan.FromDays(7)), null, now)).ToList();
        layout.UpdateRows(layoutAccounts, new Settings(), false);
        layout.Show();
        Application.DoEvents();
        var cardContainer = layout.Controls.OfType<FlowLayoutPanel>().Single(c => c.Controls.OfType<AccountRow>().Any());
        var firstCard = cardContainer.Controls.OfType<AccountRow>().First();
        int leftGap = cardContainer.Left + firstCard.Left;
        int rightGap = layout.ClientSize.Width - cardContainer.Left - firstCard.Right;
        var lastCard = cardContainer.Controls.OfType<AccountRow>().Last();
        int bottomGap = cardContainer.Height - lastCard.Bottom + layout.Padding.Bottom;
        Check(leftGap == rightGap && bottomGap == leftGap && !cardContainer.VerticalScroll.Visible, "Usage cards have matching side and bottom margins and no unnecessary scrollbar");
        Check(layout.FormBorderStyle == FormBorderStyle.None, "Usage panel has a custom header without native window buttons");
        layout.UpdateRows(layoutAccounts, new Settings(), true);
        Check(ReferenceEquals(firstCard, cardContainer.Controls.OfType<AccountRow>().First()), "Refreshing reuses cards without tearing down their controls");
        using var hover = new TrayHover();
        hover.UpdateEntries(layoutAccounts, new Settings());
        var area = Screen.PrimaryScreen!.WorkingArea;
        var anchor = new Point(area.Right - 30, area.Bottom - 10);
        hover.Open(anchor);
        Application.DoEvents();
        Check(hover.StartPosition == FormStartPosition.Manual && area.Contains(hover.Bounds) && hover.Bottom <= anchor.Y && Math.Abs(hover.Right - anchor.X) < hover.Width, "First hover opens by its tray anchor inside the screen");
        Check(hover.ClientSize.Width == (int)(250 * hover.DeviceDpi / 96f), "Hover uses the compact 250px width");
        CheckAccounts(Check, layout, layoutAccounts);
        CheckNavigation(Check, layout, layoutAccounts, now, detailLimits);
        using var nativeIcon = new NativeTrayIcon();
        using var testIcon = TrayDrawing.Icon([]);
        nativeIcon.Icon = testIcon;
        nativeIcon.Visible = true;
        Check(nativeIcon.Added && nativeIcon.TooltipSuppressed, "Native tray registers with standard tooltip suppression enabled");
    }

    static void Pump(int milliseconds)
    {
        long end = Environment.TickCount64 + milliseconds;
        while (Environment.TickCount64 < end)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }
}
