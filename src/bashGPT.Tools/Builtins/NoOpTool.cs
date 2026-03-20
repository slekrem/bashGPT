using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Builtins;

public sealed class NoOpTool : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "noop",
        Description: "Simple test tool that echoes a static response.",
        Parameters: []);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
        => Task.FromResult(new ToolResult(Success: true, Content: "noop"));
}
