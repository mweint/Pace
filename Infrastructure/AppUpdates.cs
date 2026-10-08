using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

namespace Pace;

public sealed record AppRelease(string Version, Uri Download, string Digest);

public sealed class AppUpdates
{
    const string Api = "https://api.github.com/repos/mweint/Pace/releases/latest";
    static readonly HttpClient network = CreateClient();
    readonly HttpClient client;
    readonly Action persist;
    readonly Settings settings;
    bool busy;
    public event Action? Changed;
    public AppRelease? Available { get; private set; }
    public string Status { get; private set; } = "";
    public bool Busy => busy;
    public bool Notify => Available is { } release && settings.DismissedUpdateVersion != release.Version;
    public bool Ready => Available is { } release && UpdateInstaller.ReadyVersion == release.Version;
    public static string CurrentVersion => typeof(AppUpdates).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
    public AppUpdates(Settings settings) : this(settings, network, settings.Save) { }
    internal AppUpdates(Settings settings, HttpClient client, Action persist)
    {
        this.settings = settings;
        this.client = client;
        this.persist = persist;
    }

    static HttpClient CreateClient()
    {
        var result = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        result.DefaultRequestHeaders.UserAgent.ParseAdd("Pace/" + CurrentVersion);
        result.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return result;
    }

    public static bool IsNewer(string release, string current) =>
        Version.TryParse(release.TrimStart('v'), out var candidate) && Version.TryParse(current.TrimStart('v'), out var installed) && candidate > installed;

    public async Task Check()
    {
        if (busy) return;
        busy = true;
        Status = "Checking for updates…";
        Changed?.Invoke();
        try
        {
            using var response = await client.GetAsync(Api);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Status = "No published release yet.";
                return;
            }
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;
            string version = root.GetProperty("tag_name").GetString() ?? "";
            if (root.GetProperty("prerelease").GetBoolean() || root.GetProperty("draft").GetBoolean() || !IsNewer(version, CurrentVersion))
            {
                Available = null;
                Status = "You're up to date.";
                return;
            }
            Available = null;
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                if (!OperatingSystem.IsWindows() || asset.GetProperty("name").GetString() != "Pace-win-x64.zip") continue;
                var url = new Uri(asset.GetProperty("browser_download_url").GetString()!);
                string digest = asset.TryGetProperty("digest", out var value) ? value.GetString() ?? "" : "";
                if (url.Scheme != "https" || url.Host != "github.com" || !url.AbsolutePath.StartsWith("/mweint/Pace/releases/download/", StringComparison.Ordinal) || !digest.StartsWith("sha256:", StringComparison.Ordinal))
                    throw new InvalidDataException();
                Available = new(version, url, digest[7..]);
                break;
            }
            Status = Available == null ? "No compatible update package." : $"Version {version.TrimStart('v')} available.";
        }
        catch { Status = "Couldn't check for updates. Try again."; }
        finally { busy = false; Changed?.Invoke(); }
        if (settings.AutomaticUpdates && Available != null) await Download();
    }

    public void Dismiss()
    {
        if (Available == null) return;
        settings.DismissedUpdateVersion = Available.Version;
        persist();
        Changed?.Invoke();
    }

    public bool Install()
    {
        if (UpdateInstaller.Apply()) return true;
        Status = "Couldn't start the update. Try again.";
        Changed?.Invoke();
        return false;
    }

    public async Task Download()
    {
        if (busy || Available is not { } release || Ready) return;
        busy = true;
        Status = "Downloading update…";
        Changed?.Invoke();
        string zip = Path.Combine(UpdateInstaller.DirectoryPath, "download.zip");
        try
        {
            Directory.CreateDirectory(UpdateInstaller.DirectoryPath);
            using (var response = await client.GetAsync(release.Download, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using var output = File.Create(zip);
                await response.Content.CopyToAsync(output);
            }
            await using (var input = File.OpenRead(zip))
            {
                string actual = Convert.ToHexString(await SHA256.HashDataAsync(input));
                if (!actual.Equals(release.Digest, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException();
            }
            UpdateInstaller.Stage(zip, release.Version);
            Status = "Ready to install. Update now or on the next launch.";
        }
        catch { Status = "Couldn't prepare the update. Try again."; }
        finally { if (File.Exists(zip)) File.Delete(zip); busy = false; Changed?.Invoke(); }
    }
}
