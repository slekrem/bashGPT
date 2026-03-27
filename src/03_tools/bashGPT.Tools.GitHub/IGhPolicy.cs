namespace bashGPT.Tools.GitHub;

public interface IGhPolicy
{
    bool AllowRead(string repoPath);
    bool AllowWrite(string repoPath);
}
