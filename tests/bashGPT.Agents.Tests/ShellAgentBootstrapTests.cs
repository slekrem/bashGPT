using BashGPT.Agents.Shell;

namespace BashGPT.Agents.Tests;

public class ShellAgentTests
{
    private readonly ShellAgent _agent = new();

    [Fact]
    public void ShellAgent_HasExpectedIdAndName()
    {
        Assert.Equal("shell", _agent.Id);
        Assert.Equal("Shell-Agent", _agent.Name);
    }

    [Fact]
    public void ShellAgent_EnabledTools_ContainsOnlyShellExec()
    {
        Assert.Single(_agent.EnabledTools);
        Assert.Contains("shell_exec", _agent.EnabledTools);
    }

    [Fact]
    public void ShellAgent_SystemPrompt_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(_agent.SystemPrompt));
    }

    [Fact]
    public void ShellAgent_GetInfoPanelMarkdown_ContainsH1AndToolTable()
    {
        var md = _agent.GetInfoPanelMarkdown();

        Assert.Contains("# Shell-Agent", md, StringComparison.Ordinal);
        Assert.Contains("| `shell_exec`", md, StringComparison.Ordinal);
    }
}
