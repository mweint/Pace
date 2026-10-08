using System.Text.Json;

namespace Usage;

internal static class LimitWarningChecks
{
    public static void Run(Action<bool, string> check, DateTimeOffset now)
    {
        var session = new Window(90, now.AddHours(1), TimeSpan.FromHours(5));
        var reading = new Reading(new("warning", "Claude", "Sample", ""),
            new(20, now.AddDays(4), TimeSpan.FromDays(7)), null, now,
            Limits: [new("session", "Five-hour", session), new("model:Fable", "Fable", new(30, now.AddDays(4), TimeSpan.FromDays(7)))]);
        var hidden = new Preference { ShowFiveHour = false, ShowFable = false };
        check(CompactLimitBars.For(reading, hidden).Count == 0 && LimitWarning.For(reading, now).Count == 1,
            "Hiding supplemental bars preserves approaching-limit warnings");
        check(LimitWarning.Hidden(reading, new Preference(), now).Count == 0 &&
            LimitWarning.Hidden(reading, hidden, now).Count == 1 &&
            LimitWarning.Hidden(reading, new Preference { ShowFable = false }, now).Count == 0,
            "Overview dots only flag limits whose bars are hidden");
        check(LimitWarning.Hidden(reading with { Weekly = session, Limits = [new("weekly", "Weekly", session)] }, hidden, now).Count == 0,
            "The visible weekly limit never creates a hidden-limit dot");
        check(!LimitWarning.Approaching(session with { Used = 89.9 }, now) &&
            LimitWarning.Approaching(session with { Used = 100 }, now),
            "Limit warnings begin at 90 percent and include exhausted limits");
        check(!LimitWarning.Approaching(session, session.Reset!.Value) &&
            !LimitWarning.Approaching(session with { Used = double.NaN }, now) &&
            LimitWarning.For(reading with { Error = "Stale" }, now).Count == 0,
            "Expired, invalid and stale limits do not raise current warnings");
        check(LimitWarning.For(reading with { Limits = [new("session", "Five-hour", session with { Used = 20 })] }, now).Count == 0,
            "Warnings clear when refreshed usage recovers");
        var restored = JsonSerializer.Deserialize<Preference>(JsonSerializer.Serialize(hidden))!;
        var legacy = JsonSerializer.Deserialize<Preference>("{}")!;
        check(!restored.ShowFiveHour && !restored.ShowFable && legacy.ShowFiveHour && legacy.ShowFable,
            "Limit visibility persists and existing settings default to visible bars");
        using var row = new AccountRow(reading, "Sample", showSupplementalLimits: true, preference: hidden);
        int hiddenHeight = row.Height;
        row.UpdateReading(reading, "Sample", new Preference());
        check(row.Height > hiddenHeight, "Changing visibility recomputes the existing account row height");
        var settings = new Settings();
        using (var accounts = new AccountsDialog([reading], settings, () => { }, persist: () => { }))
        {
            var editor = accounts.AccountList.Controls.OfType<AccountEditorRow>().Single();
            var toggles = editor.Controls.OfType<AccountToggle>().ToList();
            toggles.Single(toggle => toggle.Text == "5H bar").Checked = false;
            toggles.Single(toggle => toggle.Text == "Fable bar").Checked = false;
            check(toggles.All(toggle => toggle.Top == editor.PanelToggle.Top) &&
                toggles.Max(toggle => toggle.Right) <= editor.Width - UiMetrics.ContentInset,
                "All four Claude settings fit on one line within the content inset");
            check(editor.PanelToggle.Left == editor.NameEditor.Left &&
                toggles.All(toggle => toggle.Width >= toggle.GetPreferredSize(Size.Empty).Width),
                "Settings align with the name box and reserve measured widths for complete labels");
            check(accounts.SaveEdits() && !settings.For(reading.Account).ShowFiveHour && !settings.For(reading.Account).ShowFable,
                "Account options save both visibility toggles without changing warning detection");
        }
        using var icon = TrayDrawing.Bitmap([reading], now);
        using var normal = TrayDrawing.Bitmap([reading with { Limits = null }], now);
        check(Enumerable.Range(0, icon.Width).All(x => Enumerable.Range(0, icon.Height).All(y => icon.GetPixel(x, y) == normal.GetPixel(x, y))),
            "Secondary limits do not add dots to the weekly tray icon");
        using var graphics = Graphics.FromImage(icon);
        using var font = Palette.AccountFont();
        using var format = new StringFormat(StringFormat.GenericTypographic);
        string fitted = AccountRow.FitName(graphics, "A very long account name with emoji 👩‍💻", font, format, 100);
        check(fitted.EndsWith("…") && graphics.MeasureString(fitted, font, int.MaxValue, format).Width <= 100,
            "Long names truncate within the width reserved before the warning dot");
    }
}
