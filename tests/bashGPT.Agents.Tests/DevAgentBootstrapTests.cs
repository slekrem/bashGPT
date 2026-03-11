using BashGPT.Agents.Dev;

namespace BashGPT.Agents.Tests;

public class DevAgentBootstrapTests
{
    [Fact]
    public void DevAgent_HasExpectedToolingRules()
    {
        var dev = DevAgentBootstrap.DevAgent;

        Assert.Equal("dev", dev.Id);
        Assert.Equal("Dev-Agent", dev.Name);
        Assert.Contains("fetch", dev.EnabledTools);
        Assert.Contains("filesystem_search", dev.EnabledTools);
        Assert.Contains("shell_exec", dev.EnabledTools);
        Assert.Contains("Required Fields", dev.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("'pattern' Pflicht", dev.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("invalid_json", dev.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("missing_required_field", dev.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("invalid_type", dev.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("invalid_value", dev.SystemPrompt, StringComparison.Ordinal);
    }
}
