using System.Text.Json;

namespace BashGPT.Agents;

public sealed class BitcoinPriceCheck : IAgentCheck
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public AgentCheckType Type => AgentCheckType.BitcoinPrice;

    public async Task<AgentCheckResult> RunAsync(AgentRecord agent, CancellationToken ct)
    {
        var url = agent.Url ?? "https://mempool.space/api/v1/prices";

        try
        {
            var json = await Http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("USD", out var usdEl))
                return new AgentCheckResult("error", Changed: true, "Kein USD-Preis in der API-Antwort.", Success: false);

            var price = usdEl.GetDecimal();
            var hash = price.ToString("F0");
            var changed = hash != agent.LastHash;

            var apiTime = doc.RootElement.TryGetProperty("time", out var timeEl)
                ? DateTimeOffset.FromUnixTimeSeconds(timeEl.GetInt64()).ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")
                : null;
            var timeStr = apiTime is not null ? $"  [{apiTime}]" : "";

            var message = agent.LastHash is null || !decimal.TryParse(agent.LastHash, out var lastPrice)
                ? $"BTC/USD  ${price:N0}{timeStr}"
                : changed
                    ? $"BTC/USD  ${price:N0}  {(price > lastPrice ? "↑" : "↓")} {(price > lastPrice ? "+" : "-")}${Math.Abs(price - lastPrice):N0}  (zuvor ${lastPrice:N0}){timeStr}"
                    : $"BTC/USD  ${price:N0}  (unverändert){timeStr}";

            return new AgentCheckResult(hash, changed, message, Success: true);
        }
        catch (Exception ex)
        {
            return new AgentCheckResult("error", Changed: true, $"Fehler: {ex.Message}", Success: false);
        }
    }
}
