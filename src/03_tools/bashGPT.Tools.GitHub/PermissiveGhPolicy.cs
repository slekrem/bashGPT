namespace bashGPT.Tools.GitHub;

/// <summary>
/// Permissive policy: both read and write operations are allowed.
/// </summary>
public sealed class PermissiveGhPolicy : IGhPolicy
{
    public bool AllowRead(string repoPath) => true;
    public bool AllowWrite(string repoPath) => true;
}
