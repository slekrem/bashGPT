using bashGPT.Agents.Dev;

namespace bashGPT.Agents.Tests;

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
    public void DevAgent_EnabledTools_ContainsReadFileTool()
    {
        Assert.Contains("read_file", _agent.EnabledTools);
    }

    [Fact]
    public void DevAgent_SystemPrompt_ContainsReadFileInstruction()
    {
        Assert.Contains(_agent.SystemPrompt, p => p.Contains("read_file", StringComparison.Ordinal));
    }

    [Fact]
    public void DevAgent_GetInfoPanelMarkdown_ContainsSystemPromptAndLlmConfig()
    {
        var md = _agent.GetInfoPanelMarkdown();

        Assert.Contains("You are an experienced", md, StringComparison.Ordinal);
        Assert.Contains("## LLM Configuration", md, StringComparison.Ordinal);
    }

    [Fact]
    public void DevAgent_GetInfoPanelMarkdown_ContainsLlmConfigSection()
    {
        var md = _agent.GetInfoPanelMarkdown();

        Assert.Contains("## LLM Configuration", md, StringComparison.Ordinal);
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
