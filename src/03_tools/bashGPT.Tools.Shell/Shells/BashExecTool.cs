using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Shell.Shells;

/// <summary>
/// Executes commands in bash (or the shell set via the <c>SHELL</c> environment variable).
/// Suited for Linux, macOS, and Windows environments running Git Bash or WSL.
/// </summary>
public sealed class BashExecTool : ShellExecBase
{
    public BashExecTool(IShellExecPolicy? policy = null, Action<ShellExecInput, ShellExecOutput>? onExecuted = null)
        : base(policy, onExecuted) { }

    public override ToolDefinition Definition { get; } = new(
        Name: "bash_exec",
        Description: "Executes a bash command and returns stdout, stderr, exit code, duration and timeout status.",
        Parameters:
        [
            new ToolParameter("command", "string", "The bash command to execute.", Required: true),
            new ToolParameter("cwd",       "string",  "Working directory for the command.", Required: false),
            new ToolParameter("timeoutMs", "integer", "Timeout in milliseconds (default: 5000).", Required: false),
            new ToolParameter("env",       "object",  "Additional environment variables (key-value pairs).", Required: false),
        ]);

    protected override (string FileName, string[] Arguments) GetShellArgs(string command)
    {
        var shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";
        return (shell, ["-c", command]);
    }
}
