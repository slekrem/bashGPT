namespace BashGPT.Storage;

/// <summary>
/// Compact session metadata entry stored in sessions/index.json.
/// </summary>
public sealed class SessionIndexEntry
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Root document stored in sessions/index.json.
/// </summary>
public sealed class SessionIndex
{
    public int Version { get; set; } = 1;
    public List<SessionIndexEntry> Sessions { get; set; } = [];
}

/// <summary>
/// Content document stored in sessions/&lt;id&gt;/content.json.
/// </summary>
public sealed class SessionContent
{
    public int Version { get; set; } = 1;
    public List<SessionMessage> Messages { get; set; } = [];
    public SessionShellContext? ShellContext { get; set; }
    public List<string>? EnabledTools { get; set; }
    public string? AgentId { get; set; }
}

/// <summary>
/// Represents a single chat session with metadata and messages.
/// </summary>
public sealed class SessionRecord
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public List<SessionMessage> Messages { get; set; } = [];
    public SessionShellContext? ShellContext { get; set; }
    public List<string>? EnabledTools { get; set; }
    public string? AgentId { get; set; }
}

public sealed class SessionMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ExecMode { get; set; }
    public List<SessionCommand>? Commands { get; set; }
    public SessionTokenUsage? Usage { get; set; }

    /// <summary>Tool calls requested by the assistant (assistant role with tool calls).</summary>
    public List<SessionToolCall>? ToolCalls { get; set; }

    /// <summary>ID of the associated tool call (tool role).</summary>
    public string? ToolCallId { get; set; }

    /// <summary>Name of the invoked tool (tool role).</summary>
    public string? ToolName { get; set; }
}

public sealed class SessionToolCall
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = string.Empty;
}

public sealed class SessionCommand
{
    public string Command { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public bool WasExecuted { get; set; }
}

public sealed class SessionShellContext
{
    public string User { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Cwd { get; set; } = string.Empty;
}

public sealed class SessionTokenUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int? TotalTokens { get; set; }
    public int? CachedInputTokens { get; set; }
}

/// <summary>
/// Records a single POST request stored under
/// sessions/&lt;id&gt;/requests/&lt;timestamp&gt;.json.
/// Keeps request and response data separately.
/// </summary>
public sealed class SessionRequestRecord
{
    public string Timestamp { get; set; } = string.Empty;
    public SessionRequestData Request { get; set; } = new();
    public SessionResponseData Response { get; set; } = new();
}

public sealed class SessionRequestData
{
    public string Prompt { get; set; } = string.Empty;
    public string? ExecMode { get; set; }
}

public sealed class SessionResponseData
{
    public string Content { get; set; } = string.Empty;
    public List<SessionCommand>? Commands { get; set; }
    public SessionTokenUsage? Usage { get; set; }
}
