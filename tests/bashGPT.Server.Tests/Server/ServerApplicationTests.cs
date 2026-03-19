using bashGPT.Server;
using BashGPT.Tools.Execution;

namespace bashGPT.Server.Tests;

public sealed class ServerApplicationTests
{
    [Fact]
    public void CreateToolRegistry_ReturnsRegistryWithExpectedDefaultTools()
    {
        var registry = ServerApplication.CreateToolRegistry();

        Assert.NotNull(registry);
        Assert.True(registry.TryGet("shell_exec", out _));
        Assert.True(registry.TryGet("fetch", out _));
        Assert.True(registry.TryGet("filesystem_read", out _));
    }

    [Fact]
    public void CreateAgentRegistry_ReturnsRegistryWithExpectedAgents()
    {
        var registry = ServerApplication.CreateAgentRegistry();

        Assert.NotNull(registry);
        Assert.NotNull(registry.Get("generic"));
        Assert.NotNull(registry.Get("dev"));
        Assert.NotNull(registry.Get("shell"));
    }

    [Fact]
    public void CreateServerHost_ReturnsConfiguredServerHost()
    {
        var configService = new TestConfigurationService(Path.GetTempFileName());
        var toolRegistry = new ToolRegistry([]);

        var host = ServerApplication.CreateServerHost(configService, toolRegistry);

        Assert.NotNull(host);
        Assert.IsType<ServerHost>(host);
    }
}
