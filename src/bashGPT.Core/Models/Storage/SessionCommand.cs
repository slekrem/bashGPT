namespace bashGPT.Core.Models.Storage;

public sealed class SessionCommand
{
    public string Command { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public bool WasExecuted { get; set; }
}
