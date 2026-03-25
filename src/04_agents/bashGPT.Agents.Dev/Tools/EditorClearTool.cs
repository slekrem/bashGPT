using bashGPT.Tools.Abstractions;

namespace bashGPT.Agents.Dev.Tools;

/// <summary>
/// Built-in dev agent tool: closes all files in the Editor at once.
/// </summary>
public sealed class EditorClearTool : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "editor_clear",
        Description: "Closes all open files in the Editor at once.",
        Parameters: []);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        var count = EditorState.ReadFiles(call.SessionPath).Count;
        EditorState.Clear(call.SessionPath);

        return Task.FromResult(new ToolResult(
            Success: true,
            Content: count > 0
                ? $"Editor cleared — {count} file(s) closed."
                : "Editor was already empty."));
    }
}
