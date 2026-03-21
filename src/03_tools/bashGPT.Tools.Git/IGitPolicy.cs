namespace bashGPT.Tools.Git;

public interface IGitPolicy
{
    bool AllowRead(string repoPath);
    bool AllowWrite(string repoPath);
}
