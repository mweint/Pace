namespace Pace;

internal sealed class EmptyAccountsView : PaintedPanel
{
    readonly TextBlock title, explanation, requirement, status;
    readonly FilledButton claude = Palette.ServiceButton(Services.Claude), codex = Palette.ServiceButton(Services.Codex);
    readonly FilledButton? manage;
    bool signingIn;
    public bool HasAccounts { get; }
    public event Action<string>? AddRequested;
    public event Action? ManageRequested;
    public EmptyAccountsView(bool hasAccounts)
    {
        HasAccounts = hasAccounts;
        title = Palette.Label(hasAccounts ? "No accounts shown" : "Add an account", true, Palette.Text, Palette.AccountSize);
        explanation = Palette.Label(hasAccounts ? "Choose which accounts appear here in Accounts." : "Sign in through your browser.");
        requirement = Palette.Label(hasAccounts ? "" : "Requires Claude Code, or Codex desktop/CLI.");
        status = Palette.Label("");
        status.TextWrapping = TextWrapping.Wrap;
        Children.AddRange([title, explanation, requirement, status]);
        if (hasAccounts)
        {
            manage = Palette.Button("Manage accounts");
            manage.Click += (_, _) => ManageRequested?.Invoke();
            Children.Add(manage);
        }
        else
        {
            claude.MinWidth = codex.MinWidth = 0;
            claude.Click += (_, _) => AddRequested?.Invoke(Services.Claude);
            codex.Click += (_, _) => AddRequested?.Invoke(Services.Codex);
            Children.AddRange([claude, codex]);
        }
        // The empty state sits inside the overview's outer inset.
        Margin = new Thickness(UiMetrics.OuterInset, 0);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        return new(availableSize.Width, Layout(availableSize.Width, arrange: false));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        Layout(finalSize.Width, arrange: true);
        return finalSize;
    }
    // Text rows stack with inline gaps inside the outer inset; status follows the actions.
    double Layout(double width, bool arrange)
    {
        double y = UiMetrics.OuterInset;
        void Row(Control control, double height, double left = 0, double? rowWidth = null)
        {
            if (arrange) control.Arrange(new Rect(left, y, rowWidth ?? width, height));
        }
        foreach (var label in new[] { title, explanation, requirement }.Where(l => !string.IsNullOrEmpty(l.Text)))
        {
            Row(label, label.DesiredSize.Height);
            y += label.DesiredSize.Height + UiMetrics.InlineGap;
        }
        y += UiMetrics.InlineGap;
        double half = (width - UiMetrics.CardGap) / 2;
        if (manage != null) Row(manage, UiMetrics.TextButtonHeight, 0, manage.MinWidth);
        else
        {
            Row(claude, UiMetrics.TextButtonHeight, 0, half);
            Row(codex, UiMetrics.TextButtonHeight, half + UiMetrics.CardGap, half);
        }
        y += UiMetrics.TextButtonHeight;
        if (!string.IsNullOrEmpty(status.Text))
        {
            y += UiMetrics.CardGap;
            Row(status, status.DesiredSize.Height);
            y += status.DesiredSize.Height;
        }
        y += UiMetrics.OuterInset;
        return Math.Ceiling(y);
    }
    public void UpdateLoading(bool loading)
    {
        claude.IsEnabled = codex.IsEnabled = !loading && !signingIn;
        if (!signingIn && string.IsNullOrEmpty(status.Text)) status.Text = loading ? "Looking for accounts…" : "";
        else if (!signingIn && status.Text == "Looking for accounts…" && !loading) status.Text = "";
        InvalidateMeasure();
    }
    public void UpdateSignIn(bool pending, string message)
    {
        signingIn = pending; status.Text = message; UpdateLoading(false);
    }
}
