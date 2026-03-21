using System.Runtime.InteropServices;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Shell;

/// <summary>
/// Generic shell execution tool that auto-detects the platform shell at runtime.
/// On Unix/macOS uses the <c>SHELL</c> environment variable (fallback: <c>/bin/bash</c>).
/// On Windows uses <c>SHELL</c> if set (e.g. Git Bash), otherwise <c>cmd.exe</c>.
/// </summary>
public sealed class ShellExecTool : ShellExecBase
{
    public ShellExecTool(IShellExecPolicy? policy = null, Action<ShellExecInput, ShellExecOutput>? onExecuted = null)
        : base(policy, onExecuted) { }

    public override ToolDefinition Definition { get; } = new(
        Name: "shell_exec",
        Description: "Executes a shell command and returns stdout, stderr, exit code, duration and timeout status.",
        Parameters:
        [
            new ToolParameter("command",   "string",  "The shell command to execute.",                        Required: true),
            new ToolParameter("cwd",       "string",  "Working directory for the command.",                   Required: false),
            new ToolParameter("timeoutMs", "integer", "Timeout in milliseconds (default: 5000).",             Required: false),
            new ToolParameter("env",       "object",  "Additional environment variables (key-value pairs).", Required: false),
        ]);

    protected override (string FileName, string[] Arguments) GetShellArgs(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (shell is not null)
                return (shell, ["-c", command]);
            return ("cmd.exe", ["/c", command]);
        }
        var unixShell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";
        return (unixShell, ["-c", command]);
    }
}
