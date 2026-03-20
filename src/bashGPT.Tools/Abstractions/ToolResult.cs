namespace bashGPT.Tools.Abstractions;

/// <summary>
/// Represents the output of a tool execution.
/// </summary>
/// <param name="Success">Whether the tool execution succeeded.</param>
/// <param name="Content">Tool output that should be returned to the model.</param>
public sealed record ToolResult(
    bool Success,
    string Content);
