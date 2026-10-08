using System.Text.Json;
using System.Text.Json.Nodes;

namespace Usage;

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
        var lowerEdge = PaceMath.Calculate(new(46, now.AddDays(3.5), TimeSpan.FromDays(7)), now)!;
        var upperEdge = PaceMath.Calculate(new(52, now.AddDays(3.5), TimeSpan.FromDays(7)), now)!;
        var justBelow = PaceMath.Calculate(new(45.9, now.AddDays(3.5), TimeSpan.FromDays(7)), now)!;
        var justAbove = PaceMath.Calculate(new(52.1, now.AddDays(3.5), TimeSpan.FromDays(7)), now)!;
        Check(PaceMath.Compact(lowerEdge) == "on pace" && PaceMath.Compact(upperEdge) == "on pace", "Negative four and positive two are inclusive on-pace boundaries");
        Check(PaceMath.Compact(justBelow) == "4% below" && PaceMath.Compact(justAbove) == "2% above", "Values outside the asymmetric band show below or above");
        Check(Palette.Status(lowerEdge) == Palette.OnPace && Palette.Status(upperEdge) == Palette.OnPace && Palette.Status(justBelow) == Palette.Below && Palette.Status(justAbove) == Palette.Above, "Text and colors agree at both asymmetric boundaries");
        Check(PaceMath.AheadLabel(upperEdge) == null && PaceMath.AheadLabel(justAbove) == "~4h ahead", "Approximate ahead hours appear only when usage exceeds the upper tolerance");
        Check(PaceMath.HoverSummary(p) == "+7% · 12h" && PaceMath.HoverSummary(other) == "-18%" && PaceMath.HoverSummary(upperEdge) == "+2%", "Hover adds whole hours only above pace, without an ahead suffix");
        Check(PaceMath.Calculate(new(50, now, TimeSpan.FromDays(7)), now) == null, "Expired windows have no current pace");
        Check(PaceMath.Calculate(new(50, now.AddDays(8), TimeSpan.FromDays(7)), now) == null, "Future-start windows have no current pace");
        var exhausted = new Window(100, now.AddMinutes(4), TimeSpan.FromHours(5));
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
