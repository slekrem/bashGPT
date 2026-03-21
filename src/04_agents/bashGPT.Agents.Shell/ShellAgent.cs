using System.Runtime.InteropServices;
using bashGPT.Core.Models.Providers;
using bashGPT.Tools.Abstractions;
using bashGPT.Tools.Shell;
using ToolCall = bashGPT.Tools.Abstractions.ToolCall;

namespace bashGPT.Agents.Shell;

/// <summary>
/// Shell assistant focused on terminal tasks and shell commands.
/// </summary>
public sealed class ShellAgent : AgentBase
{
    private readonly ShellExecTool _shellExecTool = new();

    public override string Id => "shell";

    public override string Name => "Shell-Agent";

    public override IReadOnlyList<string> EnabledTools => ["shell_exec"];

    public override AgentLlmConfig LlmConfig => new(
        Temperature: 0.1,
        TopP:        0.9,
        Stream:      true
    );

    public override IReadOnlyList<string> SystemPrompt =>
    [
        BuildRolePrompt(),
        BuildContextPrompt(),
    ];

    private static string BuildRolePrompt()
    {
        var os    = GetOsDescription();
        var shell = GetShell();
        return $"""
            You are a shell executor on {os} using {shell}.
            Run commands and stay quiet afterward.

            Output rules:
            - Do NOT write a follow-up response after a tool call. The output is already visible.
            - Do not explain, suggest, or comment.
            - If you run multiple steps, execute them back-to-back without extra text between them.
            - Only if something fails or you need a decision: one plain-text line, nothing more.

            Execution rules:
            - Use only non-interactive commands (no vim, top, htop, less, tail -f).
            - Keep output short with head, tail, grep, and similar filters. Never dump huge raw output.
            - Always use the correct syntax for {shell}.
            {GetPlatformHints()}
            - Destructive actions such as rm -rf or disk formatting require explicit confirmation.
            """;
    }

    private static string BuildContextPrompt() =>
        $"""
        System context:
        - User:        {Environment.UserName}
        - Host:        {Environment.MachineName}
        - OS:          {GetOsDescription()}
        - Shell:       {GetShell()}
        - Directory:   {Directory.GetCurrentDirectory()}
        - Date/Time:   {DateTime.Now:dd.MM.yyyy HH:mm:ss zzz}
        """;

    public override IReadOnlyList<ITool> GetOwnedTools() => [_shellExecTool];

    public override async Task<string?> TryHandleToolCallAsync(
        string toolName,
        string argumentsJson,
        string? sessionPath,
        CancellationToken ct)
    {
        var tool = GetOwnedTools().FirstOrDefault(t => t.Definition.Name == toolName);
        if (tool is null) return null;
        var result = await tool.ExecuteAsync(new ToolCall(toolName, argumentsJson, sessionPath), ct);
        return result.Content;
    }

    protected override string GetAgentMarkdown() =>
        $"""
        # Shell-Agent

        Specialized shell assistant for terminal tasks.

        ## System Context

        | Property | Value |
        |---|---|
        | `user` | `{Environment.UserName}` |
        | `host` | `{Environment.MachineName}` |
        | `os` | `{GetOsDescription()}` |
        | `shell` | `{GetShell()}` |
        | `cwd` | `{Directory.GetCurrentDirectory()}` |
        | `date` | `{DateTime.Now:dd.MM.yyyy}` |
        | `time` | `{DateTime.Now:HH:mm:ss zzz}` |

        ## Enabled Tools

        | Tool | Description |
        |---|---|
        | `shell_exec` | Run shell commands in the current working directory |

        ## Rules

        - Use only non-interactive commands (`vim`, `top`, `less`, and `tail -f` are not allowed)
        - Keep output focused with `head`, `grep`, and similar filters
        - Do not run destructive actions without explicit confirmation
        """;

    private static string GetPlatformHints()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var shell = GetShell();
            if (shell.Contains("bash", StringComparison.OrdinalIgnoreCase))
                return "- You are running bash on Windows; use Unix paths and bash syntax, not cmd.exe or PowerShell.";
            if (shell.Contains("powershell", StringComparison.OrdinalIgnoreCase) || shell.Contains("pwsh", StringComparison.OrdinalIgnoreCase))
                return "- Use PowerShell cmdlets and syntax (e.g. Get-ChildItem, Select-String). Avoid bash/Unix-only commands.";
            return "- Use cmd.exe syntax. Avoid bash/Unix-only commands.";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "- Use full paths for system tools, for example /usr/bin/log show or /usr/sbin/system_profiler.";
        return "- Prefer standard POSIX commands for maximum compatibility.";
    }

    private static string GetOsDescription()
    {
        var desc = RuntimeInformation.OSDescription;
        var arch = RuntimeInformation.OSArchitecture;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))     return $"macOS {arch} - {desc}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))   return $"Linux {arch} - {desc}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return $"Windows {arch} - {desc}";
        return $"{desc} ({arch})";
    }

    private static string GetShell() =>
        Environment.GetEnvironmentVariable("SHELL")
        ?? Environment.GetEnvironmentVariable("ComSpec")
        ?? "unknown";
}
