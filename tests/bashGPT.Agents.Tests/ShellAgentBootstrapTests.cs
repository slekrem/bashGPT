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
        Assert.NotEmpty(_agent.SystemPrompt);
        Assert.All(_agent.SystemPrompt, p => Assert.False(string.IsNullOrWhiteSpace(p)));
    }

    [Fact]
    public void ShellAgent_GetInfoPanelMarkdown_ContainsH1AndToolTable()
    {
        var md = _agent.GetInfoPanelMarkdown();

        Assert.Contains("# Shell-Agent", md, StringComparison.Ordinal);
        Assert.Contains("| `shell_exec`", md, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellAgent_GetInfoPanelMarkdown_ContainsLlmConfigSection()
    {
        var md = _agent.GetInfoPanelMarkdown();

        Assert.Contains("## LLM-Konfiguration", md, StringComparison.Ordinal);
        Assert.Contains("`temperature`", md, StringComparison.Ordinal);
        Assert.Contains("`stream`", md, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellAgent_LlmConfig_HasExpectedValues()
    {
        var cfg = _agent.LlmConfig;

        Assert.NotNull(cfg);
        Assert.Equal(0.1, cfg.Temperature);
        Assert.Equal(0.9, cfg.TopP);
        Assert.True(cfg.Stream);
    }
}
