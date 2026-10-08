namespace Usage;

public sealed class EmptyAccountsView : Panel
{
    readonly Button claude = Palette.ServiceButton("Claude");
    readonly Button codex = Palette.ServiceButton("Codex");
    readonly Label status;
    bool signingIn;
    public bool HasAccounts { get; }
    public event Action<string>? AddRequested;
    public event Action? ManageRequested;

    public EmptyAccountsView(bool hasAccounts)
    {
        HasAccounts = hasAccounts;
        BackColor = Palette.Background;
        Font = Palette.BodyFont();
        Height = UiMetrics.EmptyAccountsHeight;
        Margin = Padding.Empty;
        var title = TextLabel(hasAccounts ? "No accounts shown" : "Add an account", 4, 26, Palette.Text);
        title.Font = Palette.AccountFont();
        var explanation = TextLabel(hasAccounts
            ? "Choose which accounts appear here in Accounts."
            : "Sign in through your browser.", 34, 22, Palette.Muted);
        var requirement = TextLabel(hasAccounts ? "" : "Requires Claude Code, or Codex desktop/CLI.", 58, 24, Palette.Muted);
        status = TextLabel("", UiMetrics.EmptyAccountsHeight, 44, Palette.Muted);
        int buttonWidth = (UiMetrics.ContentWidth - UiMetrics.CardGap) / 2;
        claude.SetBounds(0, 88, buttonWidth, UiMetrics.TextButtonHeight);
        codex.SetBounds(buttonWidth + UiMetrics.CardGap, 88, buttonWidth, UiMetrics.TextButtonHeight);
        claude.Click += (_, _) => AddRequested?.Invoke("Claude");
        codex.Click += (_, _) => AddRequested?.Invoke("Codex");
        Controls.AddRange([title, explanation, requirement, status]);
        if (hasAccounts)
        {
            var manage = Palette.Button("Manage accounts");
            manage.SetBounds(0, 88, 144, UiMetrics.TextButtonHeight);
            manage.Click += (_, _) => ManageRequested?.Invoke();
            Controls.Add(manage);
        }
        else
            Controls.AddRange([claude, codex]);
    }

    Label TextLabel(string text, int top, int height, Color color) => new()
    {
        Text = text, Left = 0, Top = top, Width = UiMetrics.ContentWidth,
        Height = height, Font = Palette.BodyFont(), ForeColor = color,
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
    };

    public void UpdateLoading(bool loading)
    {
        claude.Enabled = codex.Enabled = !loading && !signingIn;
        if (!signingIn && string.IsNullOrEmpty(status.Text))
            status.Text = loading ? "Looking for accounts…" : "";
        else if (!signingIn && status.Text == "Looking for accounts…" && !loading)
            status.Text = "";
        Height = UiMetrics.EmptyAccountsHeight + (string.IsNullOrEmpty(status.Text) ? 0 : status.Height + 6);
    }

    public void UpdateSignIn(bool pending, string message)
    {
        signingIn = pending;
        status.Text = message;
        UpdateLoading(false);
    }
}
