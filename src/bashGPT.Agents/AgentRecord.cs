using System.Text.Json.Serialization;

namespace BashGPT.Agents;

public enum AgentCheckType { GitStatus, HttpStatus, BitcoinPrice, LlmAgent }

public sealed class AgentsFile
{
    public List<AgentRecord> Agents { get; set; } = [];
}

public sealed class AgentRecord
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AgentCheckType Type { get; set; }

    public string? Path { get; set; }
    public string? Url { get; set; }
    public int IntervalSeconds { get; set; } = 30;
    public string? SystemPrompt { get; set; }
    public string? LoopInstruction { get; set; }
    public string? ExecMode { get; set; }
    public List<string> EnabledTools { get; set; } = [];
    public bool IsActive { get; set; } = true;

    // Runtime-State
    public string? LastHash { get; set; }
    public DateTimeOffset? LastRun { get; set; }
    public string? LastMessage { get; set; }
    public int FailureCount { get; set; }
    public bool LastCheckSucceeded { get; set; } = true;
}
