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
    }
}
