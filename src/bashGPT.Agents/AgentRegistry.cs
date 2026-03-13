namespace BashGPT.Agents;

/// <summary>
/// In-Memory-Registry aller registrierten Chat-Agenten.
/// Agenten werden beim Server-Start per Code registriert – keine Datei-Persistenz.
/// </summary>
public sealed class AgentRegistry
{
    private readonly Dictionary<string, AgentBase> _agents;

    public AgentRegistry(IEnumerable<AgentBase> agents)
    {
        _agents = agents.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Alle registrierten Agenten.</summary>
    public IReadOnlyCollection<AgentBase> All => _agents.Values;

    /// <summary>Sucht einen Agenten anhand seiner ID. Gibt null zurück wenn nicht gefunden.</summary>
    public AgentBase? Find(string id) => _agents.GetValueOrDefault(id);
}
