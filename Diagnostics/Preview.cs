namespace Usage;

internal static class Preview
{
    public static void Render(string path)
    {
        var now = DateTimeOffset.UtcNow;
        var demo = new List<Reading>();
        foreach (var (service, label, used, days) in new[]
        {
            ("Codex", "Personal", 57d, 3.64),
            ("Codex", "Work", 24d, 4.1),
            ("Claude", "Claude personal", 43d, 4d),
            ("Claude", "Claude work", 31d, 4.8)
        }

        )
            demo.Add(new(new(label, service, label, ""), new(used, now.AddDays(days), TimeSpan.FromDays(7)), null, now));
        demo[0] = demo[0] with
        {
            Resets = new(2, [new(1, now.AddDays(16)), new(1, now.AddDays(23))])
        };
        demo[1] = demo[1] with
        {
            Resets = new(1, [new(1, now.AddDays(2))])
        };
        demo[2] = demo[2] with { Limits = [new("session", "Five-hour", new(100, now.AddMinutes(4), TimeSpan.FromHours(5))), new("model:Fable", "Fable · Weekly", new(34, now.AddDays(4), TimeSpan.FromDays(7)))] };
        demo[3] = demo[3] with { Limits = [new("session", "Five-hour", new(18, now.AddHours(3), TimeSpan.FromHours(5))), new("model:Fable", "Fable · Weekly", new(67, now.AddDays(4.8), TimeSpan.FromDays(7)))] };
        using var panel = new UsagePanel();
        var prefs = new Settings();
        foreach (var (name, entries) in new[] { ("accounts-three.png", demo.Take(3).ToList()), ("accounts-empty.png", new List<Reading>()) })
        {
            using var accountView = new AccountsDialog(entries, new Settings(), () => { }, persist: () => { });
            accountView.Show(); Application.DoEvents();
            using var snapshot = new Bitmap(accountView.Width, accountView.Height);
            accountView.DrawToBitmap(snapshot, accountView.ClientRectangle);
            snapshot.Save(Path.Combine(Path.GetDirectoryName(path)!, name));
        }
        using (var handler = new UpdateChecks.ReleaseHandler())
        using (var client = new HttpClient(handler))
        {
            var updates = new AppUpdates(prefs, client, () => { });
            updates.Check().GetAwaiter().GetResult();
            using var updateView = new AccountsDialog(demo, prefs, () => { }, persist: () => { }, updates: updates, showGeneral: true);
            updateView.Show();
            Application.DoEvents();
            using var updateImage = new Bitmap(updateView.Width, updateView.Height);
            updateView.DrawToBitmap(updateImage, updateView.ClientRectangle);
            updateImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "settings-update.png"));
        }
        using (var general = new AccountsDialog(demo, prefs, () => { }, persist: () => { }, showGeneral: true))
        {
            general.Show();
            Application.DoEvents();
            using var snapshot = new Bitmap(general.Width, general.Height);
            general.DrawToBitmap(snapshot, general.ClientRectangle);
            snapshot.Save(Path.Combine(Path.GetDirectoryName(path)!, "settings-general.png"));
        }
        panel.UpdateRows(demo, prefs, false);
        panel.Show();
        Application.DoEvents();
        using var image = new Bitmap(panel.Width, panel.Height);
        panel.DrawToBitmap(image, panel.ClientRectangle with
        {
            Width = panel.Width,
            Height = panel.Height
        });
        image.Save(path);
        panel.UpdateNotification(true);
        using (var notification = new Bitmap(panel.Width, panel.Height))
        {
            panel.DrawToBitmap(notification, panel.ClientRectangle);
            notification.Save(Path.Combine(Path.GetDirectoryName(path)!, "update-notification.png"));
        }
        panel.UpdateNotification(false);
        prefs.For(demo[2].Account).ShowFiveHour = false;
        prefs.For(demo[2].Account).ShowFable = false;
        using (var hiddenPanel = new UsagePanel())
        {
            hiddenPanel.UpdateRows(demo, prefs, false);
            hiddenPanel.Show();
            Application.DoEvents();
            using var hiddenImage = new Bitmap(hiddenPanel.Width, hiddenPanel.Height);
            hiddenPanel.DrawToBitmap(hiddenImage, hiddenPanel.ClientRectangle);
            hiddenImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "hidden-limits-preview.png"));
        }
        prefs.For(demo[2].Account).ShowFiveHour = true;
        prefs.For(demo[2].Account).ShowFable = true;
        using var icon = TrayDrawing.Bitmap(demo, now, 128);
        icon.Save(Path.Combine(Path.GetDirectoryName(path)!, "tray-preview.png"));
        foreach (int size in new[]
        {
            16,
            20,
            24,
            32
        }

        )
        {
            using var nativeIcon = TrayDrawing.Bitmap(demo, now, size);
            nativeIcon.Save(Path.Combine(Path.GetDirectoryName(path)!, $"tray-{size}px.png"));
        }

        using var accounts = new AccountsDialog(demo, prefs, () =>
        {
        }, persist: () =>
        {
        });
        accounts.Show();
        Application.DoEvents();
        accounts.AccountList.Controls.OfType<AccountEditorRow>().First().RemoveButton.Visible = true;
        using var accountsImage = new Bitmap(accounts.Width, accounts.Height);
        accounts.DrawToBitmap(accountsImage, accounts.ClientRectangle);
        accountsImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "accounts-preview.png"));
        var draggedRow = accounts.AccountList.Controls.OfType<AccountEditorRow>().First();
        draggedRow.Dragging = true;
        using var dragImage = new Bitmap(accounts.Width, accounts.Height);
        accounts.DrawToBitmap(dragImage, accounts.ClientRectangle);
        dragImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "accounts-drag-preview.png"));
        draggedRow.Dragging = false;
        var nameEditor = accounts.AccountList.Controls.OfType<AccountEditorRow>().First().NameEditor;
        var hoverHandler = typeof(AccountNameEditor).GetMethod("OnMouseEnter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        hoverHandler.Invoke(nameEditor, [EventArgs.Empty]);
        using var nameHoverImage = new Bitmap(accounts.Width, accounts.Height);
        accounts.DrawToBitmap(nameHoverImage, accounts.ClientRectangle);
        nameHoverImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "accounts-name-hover-preview.png"));
        typeof(AccountNameEditor).GetMethod("OnMouseLeave", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(nameEditor, [EventArgs.Empty]);
        nameEditor.BeginEditing();
        using var editingImage = new Bitmap(accounts.Width, accounts.Height);
        accounts.DrawToBitmap(editingImage, accounts.ClientRectangle);
        editingImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "accounts-editing-preview.png"));
        nameEditor.FinishEditing(false);
        using var disconnected = new AccountsDialog(demo.Select((reading, index) => index == 1
            ? reading with { Error = "Sign-in missing", ConnectionIssue = ConnectionIssue.CredentialsMissing }
            : reading).ToList(), prefs, () => { }, persist: () => { });
        disconnected.Show();
        Application.DoEvents();
        using var disconnectedImage = new Bitmap(disconnected.Width, disconnected.Height);
        disconnected.DrawToBitmap(disconnectedImage, disconnected.ClientRectangle);
        disconnectedImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "accounts-disconnected-preview.png"));
        using var hover = new TrayHover();
        hover.UpdateEntries(demo, prefs);
        hover.Show();
        Application.DoEvents();
        using var hoverImage = new Bitmap(hover.Width, hover.Height);
        hover.DrawToBitmap(hoverImage, hover.ClientRectangle);
        hoverImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "hover-preview.png"));
        var detailReading = demo[2] with
        {
            Resets = new(1, [new(1, now.AddDays(3))]),
            Limits = [new("weekly", "Weekly", demo[2].Weekly!), new("session", "Five-hour", new(100, now.AddMinutes(4), TimeSpan.FromHours(5))), new("fable", "Fable · Weekly", new(12, now.AddDays(4), TimeSpan.FromDays(7)))]
        };
        using var detail = new AccountDetailsDialog(detailReading, prefs);
        detail.Show();
        Application.DoEvents();
        using var detailImage = new Bitmap(detail.Width, detail.Height);
        detail.DrawToBitmap(detailImage, detail.ClientRectangle);
        detailImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "account-detail-preview.png"));
        var resetlessReading = detailReading with
        {
            Limits = [new("weekly", "Weekly", detailReading.Weekly!), new("session", "Five-hour", new(0, null, TimeSpan.FromHours(5)))]
        };
        using var resetlessDetail = new AccountDetailsDialog(resetlessReading, prefs);
        resetlessDetail.Show();
        Application.DoEvents();
        using var resetlessImage = new Bitmap(resetlessDetail.Width, resetlessDetail.Height);
        resetlessDetail.DrawToBitmap(resetlessImage, resetlessDetail.ClientRectangle);
        resetlessImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "resetless-detail-preview.png"));
        using var onboarding = new UsagePanel();
        onboarding.UpdateRows([], new Settings(), false);
        onboarding.Show();
        Application.DoEvents();
        using var onboardingImage = new Bitmap(onboarding.Width, onboarding.Height);
        onboarding.DrawToBitmap(onboardingImage, onboarding.ClientRectangle);
        onboardingImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "onboarding-preview.png"));
    }
}
