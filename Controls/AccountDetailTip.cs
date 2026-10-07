namespace Usage;
// Only refresh errors need supplemental detail; identity is visible in Accounts.
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
