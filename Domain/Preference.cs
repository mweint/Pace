using System.Text.Json;

namespace Usage;

public sealed class Preference
{
    public string Key { get; set; } = "";
    public string Alias { get; set; } = "";
    // Identity and path only: credentials remain in the official client's files.
    public Account? RememberedAccount { get; set; }
    public bool Show { get; set; } = true;
    public bool Tray { get; set; } = true;
    public bool ShowFiveHour { get; set; } = true;
    public bool ShowFable { get; set; } = true;
}
