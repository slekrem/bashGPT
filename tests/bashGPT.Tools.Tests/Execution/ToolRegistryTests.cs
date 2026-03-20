using BashGPT.Tools.Builtins;
using BashGPT.Tools.Registration;

namespace BashGPT.Tools.Tests.Execution;

public sealed class ToolRegistryTests
{
    [Fact]
    public async Task Register_AndExecute_NoOpTool_Works()
    {
        var registry = new ToolRegistry();
        registry.Register(new NoOpTool());

        var found = registry.TryGet("noop", out var tool);

        Assert.True(found);
        Assert.NotNull(tool);

        var result = await tool!.ExecuteAsync(new("noop", "{}"), CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal("noop", result.Content);
    }

    [Fact]
    public void Register_DuplicateName_Throws()
    {
        var registry = new ToolRegistry();
        registry.Register(new NoOpTool());

        var act = () => registry.Register(new NoOpTool());

        Assert.Throws<InvalidOperationException>(act);
    }
}
