namespace BashGPT.Agents;

public interface IAgentCheck
{
    AgentCheckType Type { get; }
    Task<AgentCheckResult> RunAsync(AgentRecord agent, CancellationToken ct);
}

public record AgentCheckResult(
    string Hash,
    bool Changed,
    string Message,
    bool Success);
