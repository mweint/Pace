using System.IO.Pipes;

namespace Pace;

// One Pace per user (per session on Windows). A second launch asks the running copy to
// show its panel and exits. The channel carries a single command byte and nothing else.
internal sealed class SingleInstance : IDisposable
{
    const byte ShowCommand = 1;
    const int ConnectTimeoutMilliseconds = 2000, RetryMilliseconds = 1000;
    readonly Mutex mutex;
    readonly CancellationTokenSource stop = new();
    SingleInstance(Mutex mutex) => this.mutex = mutex;

    // Null when another copy is running; that copy has been asked to show itself.
    public static SingleInstance? Acquire()
    {
        bool created;
        // Unix sessions differ between autostart (systemd), menu launches and terminals,
        // so the Linux instance lock is per user rather than per session.
        var mutex = OperatingSystem.IsWindows() ? new Mutex(true, "Local\\Pace.TrayApp.V1", out created)
            : new Mutex(true, "Pace.TrayApp.V1", new NamedWaitHandleOptions { CurrentUserOnly = true, CurrentSessionOnly = false }, out created);
        if (created) return new(mutex);
        mutex.Dispose();
        DesktopIntegration.AllowOtherProcessActivation();
        Signal(ChannelName);
        return null;
    }

    public void Listen(Action show) => _ = Serve(ChannelName, show, stop.Token);

    // A pipe on Windows. Elsewhere .NET pipes are Unix sockets, and a rooted name is used as
    // the socket path, so it lives in the per-user runtime directory rather than shared /tmp.
    static string ChannelName => OperatingSystem.IsWindows()
        ? $"Pace.TrayApp.V1.{SessionId()}"
        : Path.Combine(Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") is { Length: > 0 } runtime && Directory.Exists(runtime) ? runtime : Settings.DirectoryPath, "pace.sock");

    static int SessionId()
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        return process.SessionId;
    }

    internal static async Task Serve(string name, Action show, CancellationToken token)
    {
        // Holding the lock means any existing socket file was left by a copy that crashed.
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(Path.GetDirectoryName(name)!);
            File.Delete(name);
        }
        var command = new byte[1];
        while (!token.IsCancellationRequested)
        {
            try
            {
                // One server for the app's lifetime: replacing it would close a Unix socket
                // along with any launch already queued on it.
                await using var server = new NamedPipeServerStream(name, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                while (true)
                {
                    await server.WaitForConnectionAsync(token);
                    try
                    {
                        if (await server.ReadAsync(command, token) == 1 && command[0] == ShowCommand) Dispatcher.UIThread.Post(show);
                    }
                    finally { server.Disconnect(); }
                }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                ErrorLog.Write(e);
                try { await Task.Delay(RetryMilliseconds, token); } catch (OperationCanceledException) { return; }
            }
        }
    }

    // The running copy may still be starting, so connecting waits briefly for its channel.
    internal static bool Signal(string name)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", name, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            client.Connect(ConnectTimeoutMilliseconds);
            client.WriteByte(ShowCommand);
            client.Flush();
            return true;
        }
        catch (Exception e) when (e is IOException or TimeoutException or UnauthorizedAccessException) { return false; }
    }

    public void Dispose()
    {
        stop.Cancel();
        mutex.Dispose();
    }
}
