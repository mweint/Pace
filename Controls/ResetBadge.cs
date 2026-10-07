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
        BackColor = Palette.Card;
        Size = new Size(42, 24);
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
        float scale = DeviceDpi / 96f;
        e.Graphics.ScaleTransform(scale, scale);
        var now = DateTimeOffset.UtcNow;
        Color ink = !stale && bank.ExpiringSoon(now) ? Palette.ResetWarning : Palette.Muted;
        IconButton.DrawArtwork(e.Graphics, artwork, new Rectangle(2, 6, 12, 12), ink);
        using var font = Palette.BodyFont();
        using var brush = new SolidBrush(ink);
        e.Graphics.DrawString(bank.Available(now).ToString(), font, brush, 19, 4);
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
