namespace Usage;

internal sealed class ResetBadge : Control
{
    readonly Bitmap artwork = IconButton.LoadArtwork("refresh-cw");
    readonly AccountDetailTip tip;
    BankedResets? bank;
    bool stale;
    public ResetBadge()
    {
        DoubleBuffered = true;
        BackColor = Palette.SectionBackground;
        Size = new Size(UiMetrics.ResetBadgeWidth, UiMetrics.ResetBadgeHeight);
        tip = new AccountDetailTip(this);
    }

    public void UpdateBank(BankedResets? value, bool stale)
    {
        bank = value;
        this.stale = stale;
        var now = DateTimeOffset.UtcNow;
        Visible = bank?.Available(now) > 0;
        AccessibleName = bank == null ? "" : $"{bank.Available(now)} banked resets";
        tip.Text = bank == null ? "" : bank.Details(now) + (stale ? "\nLast known reset information" : "");
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (bank == null)
            return;
        float scale = DeviceDpi / (float)UiMetrics.BaseDpi;
        e.Graphics.ScaleTransform(scale, scale);
        var now = DateTimeOffset.UtcNow;
        Color ink = !stale && bank.ExpiringSoon(now) ? Palette.Warning : Palette.Muted;
        IconButton.DrawArtwork(e.Graphics, artwork, new Rectangle(UiMetrics.ResetBadgeIconInset,
            (UiMetrics.ResetBadgeHeight - UiMetrics.ResetBadgeIconSize) / 2, UiMetrics.ResetBadgeIconSize, UiMetrics.ResetBadgeIconSize), ink);
        using var font = Palette.BodyFont();
        using var brush = new SolidBrush(ink);
        e.Graphics.DrawString(bank.Available(now).ToString(), font, brush,
            UiMetrics.ResetBadgeIconInset + UiMetrics.ResetBadgeIconSize + UiMetrics.ResetBadgeTextGap,
            (UiMetrics.ResetBadgeHeight - font.GetHeight(UiMetrics.BaseDpi)) / 2);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            artwork.Dispose();
            tip.Dispose();
        }

        base.Dispose(disposing);
    }
}
