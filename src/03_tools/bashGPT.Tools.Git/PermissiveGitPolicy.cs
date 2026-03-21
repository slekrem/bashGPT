namespace bashGPT.Tools.Git;

/// <summary>
/// Allows all read and write operations. Use only when explicitly configured.
/// </summary>
public sealed class PermissiveGitPolicy : IGitPolicy
{
    public bool AllowRead(string repoPath) => true;
    public bool AllowWrite(string repoPath) => true;
}
