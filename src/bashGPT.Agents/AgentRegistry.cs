namespace bashGPT.Agents;

/// <summary>
/// In-memory registry of all registered chat agents.
/// Agents are registered in code at server startup — no file persistence.
/// </summary>
public sealed class AgentRegistry
{
    private readonly Dictionary<string, AgentBase> _agents;

    /// <summary>
    /// Initializes the registry with the given agents.
    /// Throws <see cref="ArgumentException"/> when two agents share the same ID (case-insensitive).
    /// </summary>
    /// <exception cref="ArgumentException">Duplicate agent ID detected.</exception>
    public AgentRegistry(IEnumerable<AgentBase> agents)
    {
        _agents = new Dictionary<string, AgentBase>(StringComparer.OrdinalIgnoreCase);
        foreach (var agent in agents)
        {
            if (!_agents.TryAdd(agent.Id, agent))
                throw new ArgumentException($"Duplicate agent ID: '{agent.Id}'. Agent IDs must be unique (case-insensitive).");
        }
    }

    /// <summary>All registered agents.</summary>
    public IReadOnlyCollection<AgentBase> All => _agents.Values;

    /// <summary>Looks up an agent by ID. Returns null when not found.</summary>
    public AgentBase? Find(string id) => _agents.GetValueOrDefault(id);
}
