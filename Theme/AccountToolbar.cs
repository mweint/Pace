namespace Pace;

// Theme-owned service action widths, spacing and toolbar inset.
internal sealed class AccountToolbar : PaintedPanel
{
    public TextBlock TrayCount { get; } = Palette.Label("");
    readonly FilledButton claude = Palette.Button("Add Claude"), codex = Palette.Button("Add Codex");
    public event Action<string>? AddRequested;
    public AccountToolbar()
    {
        Height = UiMetrics.AccountsToolbarHeight;
        claude.MinWidth = codex.MinWidth = 0;
        claude.Width = codex.Width = UiMetrics.AccountActionWidth;
        Children.AddRange([TrayCount, claude, codex]);
        claude.Click += (_, _) => AddRequested?.Invoke(Services.Claude);
        codex.Click += (_, _) => AddRequested?.Invoke(Services.Codex);
    }
    public void SetBusy(bool busy) => claude.IsEnabled = codex.IsEnabled = !busy;
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(new Size(availableSize.Width, Height));
        return new(availableSize.Width, Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        double lastLeft = finalSize.Width - UiMetrics.OuterInset - UiMetrics.AccountActionWidth;
        double firstLeft = lastLeft - UiMetrics.CardGap - UiMetrics.AccountActionWidth;
        claude.Arrange(new Rect(firstLeft, UiMetrics.OuterInset, UiMetrics.AccountActionWidth, UiMetrics.TextButtonHeight));
        codex.Arrange(new Rect(lastLeft, UiMetrics.OuterInset, UiMetrics.AccountActionWidth, UiMetrics.TextButtonHeight));
        TrayCount.Arrange(new Rect(UiMetrics.ContentInset, UiMetrics.OuterInset + (UiMetrics.TextButtonHeight - Palette.TextHeight()) / 2,
            Math.Max(1, firstLeft - UiMetrics.ContentInset - UiMetrics.CardGap), Palette.TextHeight()));
        return finalSize;
    }
}
