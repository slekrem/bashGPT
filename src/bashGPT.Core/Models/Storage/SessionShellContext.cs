namespace bashGPT.Core.Models.Storage;

public sealed class SessionShellContext
{
    public string User { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Cwd { get; set; } = string.Empty;
}
