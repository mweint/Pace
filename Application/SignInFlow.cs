namespace Pace;

public readonly record struct SignInResult(bool Succeeded, string Message);

// One browser sign-in at a time, shared by the overview and Settings so either can cancel it.
public sealed class SignInFlow
{
    CancellationTokenSource? cancel;
    public bool Active => cancel != null;
    // The CLI's sign-in link, when it printed one (outside Windows).
    public string? Link { get; private set; }
    public event Action? Changed;

    // A cancelled sign-in fails with an empty message.
    public async Task<SignInResult> Run(string service, Settings settings, string? existingFile = null)
    {
        if (Active) return new(false, "");
        cancel = new(); Link = null; Changed?.Invoke();
        try
        {
            await SignIn.Begin(service, settings, existingFile, link => { if (Active) { Link = link; Changed?.Invoke(); } }, cancel.Token);
            return new(true, "");
        }
        catch (OperationCanceledException) { return new(false, ""); }
        catch (Exception e) { return new(false, e is InvalidOperationException ? e.Message : "Sign-in failed. Please try again."); }
        finally
        {
            cancel.Dispose(); cancel = null; Link = null;
            Changed?.Invoke();
        }
    }

    public void Cancel() => cancel?.Cancel();
}
