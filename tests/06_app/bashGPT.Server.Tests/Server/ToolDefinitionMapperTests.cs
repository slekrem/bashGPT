using bashGPT.Agents;
using bashGPT.Tools.Abstractions;
using bashGPT.Tools.Registration;
using bashGPT.Server.Services;

namespace bashGPT.Server.Tests;

/// <summary>
/// Unit tests for <see cref="ToolDefinitionMapper"/>.
/// </summary>
public sealed class ToolDefinitionMapperTests
{
    // ── null / empty ────────────────────────────────────────────────────────

    [Fact]
    public void ResolveDefinitions_NullNames_ReturnsEmpty()
    {
        var result = ToolDefinitionMapper.ResolveDefinitions(null, registry: null);

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveDefinitions_EmptyNames_ReturnsEmpty()
    {
        var result = ToolDefinitionMapper.ResolveDefinitions([], registry: null);

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveDefinitions_NameNotInRegistryAndNoAgent_Skips()
    {
        var registry = new ToolRegistry();

        var result = ToolDefinitionMapper.ResolveDefinitions(["unknown_tool"], registry);

        Assert.Empty(result);
    }

    // ── registry lookup ─────────────────────────────────────────────────────

    [Fact]
    public void ResolveDefinitions_NameInRegistry_ReturnsMappedDefinition()
    {
        var tool = new StubTool("my_tool", "Does something", []);
        var registry = new ToolRegistry([tool]);

        var result = ToolDefinitionMapper.ResolveDefinitions(["my_tool"], registry);

        Assert.Single(result);
        Assert.Equal("my_tool", result[0].Name);
        Assert.Equal("Does something", result[0].Description);
    }

    [Fact]
    public void ResolveDefinitions_MultipleNamesInRegistry_ReturnsAll()
    {
        var tools = new ITool[]
        {
            new StubTool("tool_a", "A", []),
            new StubTool("tool_b", "B", []),
        };
        var registry = new ToolRegistry(tools);

        var result = ToolDefinitionMapper.ResolveDefinitions(["tool_a", "tool_b"], registry);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Name == "tool_a");
        Assert.Contains(result, t => t.Name == "tool_b");
    }

    // ── agent ownership priority ─────────────────────────────────────────────

    [Fact]
    public void ResolveDefinitions_AgentOwnedToolTakesPrecedenceOverRegistry()
    {
        var agentTool = new StubTool("shared_tool", "from agent", []);
        var registryTool = new StubTool("shared_tool", "from registry", []);

        var agent = new StubAgent([agentTool]);
        var registry = new ToolRegistry([registryTool]);

        var result = ToolDefinitionMapper.ResolveDefinitions(["shared_tool"], registry, agent);

        Assert.Single(result);
        Assert.Equal("from agent", result[0].Description);
    }

    [Fact]
    public void ResolveDefinitions_AgentDoesNotOwnTool_FallsBackToRegistry()
    {
        var registryTool = new StubTool("registry_only", "from registry", []);
        var agent = new StubAgent([]);
        var registry = new ToolRegistry([registryTool]);

        var result = ToolDefinitionMapper.ResolveDefinitions(["registry_only"], registry, agent);

        Assert.Single(result);
        Assert.Equal("from registry", result[0].Description);
    }

    // ── schema generation ────────────────────────────────────────────────────

    [Fact]
    public void ResolveDefinitions_RequiredParameter_AppearsInRequired()
    {
        var parameters = new[]
        {
            new ToolParameter("path", "string", "File path", Required: true),
        };
        var tool = new StubTool("fs_read", "Read file", parameters);
        var registry = new ToolRegistry([tool]);

        var result = ToolDefinitionMapper.ResolveDefinitions(["fs_read"], registry);

        var schema = result[0].Parameters;
        var json = System.Text.Json.JsonSerializer.Serialize(schema);
        Assert.Contains("\"required\"", json);
        Assert.Contains("path", json);
    }

    [Fact]
    public void ResolveDefinitions_OptionalParameter_NotInRequired()
    {
        var parameters = new[]
        {
            new ToolParameter("filter", "string", "Filter expression", Required: false),
        };
        var tool = new StubTool("search", "Search", parameters);
        var registry = new ToolRegistry([tool]);

        var result = ToolDefinitionMapper.ResolveDefinitions(["search"], registry);

        var schema = result[0].Parameters;
        var json = System.Text.Json.JsonSerializer.Serialize(schema);
        // required array should be empty (no required fields)
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var required = doc.RootElement.GetProperty("required");
        Assert.Equal(0, required.GetArrayLength());
    }

    [Fact]
    public void ResolveDefinitions_ObjectTypeParameter_HasAdditionalProperties()
    {
        var parameters = new[]
        {
            new ToolParameter("env", "object", "Environment variables", Required: false),
        };
        var tool = new StubTool("run", "Run command", parameters);
        var registry = new ToolRegistry([tool]);

        var result = ToolDefinitionMapper.ResolveDefinitions(["run"], registry);

        var json = System.Text.Json.JsonSerializer.Serialize(result[0].Parameters);
        Assert.Contains("additionalProperties", json);
    }

    [Fact]
    public void ResolveDefinitions_StringTypeParameter_NoAdditionalProperties()
    {
        var parameters = new[]
        {
            new ToolParameter("cmd", "string", "Command to run", Required: true),
        };
        var tool = new StubTool("shell", "Shell exec", parameters);
        var registry = new ToolRegistry([tool]);

        var result = ToolDefinitionMapper.ResolveDefinitions(["shell"], registry);

        var json = System.Text.Json.JsonSerializer.Serialize(result[0].Parameters);
        Assert.DoesNotContain("additionalProperties", json);
    }
}

// ── test helpers ─────────────────────────────────────────────────────────────

internal sealed class StubTool(string name, string description, IReadOnlyList<ToolParameter> parameters) : ITool
{
    public ToolDefinition Definition { get; } = new(name, description, parameters);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct) =>
        Task.FromResult(new ToolResult(true, "stub"));
}

internal sealed class StubAgent(IReadOnlyList<ITool> ownedTools) : AgentBase
{
    public override string Id => "stub-agent";
    public override string Name => "Stub Agent";
    public override IReadOnlyList<string> SystemPrompt => ["You are a stub."];
    public override IReadOnlyList<ITool> GetOwnedTools() => ownedTools;
    protected override string GetAgentMarkdown() => "# Stub";
}
