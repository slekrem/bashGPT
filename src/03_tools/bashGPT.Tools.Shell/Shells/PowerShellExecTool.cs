using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Shell;

/// <summary>
/// Executes commands in PowerShell (prefers <c>pwsh.exe</c> / PowerShell 7+,
/// falls back to <c>powershell.exe</c> / Windows PowerShell 5.1).
/// Use PowerShell syntax: cmdlets, pipelines, script blocks, etc.
/// </summary>
public sealed class PowerShellExecTool : ShellExecBase
{
    private static readonly string _executable = ResolvePwsh();

    public PowerShellExecTool(IShellExecPolicy? policy = null, Action<ShellExecInput, ShellExecOutput>? onExecuted = null)
        : base(policy, onExecuted) { }

    public override ToolDefinition Definition { get; } = new(
        Name: "pwsh_exec",
        Description: "Executes a PowerShell command or script block and returns stdout, stderr, exit code, duration and timeout status.",
        Parameters:
        [
            new ToolParameter("command", "string", "The PowerShell command or script block to execute.", Required: true),
            new ToolParameter("cwd",       "string",  "Working directory for the command.", Required: false),
            new ToolParameter("timeoutMs", "integer", "Timeout in milliseconds (default: 5000).", Required: false),
            new ToolParameter("env",       "object",  "Additional environment variables (key-value pairs).", Required: false),
        ]);

    protected override (string FileName, string[] Arguments) GetShellArgs(string command)
        => (_executable, ["-NoProfile", "-NonInteractive", "-Command", command]);

    /// <summary>
    /// Returns the path to <c>pwsh.exe</c> (PowerShell 7+) if found in PATH,
    /// otherwise falls back to <c>powershell.exe</c> (Windows PowerShell 5.1).
    /// </summary>
    private static string ResolvePwsh()
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
        {
            var pwsh = Path.Combine(dir, "pwsh.exe");
            if (File.Exists(pwsh))
                return pwsh;
        }
        return "powershell.exe";
    }
}
