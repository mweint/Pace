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
        using var panel = new UsagePanel();
        var prefs = new Settings();
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
        accounts.Controls.OfType<AnimatedAccountList>().Single().Controls.OfType<AccountEditorRow>().First().RemoveButton.Visible = true;
        using var accountsImage = new Bitmap(accounts.Width, accounts.Height);
        accounts.DrawToBitmap(accountsImage, accounts.ClientRectangle);
        accountsImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "accounts-preview.png"));
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
            Limits = [new("weekly", "Weekly", demo[2].Weekly!), new("session", "Five-hour", new(27, now.AddHours(2), TimeSpan.FromHours(5))), new("fable", "Fable · Weekly", new(12, now.AddDays(4), TimeSpan.FromDays(7)))]
        };
        using var detail = new AccountDetailsDialog(detailReading, prefs);
        detail.Show();
        Application.DoEvents();
        using var detailImage = new Bitmap(detail.Width, detail.Height);
        detail.DrawToBitmap(detailImage, detail.ClientRectangle);
        detailImage.Save(Path.Combine(Path.GetDirectoryName(path)!, "account-detail-preview.png"));
    }
}
