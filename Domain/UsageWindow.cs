namespace Pace;

public sealed record UsageWindow(double Used, DateTimeOffset? Reset, TimeSpan Period);
