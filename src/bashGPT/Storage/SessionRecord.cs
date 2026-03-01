namespace BashGPT.Storage;

/// <summary>
/// Wurzel-Dokument für ~/.config/bashgpt/sessions.json
/// </summary>
public sealed class SessionsFile
{
    public int Version { get; set; } = 1;
    public List<SessionRecord> Sessions { get; set; } = [];
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
}
