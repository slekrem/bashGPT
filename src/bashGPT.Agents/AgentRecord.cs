namespace BashGPT.Agents;

public sealed class AgentRecord
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? SystemPrompt { get; set; }
    public List<string> EnabledTools { get; set; } = [];
}
