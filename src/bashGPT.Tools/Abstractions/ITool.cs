namespace BashGPT.Tools.Abstractions;

/// <summary>
/// Defines a runtime tool that can be exposed to an LLM through the server.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Gets the public definition that is exposed to the model.
    /// </summary>
    ToolDefinition Definition { get; }

    /// <summary>
    /// Executes the tool call and returns the result that should be sent back to the model.
    /// </summary>
    Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct);
}
