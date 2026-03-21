using bashGPT.Agents.Shell;

namespace bashGPT.Agents.Tests;

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
    public void ShellAgent_EnabledTools_ContainsExactlyOneShellTool()
    {
        Assert.Single(_agent.EnabledTools);
        var toolName = _agent.EnabledTools[0];
        Assert.True(
            toolName is "shell_exec" or "bash_exec" or "cmd_exec" or "pwsh_exec",
            $"Unexpected tool name: {toolName}");
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
        var md       = _agent.GetInfoPanelMarkdown();
        var toolName = _agent.EnabledTools[0];

        Assert.Contains("# Shell-Agent", md, StringComparison.Ordinal);
        Assert.Contains($"| `{toolName}`", md, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellAgent_GetInfoPanelMarkdown_ContainsLlmConfigSection()
    {
        var md = _agent.GetInfoPanelMarkdown();

        Assert.Contains("## LLM Configuration", md, StringComparison.Ordinal);
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
