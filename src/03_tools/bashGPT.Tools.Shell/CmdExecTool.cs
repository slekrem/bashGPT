using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Shell;

/// <summary>
/// Executes commands in Windows cmd.exe.
/// Use cmd.exe-compatible syntax: pipes (|), redirects (&gt;), FOR loops, etc.
/// </summary>
public sealed class CmdExecTool : ShellExecBase
{
    public CmdExecTool(IShellExecPolicy? policy = null, Action<ShellExecInput, ShellExecOutput>? onExecuted = null)
        : base(policy, onExecuted) { }

    public override ToolDefinition Definition { get; } = new(
        Name: "cmd_exec",
        Description: "Executes a Windows cmd.exe command and returns stdout, stderr, exit code, duration and timeout status.",
        Parameters:
        [
            new ToolParameter("command", "string", "The cmd.exe command to execute.", Required: true),
            new ToolParameter("cwd",       "string",  "Working directory for the command.", Required: false),
            new ToolParameter("timeoutMs", "integer", "Timeout in milliseconds (default: 5000).", Required: false),
            new ToolParameter("env",       "object",  "Additional environment variables (key-value pairs).", Required: false),
        ]);

    protected override (string FileName, string[] Arguments) GetShellArgs(string command)
        => ("cmd.exe", ["/c", command]);
}
