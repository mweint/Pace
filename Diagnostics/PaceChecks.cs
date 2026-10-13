using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pace;

internal static class PaceChecks
{
    public static void Run(Action<bool, string> Check, DateTimeOffset now)
    {
        var p = PaceMath.Calculate(new(57, now.AddDays(3.5), TimeSpan.FromDays(7)), now)!;
        Check(Math.Abs(p.Points - 7) < .0001 && p.Expected == 50, "Uses percentage-point difference, not relative forecast");
        var other = PaceMath.Calculate(new(57, now.AddDays(1.75), TimeSpan.FromDays(7)), now)!;
        Check(other.Expected == 75 && other.Points == -18, "Each account has its own reset baseline");
        Check(PaceMath.Compact(p) == "7% above" && PaceMath.Compact(other) == "18% below", "Compact percent labels preserve the baseline difference");
        Check(Math.Abs(p.Ahead!.Value.TotalHours - 11.76) < .0001, "Ahead hours measure the current gap on the seven-day baseline");
        Check(other.Ahead == null, "Below pace does not show hours ahead");
        // Halfway through a week, ahead begins past max(2, 5% of the remaining 50) = 2.5 points.
        var marker = PaceMath.Calculate(new(50, now.AddDays(3.5), TimeSpan.FromDays(7)), now)!;
        var slightly = PaceMath.Calculate(new(52, now.AddDays(3.5), TimeSpan.FromDays(7)), now)!;
        var upperEdge = PaceMath.Calculate(new(52.5, now.AddDays(3.5), TimeSpan.FromDays(7)), now)!;
        var justAbove = PaceMath.Calculate(new(52.6, now.AddDays(3.5), TimeSpan.FromDays(7)), now)!;
        Check(PaceMath.Classify(marker) == PaceState.Below && PaceMath.Compact(marker) == "on pace", "Usage at the marker is green and reads on pace");
        Check(PaceMath.Classify(slightly) == PaceState.OnPace && PaceMath.Classify(upperEdge) == PaceState.OnPace && PaceMath.Classify(justAbove) == PaceState.Above,
            "Ahead begins past the larger of the floor and the slowdown share of the remaining window");
        var fresh = PaceMath.Calculate(new(4.9, now.AddDays(7), TimeSpan.FromDays(7)), now)!;
        var late = PaceMath.Calculate(new(92.1, now.AddDays(.7), TimeSpan.FromDays(7)), now)!;
        Check(PaceMath.Classify(fresh) == PaceState.OnPace && PaceMath.Classify(late) == PaceState.Above, "The ahead threshold is wider after a reset and floors near the next one");
        Check(PaceMath.Compact(slightly) == "2% above" && PaceMath.Compact(justAbove) == "3% above", "Values off the marker show above or below");
        Check(Palette.Status(marker) == Palette.Below && Palette.Status(slightly) == Palette.OnPace && Palette.Status(justAbove) == Palette.Above, "Text and colors agree at the boundaries");
        Check(PaceMath.AheadLabel(upperEdge) == null && PaceMath.AheadLabel(justAbove) == "~4h ahead", "Approximate ahead hours appear only when usage is ahead");
        Check(PaceMath.HoverSummary(p) == "+7% · 12h" && PaceMath.HoverSummary(other) == "-18%" && PaceMath.HoverSummary(slightly) == "+2%", "Hover adds whole hours only above pace, without an ahead suffix");
        Check(PaceMath.Calculate(new(50, now, TimeSpan.FromDays(7)), now) == null, "Expired windows have no current pace");
        Check(PaceMath.Calculate(new(50, now.AddDays(8), TimeSpan.FromDays(7)), now) == null, "Future-start windows have no current pace");
        Check(PaceMath.Classify(new UsageWindow(0, now.AddDays(7).AddSeconds(1), TimeSpan.FromDays(7)), now) == PaceState.Below,
            "An unused window starting a moment ahead of the local clock counts as just started");
        var exhausted = new UsageWindow(100, now.AddMinutes(4), TimeSpan.FromHours(5));
        Check(PaceMath.Classify(PaceMath.Calculate(exhausted, now)) == PaceState.OnPace &&
            PaceMath.Classify(exhausted, now) == PaceState.Exhausted &&
            PaceMath.Compact(exhausted, now) == "limit reached" && Palette.Status(exhausted, now) == Palette.Warning,
            "Exhausted limits override on-pace tolerance with the warning label and color");
        Check(PaceMath.Classify(exhausted, exhausted.Reset!.Value) == PaceState.Unavailable &&
            PaceMath.Classify(exhausted with { Used = 99 }, now) == PaceState.OnPace,
            "Exhaustion requires 100 percent usage in an active window");
        var clockNow = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
        var sameDayReset = clockNow.AddHours(5.5);
        Check(!PaceMath.ResetLabel(sameDayReset, clockNow, TimeZoneInfo.Utc).Contains("·") &&
            PaceMath.ResetLabel(sameDayReset.AddDays(1), clockNow, TimeZoneInfo.Utc).Contains("·"),
            "Reset labels omit the weekday only for the same local calendar day");
        Check(PaceMath.ResetLabel(clockNow.AddMinutes(30), clockNow, TimeZoneInfo.CreateCustomTimeZone("test", TimeSpan.FromHours(-12.25), "test", "test")).Contains("·"),
            "Reset labels use local calendar boundaries even when UTC dates match");
        Check(PaceMath.ResetCountdown(sameDayReset, clockNow) == "Resets in 5h 30m" &&
            PaceMath.ResetLabel(null, clockNow) == "Reset time unavailable" &&
            PaceMath.ResetLabel(clockNow, clockNow) == "Reset passed · refresh needed",
            "Countdown tooltips and unknown or passed reset states remain explicit");
    }
}
