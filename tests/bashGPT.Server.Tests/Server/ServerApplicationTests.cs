using bashGPT.Server;
using bashGPT.Tools.Abstractions;
using bashGPT.Tools.Registration;

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
    public void CreateDefaultTools_ReturnsExpectedBuiltInTools()
    {
        var tools = ServerApplication.CreateDefaultTools();

        Assert.Contains(tools, tool => tool.Definition.Name == "shell_exec");
        Assert.Contains(tools, tool => tool.Definition.Name == "fetch");
        Assert.Contains(tools, tool => tool.Definition.Name == "filesystem_read");
    }

    [Fact]
    public void CreateToolRegistry_WithAdditionalTools_RegistersCustomTools()
    {
        var registry = ServerApplication.CreateToolRegistry([new FakeTool("custom_tool")]);

        Assert.True(registry.TryGet("custom_tool", out var tool));
        Assert.NotNull(tool);
    }

    [Fact]
    public void CreateAgentRegistry_ReturnsRegistryWithExpectedAgents()
    {
        var registry = ServerApplication.CreateAgentRegistry();

        Assert.NotNull(registry);
        Assert.NotNull(registry.Find("generic"));
        Assert.NotNull(registry.Find("dev"));
        Assert.NotNull(registry.Find("shell"));
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

    private sealed class FakeTool(string name) : ITool
    {
        public ToolDefinition Definition { get; } = new(name, "Fake custom tool", []);

        public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct) =>
            Task.FromResult(new ToolResult(true, "ok"));
    }
}
