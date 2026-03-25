using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: clears the entire session-scoped context cache.
/// </summary>
public sealed class ContextClearFilesTool : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "context_clear_files",
        Description: "Removes all loaded files from the context at once.",
        Parameters: []);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        var count = ContextFileCache.ReadFiles(call.SessionPath).Count;
        ContextFileCache.Clear(call.SessionPath);

        return Task.FromResult(new ToolResult(
            Success: true,
            Content: count > 0
                ? $"Context cleared — {count} file(s) removed."
                : "Context was already empty."));
    }
}
