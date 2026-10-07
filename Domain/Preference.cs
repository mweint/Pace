using System.Text.Json;

namespace Usage;

public sealed class Preference
{
    public string Key { get; set; } = "";
    public string Alias { get; set; } = "";
    public bool Show { get; set; } = true;
    public bool Tray { get; set; } = true;
}
