using System.Diagnostics;

namespace Pace;

// Second launches, browser sign-in and the X11 pointer, without a real CLI or a second Pace.
internal static class LaunchChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        await InstanceChannel(check);
        SignInLinks(check);
        await SignInCancel(check);
        if (DesktopIntegration.IsX11Session) check(DesktopIntegration.Pointer != null, "X11 reports the pointer that places the panel after a tray click");
    }

    static async Task InstanceChannel(Action<bool, string> check)
    {
        string id = Guid.NewGuid().ToString("N");
        string name = OperatingSystem.IsWindows() ? "Pace.SelfTest." + id : Path.Combine(Path.GetTempPath(), "pace-" + id[..8] + ".sock");
        using var stop = new CancellationTokenSource();
        int shown = 0;
        var serving = SingleInstance.Serve(name, () => shown++, stop.Token);
        bool sent = await Task.Run(() => SingleInstance.Signal(name) && SingleInstance.Signal(name));
        for (int i = 0; i < 50 && shown < 2; i++) await Task.Delay(20);
        check(sent && shown == 2, "A second launch asks the running copy to show its panel, more than once");
        stop.Cancel(); await serving;
        check(!await Task.Run(() => SingleInstance.Signal(name)), "Signalling with no running copy gives up after a short wait");
    }

    static void SignInLinks(Action<bool, string> check)
    {
        check(SignIn.SignInLink("Starting local login server on http://localhost:1455.") == null
            && SignIn.SignInLink("https://auth.openai.com/oauth/authorize?client_id=x&state=y") == "https://auth.openai.com/oauth/authorize?client_id=x&state=y"
            && SignIn.SignInLink("\u001b[2mIf the browser didn't open, visit: https://claude.ai/oauth/authorize?code=true&x=1\u001b[0m") == "https://claude.ai/oauth/authorize?code=true&x=1"
            && SignIn.SignInLink("\u001b]8;;https://claude.com/cai/oauth/authorize?a=b\u001b\\link\u001b]8;;\u001b\\") == "https://claude.com/cai/oauth/authorize?a=b"
            && SignIn.SignInLink("see https://example.com/claude.ai/oauth or https://notclaude.ai/x") == null,
            "Only a sign-in link to the service is taken from CLI output");
    }

    static async Task SignInCancel(Action<bool, string> check)
    {
        const string url = "https://auth.openai.com/oauth/authorize?selftest=1";
        var start = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("powershell.exe") { ArgumentList = { "-NoProfile", "-NonInteractive", "-Command", $"Write-Output 'Visit {url} to continue'; Start-Sleep 60" } }
            : new ProcessStartInfo("/bin/sh") { ArgumentList = { "-c", $"echo 'Visit {url} to continue'; exec sleep 60" } };
        start.UseShellExecute = false; start.CreateNoWindow = true;
        start.RedirectStandardOutput = start.RedirectStandardError = true;
        using var cancel = new CancellationTokenSource();
        var link = new TaskCompletionSource<string>();
        var running = SignIn.Run(start, found => link.TrySetResult(found), cancel.Token);
        bool reported = await Task.WhenAny(link.Task, Task.Delay(10_000)) == link.Task && link.Task.Result == url;
        var watch = Stopwatch.StartNew();
        cancel.Cancel();
        bool cancelled = false;
        try { await running; }
        catch (OperationCanceledException) { cancelled = true; }
        check(reported && cancelled && watch.ElapsedMilliseconds < AppTiming.SignInStopGraceMilliseconds + 2000,
            "Cancelling sign-in stops the CLI promptly after its link is reported");
    }
}
