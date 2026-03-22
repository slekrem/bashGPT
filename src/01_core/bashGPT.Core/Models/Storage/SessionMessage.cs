namespace bashGPT.Core.Models.Storage;

public sealed class SessionMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<SessionCommand>? Commands { get; set; }
    public SessionTokenUsage? Usage { get; set; }

    /// <summary>Tool calls requested by the assistant (assistant role with tool calls).</summary>
    public List<SessionToolCall>? ToolCalls { get; set; }

    /// <summary>ID of the associated tool call (tool role).</summary>
    public string? ToolCallId { get; set; }

    /// <summary>Name of the invoked tool (tool role).</summary>
    public string? ToolName { get; set; }
}
