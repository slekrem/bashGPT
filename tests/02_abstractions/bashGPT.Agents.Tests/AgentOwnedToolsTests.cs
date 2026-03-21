using bashGPT.Agents;
using bashGPT.Agents.Dev;
using bashGPT.Agents.Shell;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Tests;

/// <summary>
/// Verifies the "agents own their tools" architectural contract:
/// - Self-contained agents expose their tools via GetOwnedTools()
/// - EnabledTools is derived automatically from GetOwnedTools()
/// - TryHandleToolCallAsync routes to owned tools without the registry
/// </summary>
public class AgentOwnedToolsTests
{
    // --- AgentBase defaults --------------------------------------------------

    [Fact]
    public void AgentBase_EnabledTools_DerivedFromOwnedTools()
    {
        var agent = new ShellAgent();

        var ownedNames  = agent.GetOwnedTools().Select(t => t.Definition.Name).ToArray();
        var enabledNames = agent.EnabledTools.ToArray();

        Assert.Equal(ownedNames, enabledNames);
    }

    [Fact]
    public async Task AgentBase_TryHandleToolCallAsync_ExecutesOwnedTool()
    {
        var agent = new ShellAgent();
        var toolName = agent.EnabledTools[0]; // e.g. "bash_exec" or "cmd_exec"

        // A minimal valid shell command that should succeed on any platform
        var args = """{"command": "echo hello"}""";
        var result = await agent.TryHandleToolCallAsync(toolName, args, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("hello", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AgentBase_TryHandleToolCallAsync_ReturnsNullForUnknownTool()
    {
        var agent = new ShellAgent();

        var result = await agent.TryHandleToolCallAsync("unknown_tool", "{}", null, CancellationToken.None);

        Assert.Null(result);
    }

    // --- ShellAgent ----------------------------------------------------------

    [Fact]
    public void ShellAgent_GetOwnedTools_ReturnsSingleTool()
    {
        var agent = new ShellAgent();

        Assert.Single(agent.GetOwnedTools());
    }

    [Fact]
    public void ShellAgent_EnabledTools_MatchesOwnedToolName()
    {
        var agent = new ShellAgent();

        Assert.Single(agent.EnabledTools);
        Assert.Equal(agent.GetOwnedTools()[0].Definition.Name, agent.EnabledTools[0]);
    }

    // --- DevAgent ------------------------------------------------------------

    [Fact]
    public void DevAgent_GetOwnedTools_ContainsContextTools()
    {
        var agent = new DevAgent();
        var ownedNames = agent.GetOwnedTools().Select(t => t.Definition.Name).ToArray();

        Assert.Contains("context_load_files",   ownedNames);
        Assert.Contains("context_unload_files", ownedNames);
        Assert.Contains("context_clear_files",  ownedNames);
    }

    [Fact]
    public void DevAgent_EnabledTools_ContainsBothOwnedAndRegistryTools()
    {
        var agent = new DevAgent();

        // owned
        Assert.Contains("context_load_files", agent.EnabledTools);
        // registry
        Assert.Contains("shell_exec",          agent.EnabledTools);
        Assert.Contains("filesystem_read",     agent.EnabledTools);
        Assert.Contains("git_status",          agent.EnabledTools);
    }

    [Fact]
    public async Task DevAgent_TryHandleToolCallAsync_HandlesOwnedContextTool()
    {
        var agent = new DevAgent();
        var args  = """{"paths": []}""";

        var result = await agent.TryHandleToolCallAsync(
            "context_load_files", args, null, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DevAgent_TryHandleToolCallAsync_ReturnsNullForRegistryTool()
    {
        var agent = new DevAgent();

        // git_status is a registry tool — agent should not handle it
        var result = await agent.TryHandleToolCallAsync(
            "git_status", "{}", null, CancellationToken.None);

        Assert.Null(result);
    }
}
