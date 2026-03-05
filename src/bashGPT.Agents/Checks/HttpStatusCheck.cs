namespace BashGPT.Agents;

public sealed class HttpStatusCheck : IAgentCheck
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public AgentCheckType Type => AgentCheckType.HttpStatus;

    public async Task<AgentCheckResult> RunAsync(AgentRecord agent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(agent.Url))
            return new AgentCheckResult("error", Changed: true, "Keine URL konfiguriert.", Success: false);

        try
        {
            var response = await Http.GetAsync(agent.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            var hash = ((int)response.StatusCode).ToString();
            var changed = hash != agent.LastHash;
            var message = changed
                ? $"Status geändert: {(int)response.StatusCode} {response.ReasonPhrase}"
                : $"Status unverändert: {(int)response.StatusCode} {response.ReasonPhrase}";

            return new AgentCheckResult(hash, changed, message, Success: true);
        }
        catch (Exception ex)
        {
            const string errorHash = "error";
            var changed = errorHash != agent.LastHash;
            return new AgentCheckResult(errorHash, changed, $"Fehler: {ex.Message}", Success: false);
        }
    }
}
