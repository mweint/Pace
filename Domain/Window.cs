using System.Text.Json;

namespace Usage;

public sealed record Window(double Used, DateTimeOffset Reset, TimeSpan Period);
