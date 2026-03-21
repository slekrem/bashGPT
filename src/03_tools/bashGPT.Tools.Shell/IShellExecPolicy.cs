namespace bashGPT.Tools.Shell;

public interface IShellExecPolicy
{
    bool Allow(ShellExecInput input);
}
