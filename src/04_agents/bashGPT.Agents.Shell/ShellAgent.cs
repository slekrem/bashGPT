using System.Runtime.InteropServices;
using bashGPT.Core.Models.Providers;
using bashGPT.Tools.Abstractions;
using bashGPT.Tools.Shell.Shells;
using ToolCall = bashGPT.Tools.Abstractions.ToolCall;

namespace bashGPT.Agents.Shell;

/// <summary>
/// Shell assistant that identifies the active shell at startup and exposes
/// only the matching shell-specific tool to the LLM.
/// </summary>
public sealed class ShellAgent : AgentBase
{
    private readonly ITool _shellTool = DetectShellTool();

    public override string Id => "shell";

    public override string Name => "Shell-Agent";

    public override IReadOnlyList<string> EnabledTools => [_shellTool.Definition.Name];

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

    public override IReadOnlyList<ITool> GetOwnedTools() => [_shellTool];

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

    protected override string GetAgentMarkdown()
    {
        var toolName = _shellTool.Definition.Name;
        return $"""
            # Shell-Agent

            Specialized shell assistant for terminal tasks.

            ## System Context

            | Property | Value |
            |---|---|
            | `user` | `{Environment.UserName}` |
            | `host` | `{Environment.MachineName}` |
            | `os` | `{GetOsDescription()}` |
            | `shell` | `{GetShellLabel()}` |
            | `cwd` | `{Directory.GetCurrentDirectory()}` |
            | `date` | `{DateTime.Now:dd.MM.yyyy}` |
            | `time` | `{DateTime.Now:HH:mm:ss zzz}` |

            ## Enabled Tools

            | Tool | Description |
            |---|---|
            | `{toolName}` | {_shellTool.Definition.Description} |

            ## Rules

            - Use only non-interactive commands (`vim`, `top`, `less`, and `tail -f` are not allowed)
            - Keep output focused with filters
            - Do not run destructive actions without explicit confirmation
            """;
    }

    // --- detection -----------------------------------------------------------

    /// <summary>
    /// Detects the active shell and returns the matching tool instance.
    /// On non-Windows systems always returns <see cref="BashExecTool"/>.
    /// On Windows: bash (Git Bash / MSYS2) → <see cref="BashExecTool"/>,
    /// PowerShell session → <see cref="PowerShellExecTool"/>,
    /// otherwise → <see cref="CmdExecTool"/>.
    /// </summary>
    private static ITool DetectShellTool()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new BashExecTool();

        if (Environment.GetEnvironmentVariable("SHELL") is not null)
            return new BashExecTool();

        if (IsPowerShellSession())
            return new PowerShellExecTool();

        return new CmdExecTool();
    }

    /// <summary>
    /// Heuristic: PowerShell (Core 7+ or Windows PowerShell 5) adds its own paths
    /// to <c>PSModulePath</c> at session start that cmd.exe never adds.
    /// </summary>
    private static bool IsPowerShellSession()
    {
        var psModulePath = Environment.GetEnvironmentVariable("PSModulePath") ?? string.Empty;
        return psModulePath.Split(Path.PathSeparator)
            .Any(p => p.Contains("PowerShell", StringComparison.OrdinalIgnoreCase));
    }

    // --- prompt builders -----------------------------------------------------

    private static string BuildRolePrompt()
    {
        var os    = GetOsDescription();
        var shell = GetShellLabel();
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
            - Keep output short with filters. Never dump huge raw output.
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
        - Shell:       {GetShellLabel()}
        - Directory:   {Directory.GetCurrentDirectory()}
        - Date/Time:   {DateTime.Now:dd.MM.yyyy HH:mm:ss zzz}
        """;

    // --- helpers -------------------------------------------------------------

    private static string GetPlatformHints()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (Environment.GetEnvironmentVariable("SHELL") is not null)
                return "- You are running bash on Windows; use Unix paths and bash syntax, not cmd.exe or PowerShell.";
            if (IsPowerShellSession())
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

    private static string GetShellLabel()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";

        if (Environment.GetEnvironmentVariable("SHELL") is { } bashShell)
            return bashShell;

        if (IsPowerShellSession())
            return "PowerShell";

        return Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
    }
}
