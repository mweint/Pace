using System.Diagnostics;
using System.IO.Compression;

namespace Pace;

internal static class UpdateInstaller
{
    public static string DirectoryPath => Path.Combine(Settings.DirectoryPath, "updates");
    static string Marker => Path.Combine(DirectoryPath, "ready.txt");
    public static string? ReadyVersion
    {
        get { try { return File.ReadAllText(Marker).Trim(); } catch { return null; } }
    }

    public static void Stage(string zip, string version)
    {
        File.Delete(Marker);
        string payload = Path.Combine(DirectoryPath, "payload");
        if (Directory.Exists(payload)) Directory.Delete(payload, true);
        ZipFile.ExtractToDirectory(zip, payload);
        var executables = Directory.GetFiles(payload, "Pace.exe", SearchOption.AllDirectories);
        if (executables.Length != 1) throw new InvalidDataException("Invalid Pace package");
        var info = FileVersionInfo.GetVersionInfo(executables[0]);
        if (!Version.TryParse(version.TrimStart('v'), out var expected) ||
            !Version.TryParse(info.FileVersion, out var actual) || expected.Major != actual.Major || expected.Minor != actual.Minor || expected.Build != actual.Build)
            throw new InvalidDataException("Package version mismatch");
        File.WriteAllText(Marker, version);
    }

    public static bool Apply()
    {
        try { return StartInstall(); }
        catch { return false; }
    }

    static bool StartInstall()
    {
        if (!OperatingSystem.IsWindows()) return false;
        if (ReadyVersion is not { } version || !AppUpdates.IsNewer(version, AppUpdates.CurrentVersion)) return false;
        string payload = Path.Combine(DirectoryPath, "payload");
        if (!Directory.Exists(payload)) return false;
        var executables = Directory.GetFiles(payload, "Pace.exe", SearchOption.AllDirectories);
        if (executables.Length != 1) return false;
        string script = Path.Combine(DirectoryPath, "install.ps1");
        using var resource = typeof(UpdateInstaller).Assembly.GetManifestResourceStream("Pace.Install-Update.ps1")!;
        using (var output = File.Create(script)) resource.CopyTo(output);
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden };
        foreach (string argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script, "-Source", Path.GetDirectoryName(executables[0])!, "-Destination", AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), "-ProcessId", Environment.ProcessId.ToString(), "-Marker", Marker })
            start.ArgumentList.Add(argument);
        Process.Start(start);
        return true;
    }
}
