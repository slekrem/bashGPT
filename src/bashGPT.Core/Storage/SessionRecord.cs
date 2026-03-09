namespace BashGPT.Storage;

/// <summary>
/// Wurzel-Dokument für ~/.config/bashgpt/sessions.json (Legacy – nur noch für Migration).
/// </summary>
public sealed class SessionsFile
{
    public int Version { get; set; } = 1;
    public List<SessionRecord> Sessions { get; set; } = [];
}

/// <summary>
/// Schlanker Index-Eintrag für sessions/index.json (ohne Messages).
/// </summary>
public sealed class SessionIndexEntry
{
    public string Id        { get; set; } = string.Empty;
    public string Title     { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Wurzel-Dokument für sessions/index.json.
/// </summary>
public sealed class SessionIndex
{
    public int Version { get; set; } = 1;
    public List<SessionIndexEntry> Sessions { get; set; } = [];
}

/// <summary>
/// Inhalt einer einzelnen Session-Datei sessions/&lt;id&gt;.json.
/// </summary>
public sealed class SessionContent
{
    public int Version { get; set; } = 1;
    public List<SessionMessage> Messages { get; set; } = [];
    public SessionShellContext? ShellContext { get; set; }
}

/// <summary>
/// Repräsentiert eine einzelne Chat-Session mit Metadaten und Nachrichten.
/// </summary>
public sealed class SessionRecord
{
    public string Id          { get; set; } = string.Empty;
    public string Title       { get; set; } = string.Empty;
    public string CreatedAt   { get; set; } = string.Empty;
    public string UpdatedAt   { get; set; } = string.Empty;
    public List<SessionMessage> Messages { get; set; } = [];
    public SessionShellContext? ShellContext { get; set; }
}

public sealed class SessionMessage
{
    public string   Role     { get; set; } = string.Empty;
    public string   Content  { get; set; } = string.Empty;
    public string?  ExecMode { get; set; }
    public List<SessionCommand>? Commands { get; set; }
    public SessionTokenUsage? Usage { get; set; }

    /// <summary>Tool-Calls die der Assistent angefordert hat (Role=assistant mit Tool-Calls).</summary>
    public List<SessionToolCall>? ToolCalls { get; set; }

    /// <summary>ID des zugehörigen Tool-Calls (Role=tool).</summary>
    public string? ToolCallId { get; set; }

    /// <summary>Name des aufgerufenen Tools (Role=tool).</summary>
    public string? ToolName { get; set; }
}

public sealed class SessionToolCall
{
    public string? Id            { get; set; }
    public string  Name          { get; set; } = string.Empty;
    public string  ArgumentsJson { get; set; } = string.Empty;
}

public sealed class SessionCommand
{
    public string Command     { get; set; } = string.Empty;
    public int    ExitCode    { get; set; }
    public string Output      { get; set; } = string.Empty;
    public bool   WasExecuted { get; set; }
}

public sealed class SessionShellContext
{
    public string User { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Cwd  { get; set; } = string.Empty;
}

public sealed class SessionTokenUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int? TotalTokens { get; set; }
    public int? CachedInputTokens { get; set; }
}

/// <summary>
/// Dokumentation einer einzelnen POST-Anfrage, gespeichert unter
/// sessions/&lt;id&gt;/requests/&lt;timestamp&gt;.json.
/// Enthält Request- und Response-Daten getrennt.
/// </summary>
public sealed class SessionRequestRecord
{
    public string              Timestamp { get; set; } = string.Empty;
    public SessionRequestData  Request   { get; set; } = new();
    public SessionResponseData Response  { get; set; } = new();
}

public sealed class SessionRequestData
{
    public string  Prompt   { get; set; } = string.Empty;
    public string? ExecMode { get; set; }
}

public sealed class SessionResponseData
{
    public string                Content  { get; set; } = string.Empty;
    public List<SessionCommand>? Commands { get; set; }
    public SessionTokenUsage?    Usage    { get; set; }
}
