using System.Text.Json;
using System.Text.Json.Nodes;

namespace Usage;

internal static class ThemeChecks
{
    public static void Run(Action<bool, string> Check)
    {
        using var bodyFont = Palette.BodyFont();
        using var accountFont = Palette.AccountFont();
        Check(bodyFont.Name == "Pace Sans" && accountFont.Name == "Pace Sans SemiBold", "Packaged theme fonts load without a system-font fallback");
        using var typographyHover = new TrayHover();
        Check(typographyHover.Font.Name == accountFont.Name && typographyHover.Font.Size == accountFont.Size, "Tray hover account names use the shared account typography");
        using var themedTip = new ThemedToolTip();
        Check(themedTip.OwnerDraw, "Supplemental tooltips render through the shared theme instead of Windows default typography");
        using var iconButton = new IconButton("refresh", "Refresh");
        using var filledButton = Palette.Button("Refresh");
        Check(iconButton.Font.Name == bodyFont.Name && filledButton.Font.Name == bodyFont.Name &&
            iconButton.Font.Size == bodyFont.Size && filledButton.Font.Size == bodyFont.Size,
            "Icon and filled buttons use theme typography while retaining their distinct styles");
        filledButton.Size = new Size(100, UiMetrics.TextButtonHeight);
        filledButton.Enabled = false;
        using var disabledImage = new Bitmap(filledButton.Width, filledButton.Height);
        filledButton.DrawToBitmap(disabledImage, filledButton.ClientRectangle);
        Check(disabledImage.GetPixel(1, 1).ToArgb() == Palette.Action.ToArgb(),
            "Disabled filled buttons retain the themed surface instead of native Windows rendering");
        Check(Palette.Warning == Palette.Above,
            "Warnings and above-pace status share the same attention color");
        var reading = new Reading(new("theme", "Claude", "Demo", ""), null, null, DateTimeOffset.UtcNow);
        using var row = new AccountRow(reading, "Demo");
        using var editor = new AccountEditorRow(reading, new Preference { Key = "theme" }, () => { });
        Check(row.BackColor == Palette.SectionBackground && editor.BackColor == row.BackColor && editor.NameEditor.BackColor == row.BackColor && editor.NameInput.BackColor == row.BackColor && editor.PanelToggle.BackColor == row.BackColor,
            "Overview, details, account editors and their embedded fields share the same section surface");
        using var sections = new SectionList { Width = UiMetrics.PanelWidth };
        sections.Controls.AddRange([row, editor]);
        sections.PerformLayout();
        Check(!row.ShowSeparator && editor.ShowSeparator && row.Margin == Padding.Empty && editor.Margin == Padding.Empty,
            "The shared section list owns dividers and spacing for both account controls");
        sections.Controls.SetChildIndex(editor, 0);
        sections.PerformLayout();
        Check(!editor.ShowSeparator && row.ShowSeparator, "Section dividers follow reordering automatically");
        using var information = new InfoSection();
        information.Add("Banked resets · 0", Palette.Text);
        information.Add("Updated just now", Palette.Muted);
        sections.Controls.Add(information);
        sections.PerformLayout();
        var informationLabels = information.Controls.OfType<Label>().ToList();
        Check(information.ShowSeparator && informationLabels[0].Top == UiMetrics.ContentInset &&
            informationLabels.All(label => label.Left == UiMetrics.ContentInset && label.Font.Name == bodyFont.Name) &&
            information.Height - informationLabels[^1].Bottom == UiMetrics.ContentInset,
            "Account information has a theme-owned divider, typography and equal section insets");
    }
}
