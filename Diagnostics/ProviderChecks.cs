using System.Text.Json;
using System.Text.Json.Nodes;

namespace Usage;

internal static class ProviderChecks
{
    public static List<UsageLimit> Run(Action<bool, string> Check, DateTimeOffset now)
    {
        var resetCredits = JsonNode.Parse("""{"available_count":2,"credits":[{"status":"available","expires_at":"2026-10-07T12:00:00Z"},{"status":"available","expires_at":"2026-10-20T12:00:00Z"},{"status":"redeemed","expires_at":"2026-10-10T12:00:00Z"}]}""")!.AsObject();
        var bank = Providers.ParseCodexResets(resetCredits, now)!;
        Check(bank.Available(now) == 2 && bank.Grants!.Count == 2 && bank.ExpiringSoon(now), "Saved reset count and nearest expiry ignore redeemed credits");
        Check(bank.Available(now.AddDays(2)) == 1 && !bank.ExpiringSoon(now.AddDays(2)), "Expired saved resets drop from the badge between refreshes");
        var claudeResets = JsonNode.Parse("""{"cedar_ember":{"grants":[{"resets_left":2,"ends_at":"2026-10-08T12:00:00Z"},{"resets_left":0,"ends_at":"2026-10-10T12:00:00Z"},{"resets_left":1,"ends_at":"2026-10-01T12:00:00Z"}]}}""")!.AsObject();
        Check(Providers.ParseClaudeResets(claudeResets, now)?.Available(now) == 2, "Claude saved resets count remaining grants and exclude expired grants");
        Check(Providers.ParseClaudeResets(new(), now) == null && Providers.ParseCodexResets(new(), now) == null, "Missing reset information remains unknown rather than claiming zero");
        var codex = JsonNode.Parse("""{"rate_limit":{"primary_window":{"used_percent":88,"limit_window_seconds":18000,"reset_after_seconds":1000},"secondary_window":{"used_percent":57,"limit_window_seconds":604800,"reset_after_seconds":302400}}}""")!.AsObject();
        var cw = Providers.ParseCodex(codex, now)!;
        Check(cw.Used == 57 && cw.Reset == now.AddDays(3.5), "Codex reads weekly window, ignoring session limit");
        var swapped = JsonNode.Parse("""{"rate_limit":{"primary_window":{"used_percent":23,"limit_window_seconds":604800,"reset_at":1791288000},"secondary_window":{"used_percent":80,"limit_window_seconds":18000,"reset_after_seconds":30}}}""")!.AsObject();
        Check(Providers.ParseCodex(swapped, now)?.Used == 23, "Codex identifies weekly by duration when windows swap");
        var claude = JsonNode.Parse("""{"five_hour":{"utilization":99},"seven_day":{"utilization":42,"resets_at":"2026-10-10T12:00:00Z"}}""")!.AsObject();
        Check(Providers.ParseClaude(claude)?.Used == 42, "Claude reads overall seven-day allowance");
        var resetless = JsonNode.Parse("""{"five_hour":{"utilization":0,"resets_at":null},"seven_day":{"utilization":42,"resets_at":"2026-10-10T12:00:00Z"}}""")!.AsObject();
        var resetlessLimits = Providers.ParseLimits("Claude", resetless, now);
        var resetlessSession = resetlessLimits.Single(limit => limit.Key == "session");
        Check(resetlessSession.Window.Used == 0 && resetlessSession.Window.Reset == null &&
            PaceMath.Calculate(resetlessSession.Window, now) == null,
            "Five-hour usage remains visible when a reset response has no reset time");
        Check(CompactLimitBars.For(new(new("resetless", "Claude", "Sample", ""), Providers.ParseClaude(resetless), null, now, Limits: resetlessLimits)).Count == 1,
            "A zero-usage five-hour limit without a reset still has an overview bar");
        Check(PaceMath.Classify(resetlessSession.Window with { Used = 100 }, now) == PaceState.Exhausted,
            "Known exhausted usage remains a warning even if the reset time is missing");
        var detailClaude = JsonNode.Parse("""{"five_hour":{"utilization":9,"resets_at":"2026-10-06T15:00:00Z"},"seven_day":{"utilization":18,"resets_at":"2026-10-08T12:00:00Z"},"limits":[{"group":"weekly","percent":3,"resets_at":"2026-10-08T12:00:00Z","scope":{"model":{"display_name":"Fable"}}}],"seven_day_sonnet":null}""")!.AsObject();
        var detailLimits = Providers.ParseLimits("Claude", detailClaude, now);
        Check(detailLimits.Count == 3 && detailLimits.Single(l => l.Key == "session").Window.Period == TimeSpan.FromHours(5) && detailLimits.Single(l => l.Name == "Fable · Weekly").Window.Used == 3, "Details retain real session and model-scoped allowances without empty sections");
        var weeklyOnly = JsonNode.Parse("""{"rate_limit":{"primary_window":{"used_percent":23,"limit_window_seconds":604800,"reset_after_seconds":1000}}}""")!.AsObject();
        Check(Providers.ParseLimits("Codex", weeklyOnly, now) is { Count: 1 } only && only[0].Name == "Weekly", "Weekly-only Codex details do not invent a five-hour limit");
        Check(Providers.ParseClaude(JsonNode.Parse("""{"seven_day":null}""")!.AsObject()) == null, "Absent Claude weekly data remains unavailable");
        Check(Providers.ParseCodex(JsonNode.Parse("""{"rate_limit":{}}""")!.AsObject(), now) == null, "Absent Codex weekly data remains unavailable");
        Check(TrayToggle.ShouldClose(false, 1000, 1100), "Tray click closes a popup already hidden by Windows deactivation");
        Check(!TrayToggle.ShouldClose(false, 1000, 1400), "Subsequent tray click opens the popup");
        Check(TrayToggle.ShouldClose(true, 0, 1400), "Visible popup closes on tray click");
        return detailLimits;
    }
}
