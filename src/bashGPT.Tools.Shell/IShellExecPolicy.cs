namespace BashGPT.Tools.Shell;

public interface IShellExecPolicy
{
    bool Allow(ShellExecInput input);
}
