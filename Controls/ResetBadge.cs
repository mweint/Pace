namespace Pace;

internal sealed class ResetBadge : Control
{
    BankedResets? bank;
    bool stale;
    public ResetBadge()
    {
        Width = UiMetrics.ResetBadgeWidth;
        Height = UiMetrics.ResetBadgeHeight;
    }
    public void UpdateBank(BankedResets? value, bool isStale)
    {
        bank = value; stale = isStale;
        var now = DateTimeOffset.UtcNow;
        IsVisible = bank?.Available(now) > 0;
        Avalonia.Automation.AutomationProperties.SetName(this, bank == null ? "" : $"{bank.Available(now)} banked resets");
        ThemedToolTip.Set(this, bank == null ? "" : bank.Details(now) + (stale ? "\nLast known reset information" : ""));
        InvalidateVisual();
    }
    public override void Render(DrawingContext context)
    {
        if (bank == null) return;
        var now = DateTimeOffset.UtcNow;
        var ink = !stale && bank.ExpiringSoon(now) ? Palette.Warning : Palette.Muted;
        IconArtwork.Draw(context, "refresh-cw", new Rect(UiMetrics.ResetBadgeIconInset,
            (UiMetrics.ResetBadgeHeight - UiMetrics.ResetBadgeIconSize) / 2d, UiMetrics.ResetBadgeIconSize, UiMetrics.ResetBadgeIconSize), ink);
        var text = Palette.Format(bank.Available(now).ToString(), ink);
        context.DrawText(text, new Point(UiMetrics.ResetBadgeIconInset + UiMetrics.ResetBadgeIconSize + UiMetrics.ResetBadgeTextGap,
            Math.Round((UiMetrics.ResetBadgeHeight - text.Height) / 2)));
    }
}
