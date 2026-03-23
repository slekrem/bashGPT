using bashGPT.Agents;
using bashGPT.Server;
using bashGPT.Tools.Abstractions;
using bashGPT.Tools.Registration;

namespace bashGPT.Server.Tests;

public sealed class ServerApplicationTests
{
    [Fact]
    public void CreateToolRegistry_ReturnsEmptyRegistryByDefault()
    {
        var registry = ServerApplication.CreateToolRegistry();

        Assert.NotNull(registry);
        // Tools are now loaded as plugins, not built-ins.
        Assert.False(registry.TryGet("shell_exec", out _));
    }

    [Fact]
    public void CreateToolRegistry_WithAdditionalTools_RegistersCustomTools()
    {
        var registry = ServerApplication.CreateToolRegistry([new FakeTool("custom_tool")]);

        Assert.True(registry.TryGet("custom_tool", out var tool));
        Assert.NotNull(tool);
    }

    [Fact]
    public void CreateToolRegistry_WithDuplicateToolName_SkipsDuplicate()
    {
        var registry = ServerApplication.CreateToolRegistry([
            new FakeTool("my_tool"),
            new FakeTool("my_tool"),
        ]);

        Assert.True(registry.TryGet("my_tool", out var tool));
        Assert.NotNull(tool);
    }

    [Fact]
    public void CreateAgentRegistry_ReturnsRegistryWithGenericBuiltin()
    {
        var registry = ServerApplication.CreateAgentRegistry();

        Assert.NotNull(registry);
        Assert.NotNull(registry.Find("generic"));
        // dev and shell are now loaded as plugins, not built-ins.
        Assert.Null(registry.Find("dev"));
        Assert.Null(registry.Find("shell"));
    }

    [Fact]
    public void CreateAgentRegistry_WithAdditionalAgents_RegistersPluginAgents()
    {
        var registry = ServerApplication.CreateAgentRegistry([new FakeAgent("plugin-agent")]);

        Assert.NotNull(registry.Find("plugin-agent"));
        Assert.NotNull(registry.Find("generic"));
    }

    [Fact]
    public void CreateAgentRegistry_WithDuplicateBuiltInAgentId_SkipsDuplicate()
    {
        var registry = ServerApplication.CreateAgentRegistry([new FakeAgent("generic")]);

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
