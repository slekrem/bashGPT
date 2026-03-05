namespace BashGPT.Tools.Shell;

public sealed class DefaultShellExecPolicy : IShellExecPolicy
{
    private static readonly string[] BlockedPatterns =
    [
        "rm -rf",
        "rm -fr",
        "dd if=",
        "mkfs",
        "> /dev/",
        ":(){ :|:& };:",   // fork bomb
        "chmod -R 777",
        "chown -R",
        "curl | bash",
        "curl|bash",
        "wget | bash",
        "wget|bash",
    ];

    public bool Allow(ShellExecInput input)
    {
        foreach (var pattern in BlockedPatterns)
        {
            if (input.Command.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
