namespace bashGPT.Tools.GitHub;

/// <summary>
/// Default policy: read operations are allowed, write operations are blocked.
/// </summary>
public sealed class DefaultGhPolicy : IGhPolicy
{
    public bool AllowRead(string repoPath) => true;
    public bool AllowWrite(string repoPath) => false;
}
