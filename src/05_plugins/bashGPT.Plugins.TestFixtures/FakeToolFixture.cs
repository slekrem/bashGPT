using bashGPT.Tools.Abstractions;

namespace bashGPT.Plugins.TestFixtures;

/// <summary>
/// Minimal <see cref="ITool"/> implementation used only by plugin loader tests.
/// </summary>
public sealed class FakeToolFixture : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "fake_tool",
        Description: "A fake tool for plugin loader tests.",
        Parameters: []);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct) =>
        Task.FromResult(new ToolResult(true, "fake"));
}
