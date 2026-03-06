using BashGPT.Agents;
using BashGPT.Agents.Dev;

namespace BashGPT.Agents.Dev.Tests;

public class DevAgentCheckTests
{
    [Fact]
    public async Task RunAsync_NoProvider_ReturnsError()
    {
        var check = new DevAgentCheck(provider: null);
        var agent = new AgentRecord { Id = "1", Name = "test", LoopInstruction = "do something", Path = "/tmp" };

        var result = await check.RunAsync(agent, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Provider", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_NoLoopInstruction_ReturnsError()
    {
        var check = new DevAgentCheck(provider: null);
        var agent = new AgentRecord { Id = "1", Name = "test", LoopInstruction = null, Path = "/tmp" };

        var result = await check.RunAsync(agent, CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RunAsync_NoPath_ReturnsError()
    {
        var check = new DevAgentCheck(provider: null);
        var agent = new AgentRecord { Id = "1", Name = "test", LoopInstruction = "do something", Path = null };

        var result = await check.RunAsync(agent, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Repository", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Type_IsDevAgent()
    {
        var check = new DevAgentCheck(provider: null);
        Assert.Equal(AgentCheckType.DevAgent, check.Type);
    }
}
