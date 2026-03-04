namespace BashGPT.Agents;

/// <summary>
/// Führt aktive Agenten zyklisch aus und meldet Zustandsänderungen.
/// </summary>
public sealed class AgentRunner
{
    private readonly AgentStore _store;
    private readonly IReadOnlyDictionary<AgentCheckType, IAgentCheck> _checks;

    public AgentRunner(AgentStore store, IEnumerable<IAgentCheck> checks)
    {
        _store = store;
        _checks = checks.ToDictionary(c => c.Type);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var agents = await _store.LoadAllAsync();

            foreach (var agent in agents.Where(a => a.IsActive))
            {
                if (!ShouldRun(agent))
                    continue;

                if (!_checks.TryGetValue(agent.Type, out var check))
                {
                    Console.Error.WriteLine($"[WARN] Kein Check für Typ {agent.Type} registriert.");
                    continue;
                }

                var now = DateTimeOffset.UtcNow;
                try
                {
                    var result = await check.RunAsync(agent, ct);

                    if (result.Changed)
                        Console.WriteLine($"[{now:HH:mm:ss}] {agent.Name}: {result.Message}");

                    agent.LastHash = result.Hash;
                    agent.LastRun = now;
                    agent.LastMessage = result.Message;
                    agent.LastCheckSucceeded = result.Success;
                    agent.FailureCount = result.Success ? 0 : agent.FailureCount + 1;
                }
                catch (Exception ex)
                {
                    agent.FailureCount++;
                    agent.LastCheckSucceeded = false;
                    agent.LastRun = now;
                    Console.Error.WriteLine($"[ERROR] {agent.Name}: {ex.Message}");
                }

                await _store.UpsertAsync(agent);
            }

            await Task.Delay(1_000, ct).ConfigureAwait(false);
        }
    }

    private static bool ShouldRun(AgentRecord agent)
    {
        if (agent.LastRun is null)
            return true;
        return DateTimeOffset.UtcNow >= agent.LastRun.Value.AddSeconds(agent.IntervalSeconds);
    }
}
