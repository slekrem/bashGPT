namespace BashGPT.Tools.Abstractions;

/// <summary>
/// Describes a tool as it is advertised to the model.
/// </summary>
/// <param name="Name">Stable tool name used for registration and model calls.</param>
/// <param name="Description">Human-readable description of the tool capability.</param>
/// <param name="Parameters">Structured parameter definitions exposed to the model.</param>
public sealed record ToolDefinition(
    string Name,
    string Description,
    IReadOnlyList<ToolParameter> Parameters);
