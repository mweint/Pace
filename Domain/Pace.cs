using System.Text.Json;

namespace Usage;

public sealed record Pace(double Expected, double Points, TimeSpan? Ahead);
