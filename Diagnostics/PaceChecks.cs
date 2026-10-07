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
    }
}
