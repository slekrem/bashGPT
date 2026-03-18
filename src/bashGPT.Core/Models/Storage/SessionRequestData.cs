namespace BashGPT.Storage;

public sealed class SessionRequestData
{
    public string Prompt { get; set; } = string.Empty;
    public string? ExecMode { get; set; }
}
