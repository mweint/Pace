using Avalonia.Media.Imaging;

namespace Pace;

internal static class Preview
{
    public static async Task Render(string path)
    {
        string folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(folder);
        var demo = SampleData.Readings();
        var prefs = new Settings();
        foreach (var reading in demo) prefs.For(reading.Account);
        var overview = new UsagePanel { PreviewMode = true };
        overview.UpdateRows(demo, prefs, false);
        await Capture(overview, path);
        overview.UpdateNotification(true);
        await Capture(overview, Path.Combine(folder, "update-notification.png"));
        overview.Shutdown();
        foreach (var (name, entries) in new[] { ("accounts-three.png", demo.Take(3).ToList()), ("accounts-empty.png", new List<Reading>()) })
        {
            var view = new AccountsDialog(entries, new Settings(), () => { }, persist: () => { }) { PreviewMode = true };
            await Capture(view, Path.Combine(folder, name)); view.Close();
        }
        var accounts = new AccountsDialog(demo, prefs, () => { }, persist: () => { }) { PreviewMode = true };
        await Capture(accounts, Path.Combine(folder, "accounts-preview.png"));
        ((Control)accounts.Content!).GetVisualDescendants().OfType<TabStrip>().Single().Focus(NavigationMethod.Tab);
        await Capture(accounts, Path.Combine(folder, "accounts-tabs-focused.png"));
        var first = accounts.AccountList.Children.OfType<AccountEditorRow>().First();
        first.RemoveButton.Opacity = 1;
        await Capture(accounts, Path.Combine(folder, "accounts-name-hover-preview.png"));
        first.Dragging = true;
        await Capture(accounts, Path.Combine(folder, "accounts-drag-preview.png"));
        first.Dragging = false; first.NameEditor.BeginEditing();
        await Capture(accounts, Path.Combine(folder, "accounts-editing-preview.png"));
        first.NameEditor.FinishEditing(false);
        accounts.Close();
        var disconnected = new AccountsDialog(demo.Select((r, i) => i == 1 ? r with { Error = "Sign-in missing", ConnectionIssue = ConnectionIssue.CredentialsMissing } : r).ToList(), prefs, () => { }, persist: () => { }) { PreviewMode = true };
        await Capture(disconnected, Path.Combine(folder, "accounts-disconnected-preview.png")); disconnected.Close();
        var general = new AccountsDialog(demo, prefs, () => { }, persist: () => { }, showGeneral: true) { PreviewMode = true };
        await Capture(general, Path.Combine(folder, "settings-general.png"));
        var generalControls = ((Control)general.Content!).GetVisualDescendants().OfType<Control>().ToList();
        var back = generalControls.OfType<IconButton>().Single(b => b.Kind == "back");
        general.Activate(); await Task.Delay(40);
        back.Focus(NavigationMethod.Pointer);
        await Capture(general, Path.Combine(folder, "settings-mouse-focus.png"));
        back.Focus(NavigationMethod.Tab);
        await Capture(general, Path.Combine(folder, "settings-keyboard-focus.png"));
        var check = generalControls.OfType<FilledButton>().Single(b => b.Text == "Check for updates");
        check.IsEnabled = false;
        await Capture(general, Path.Combine(folder, "settings-disabled-button.png")); general.Close();
        using var handler = new UpdateChecks.ReleaseHandler();
        using var client = new HttpClient(handler);
        var updates = new AppUpdates(prefs, client, () => { });
        await updates.Check();
        var update = new AccountsDialog(demo, prefs, () => { }, persist: () => { }, updates: updates, showGeneral: true) { PreviewMode = true };
        await Capture(update, Path.Combine(folder, "settings-update.png")); update.Close();
        prefs.For(demo[2].Account).ShowFiveHour = prefs.For(demo[2].Account).ShowFable = false;
        var hidden = new UsagePanel { PreviewMode = true };
        hidden.UpdateRows(demo, prefs, false);
        await Capture(hidden, Path.Combine(folder, "hidden-limits-preview.png")); hidden.Shutdown();
        prefs.For(demo[2].Account).ShowFiveHour = prefs.For(demo[2].Account).ShowFable = true;
        var detail = new AccountDetailsDialog(SampleData.Detail(demo[2]), prefs) { PreviewMode = true };
        await Capture(detail, Path.Combine(folder, "account-detail-preview.png")); detail.Close();
        var resetless = new AccountDetailsDialog(SampleData.Detail(demo[2]) with { Limits = [new("weekly", "Weekly", demo[2].Weekly!), new("session", "Five-hour", new(0, null, TimeSpan.FromHours(5)))] }, prefs) { PreviewMode = true };
        await Capture(resetless, Path.Combine(folder, "resetless-detail-preview.png")); resetless.Close();
        var empty = new UsagePanel { PreviewMode = true };
        empty.UpdateRows([], new Settings(), false);
        await Capture(empty, Path.Combine(folder, "onboarding-preview.png")); empty.Shutdown();
        var hover = new TrayHover { PreviewMode = true };
        hover.UpdateEntries(demo, prefs);
        await Capture(hover, Path.Combine(folder, "hover-preview.png")); hover.Close();
        foreach (int size in new[] { 16, 20, 24, 32, 128 })
        {
            using var icon = TrayDrawing.Bitmap(demo, DateTimeOffset.UtcNow, size);
            icon.Save(Path.Combine(folder, size == 128 ? "tray-preview.png" : $"tray-{size}px.png"), PngBitmapEncoderOptions.Default);
        }
    }
    internal static async Task Capture(WidgetWindow view, string path)
    {
        view.PreviewMode = true;
        if (!view.IsVisible) view.Show();
        await Task.Delay(80);
        var root = (Control)view.Content!;
        root.Measure(new Size(view.Width, view.Height)); root.Arrange(new Rect(0, 0, view.Width, view.Height));
        await Task.Delay(40);
        foreach (var visual in root.GetVisualDescendants()) visual.InvalidateVisual();
        await Task.Delay(40);
        using var image = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(view.Width), (int)Math.Ceiling(view.Height)), new Vector(UiMetrics.BaseDpi, UiMetrics.BaseDpi));
        image.Render(root); image.Save(path, PngBitmapEncoderOptions.Default);
    }
}
