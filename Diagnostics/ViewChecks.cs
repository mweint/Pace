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
        editor.NameEditor.BeginEditing();
        editor.NameInput.Text = "Work Claude";
        editor.NameEditor.FinishEditing(true);
        editor.TrayToggle.Checked = false;
        Check(editor.Edited().Alias == "Work Claude" && !editor.Edited().Tray, "Account name and tray selection are directly editable");
        var compactReading = new Reading(new("compact", "Claude", "Demo", ""), new(20, now.AddDays(3), TimeSpan.FromDays(7)), null, now,
            Limits: [new("session", "Five-hour", new(82, now.AddHours(1), TimeSpan.FromHours(5))), new("model:Fable", "Fable · Weekly", new(34, now.AddDays(3), TimeSpan.FromDays(7)))]);
        using var compactRow = new AccountRow(compactReading, "Demo", showSupplementalLimits: true);
        using var layoutTitle = Palette.AccountFont();
        using var layoutBody = Palette.BodyFont();
        var compactLayout = AccountRowLayout.Create(compactRow.Width, layoutTitle, layoutBody, true, 2);
        Check(compactLayout.Secondary.Length == 2 && compactRow.Height - compactLayout.Secondary[^1].Bottom == UiMetrics.ContentInset && compactLayout.Header.Top == UiMetrics.ContentInset && compactLayout.Secondary[^1].Left == UiMetrics.ContentInset, "Claude sections have equal top, side and bottom insets derived from their final content row");
        Check(compactLayout.Secondary[0].Top - compactLayout.Metadata.Bottom == UiMetrics.CompactLimitGroupGap && compactLayout.Secondary[1].Top > compactLayout.Secondary[0].Bottom, "Secondary limits form a distinct group without overlapping metadata or each other");
        compactRow.UpdateReading(compactReading with { Limits = null }, "Demo");
        int basicHeight = AccountRowLayout.Create(compactRow.Width, layoutTitle, layoutBody, true, 0).Height;
        Check(compactRow.Height == basicHeight, "A refresh removes supplemental space when limits are unavailable");
        using var detailRow = new AccountRow(compactReading, "Weekly", showServiceMark: false);
        Check(detailRow.Height == basicHeight && CompactLimitBars.For(compactReading with { Account = compactReading.Account with { Service = "Codex" } }).Count == 0, "Detailed limit cards and Codex cards do not gain supplemental bars");
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
        Check(leftGap == rightGap && bottomGap == leftGap && !cardContainer.VerticalScroll.Visible && firstCard.BackColor == layout.BackColor && !firstCard.ShowSeparator && lastCard.ShowSeparator, "Flat overview sections share one background with separators and no redundant outer card padding");
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
        var trayBounds = new Rectangle(area.Right - 70, area.Bottom + 4, 24, 24);
        hover.Open(trayBounds);
        int gap = (int)Math.Round(UiMetrics.ScreenInset * hover.DeviceDpi / 96f);
        Check(hover.Bottom == area.Bottom - gap, "Hover preserves the taskbar gap instead of clamping against its edge");
        hover.UpdateEntries(layoutAccounts.Take(1).ToList(), new Settings());
        Check(hover.Bottom == area.Bottom - gap && hover.ClientSize.Height == (int)((2 * UiMetrics.HoverInset + UiMetrics.ServiceIconSize) * hover.DeviceDpi / 96f), "Hover keeps equal vertical insets and its anchor when account count changes");
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
