using BashGPT.Agents.Dev;

namespace BashGPT.Agents.Tests;

public class DevAgentTests
{
    private readonly DevAgent _agent = new();

    [Fact]
    public void DevAgent_HasExpectedIdAndName()
    {
        Assert.Equal("dev", _agent.Id);
        Assert.Equal("Dev-Agent", _agent.Name);
    }

    [Fact]
    public void DevAgent_EnabledTools_ContainsExpectedTools()
    {
        Assert.Contains("fetch", _agent.EnabledTools);
        Assert.Contains("filesystem_search", _agent.EnabledTools);
        Assert.Contains("shell_exec", _agent.EnabledTools);
        Assert.Contains("git_status", _agent.EnabledTools);
        Assert.Contains("test_run", _agent.EnabledTools);
        Assert.Contains("build_run", _agent.EnabledTools);
    }

    [Fact]
    public void DevAgent_SystemPrompt_ContainsToolCallRules()
    {
        Assert.Contains("Required Fields", _agent.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("'pattern' Pflicht", _agent.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("invalid_json", _agent.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("missing_required_field", _agent.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("invalid_type", _agent.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("invalid_value", _agent.SystemPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void DevAgent_GetInfoPanelMarkdown_ContainsH1AndToolTable()
    {
        var md = _agent.GetInfoPanelMarkdown();

        Assert.Contains("# Dev-Agent", md, StringComparison.Ordinal);
        Assert.Contains("| `fetch`", md, StringComparison.Ordinal);
        Assert.Contains("| `shell_exec`", md, StringComparison.Ordinal);
    }
}
