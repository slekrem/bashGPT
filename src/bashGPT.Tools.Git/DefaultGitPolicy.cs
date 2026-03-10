namespace BashGPT.Tools.Git;

/// <summary>
/// Read-only by default. Write-ops (add, commit, checkout) are blocked.
/// </summary>
public sealed class DefaultGitPolicy : IGitPolicy
{
    public bool AllowRead(string repoPath) => true;
    public bool AllowWrite(string repoPath) => false;
}
