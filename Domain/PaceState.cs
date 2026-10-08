using System.Text.Json;

namespace Usage;

public enum PaceState
{
    Unavailable,
    OnPace,
    Above,
    Below,
    Exhausted
}
