using Avalonia.Input.Platform;

namespace Pace;

// Theme-owned service action widths, spacing and toolbar inset.
internal sealed class AccountToolbar : PaintedPanel
{
    public TextBlock TrayCount { get; } = Palette.Label("");
    readonly FilledButton claude = Palette.Button("Add Claude"), codex = Palette.Button("Add Codex");
    readonly FilledButton copyLink = Palette.Button("Copy link"), cancel = Palette.Button("Cancel");
    string? link;
    public event Action<string>? AddRequested;
    public event Action? CancelRequested;
    public AccountToolbar()
    {
        Height = UiMetrics.AccountsToolbarHeight;
        claude.MinWidth = codex.MinWidth = copyLink.MinWidth = cancel.MinWidth = 0;
        claude.Width = codex.Width = copyLink.Width = cancel.Width = UiMetrics.AccountActionWidth;
        copyLink.IsVisible = cancel.IsVisible = false;
        Children.AddRange([TrayCount, claude, codex, copyLink, cancel]);
        claude.Click += (_, _) => AddRequested?.Invoke(Services.Claude);
        codex.Click += (_, _) => AddRequested?.Invoke(Services.Codex);
        cancel.Click += (_, _) => CancelRequested?.Invoke();
        copyLink.Click += async (_, _) => await CopyLink();
    }
    // While signing in, Cancel and (with a link from the CLI) Copy link replace the add actions.
    public void ShowSignIn(bool active, string? signInLink)
    {
        link = active ? signInLink : null;
        claude.IsVisible = codex.IsVisible = !active;
        cancel.IsVisible = active; copyLink.IsVisible = link != null;
    }
    async Task CopyLink()
    {
        if (link == null || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard) return;
        await clipboard.SetTextAsync(link);
        TrayCount.Text = "Link copied";
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(new Size(availableSize.Width, Height));
        return new(availableSize.Width, Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        double lastLeft = finalSize.Width - UiMetrics.OuterInset - UiMetrics.AccountActionWidth;
        double firstLeft = lastLeft - UiMetrics.CardGap - UiMetrics.AccountActionWidth;
        foreach (var (button, left) in new[] { (claude, firstLeft), (codex, lastLeft), (copyLink, firstLeft), (cancel, lastLeft) })
            button.Arrange(new Rect(left, UiMetrics.OuterInset, UiMetrics.AccountActionWidth, UiMetrics.TextButtonHeight));
        TrayCount.Arrange(new Rect(UiMetrics.ContentInset, UiMetrics.OuterInset + (UiMetrics.TextButtonHeight - Palette.TextHeight()) / 2,
            Math.Max(1, firstLeft - UiMetrics.ContentInset - UiMetrics.CardGap), Palette.TextHeight()));
        return finalSize;
    }
}
