namespace Usage;
// Reset countdowns, hidden-limit warnings and refresh errors share a themed tooltip.
internal sealed class AccountDetailTip : IDisposable
{
    readonly Control owner;
    readonly ThemedToolTip tip = new();
    string text = "";
    public string Text
    {
        set
        {
            if (text == value)
                return;
            text = value;
            tip.SetToolTip(owner, value);
        }
    }

    public AccountDetailTip(Control owner) => this.owner = owner;
    public void Dispose() => tip.Dispose();
}
