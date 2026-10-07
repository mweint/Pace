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
    }
}
