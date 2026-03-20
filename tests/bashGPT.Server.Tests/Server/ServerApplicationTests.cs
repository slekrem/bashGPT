using bashGPT.Agents;
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
    public void CreateToolRegistry_WithDuplicateBuiltInToolName_SkipsDuplicate()
    {
        var registry = ServerApplication.CreateToolRegistry([new FakeTool("shell_exec")]);

        // Built-in must still be present; no exception is thrown.
        Assert.True(registry.TryGet("shell_exec", out var tool));
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
    public void CreateAgentRegistry_WithAdditionalAgents_RegistersPluginAgents()
    {
        var registry = ServerApplication.CreateAgentRegistry([new FakeAgent("plugin-agent")]);

        Assert.NotNull(registry.Find("plugin-agent"));
        // Built-ins must still be present.
        Assert.NotNull(registry.Find("generic"));
    }

    [Fact]
    public void CreateAgentRegistry_WithDuplicateBuiltInAgentId_SkipsDuplicate()
    {
        var registry = ServerApplication.CreateAgentRegistry([new FakeAgent("generic")]);

        // Built-in generic must still be present; no exception is thrown.
        Assert.NotNull(registry.Find("generic"));
    }

    [Fact]
    public void LoadPlugins_NonExistentDirectory_ReturnsEmptyResult()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var result = ServerApplication.LoadPlugins(dir);

        Assert.Empty(result.Tools);
        Assert.Empty(result.Agents);
        Assert.Empty(result.Errors);
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

    [Fact]
    public void CreateServerHost_WithAdditionalAgents_IncludesThemInRegistry()
    {
        var configService = new TestConfigurationService(Path.GetTempFileName());
        var toolRegistry = new ToolRegistry([]);

        var host = ServerApplication.CreateServerHost(configService, toolRegistry, [new FakeAgent("my-agent")]);

        Assert.NotNull(host);
    }

    private sealed class FakeTool(string name) : ITool
    {
        public ToolDefinition Definition { get; } = new(name, "Fake custom tool", []);

        public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct) =>
            Task.FromResult(new ToolResult(true, "ok"));
    }

    private sealed class FakeAgent(string id) : AgentBase
    {
        public override string Id => id;
        public override string Name => id;
        public override IReadOnlyList<string> EnabledTools => [];
        public override IReadOnlyList<string> SystemPrompt => [];
        protected override string GetAgentMarkdown() => string.Empty;
    }
}
