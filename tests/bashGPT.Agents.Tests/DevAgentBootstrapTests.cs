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
        Assert.Contains(_agent.SystemPrompt, p => p.Contains("invalid_json", StringComparison.Ordinal));
        Assert.Contains(_agent.SystemPrompt, p => p.Contains("missing_required_field", StringComparison.Ordinal));
        Assert.Contains(_agent.SystemPrompt, p => p.Contains("invalid_type", StringComparison.Ordinal));
        Assert.Contains(_agent.SystemPrompt, p => p.Contains("invalid_value", StringComparison.Ordinal));
    }

    [Fact]
    public void DevAgent_GetInfoPanelMarkdown_ContainsH1AndToolTable()
    {
        var md = _agent.GetInfoPanelMarkdown();

        Assert.Contains("# Dev-Agent", md, StringComparison.Ordinal);
        Assert.Contains("| `fetch`", md, StringComparison.Ordinal);
        Assert.Contains("| `shell_exec`", md, StringComparison.Ordinal);
    }

    [Fact]
    public void DevAgent_GetInfoPanelMarkdown_ContainsLlmConfigSection()
    {
        var md = _agent.GetInfoPanelMarkdown();

        Assert.Contains("## LLM-Konfiguration", md, StringComparison.Ordinal);
        Assert.Contains("`temperature`", md, StringComparison.Ordinal);
        Assert.Contains("`top_p`", md, StringComparison.Ordinal);
        Assert.Contains("`stream`", md, StringComparison.Ordinal);
        Assert.Contains("`stream_options`", md, StringComparison.Ordinal);
    }

    [Fact]
    public void DevAgent_LlmConfig_HasExpectedValues()
    {
        var cfg = _agent.LlmConfig;

        Assert.NotNull(cfg);
        Assert.Equal(0.1, cfg.Temperature);
        Assert.Equal(0.95, cfg.TopP);
        Assert.Equal(8192, cfg.MaxTokens);
        Assert.True(cfg.Stream);
    }
}
