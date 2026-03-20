namespace bashGPT.Tools.Abstractions;

/// <summary>
/// Represents a concrete runtime invocation of a tool.
/// </summary>
/// <param name="Name">Registered tool name.</param>
/// <param name="ArgumentsJson">Raw JSON arguments provided by the model.</param>
/// <param name="SessionPath">Optional session-scoped path provided by the server runtime.</param>
public sealed record ToolCall(
    string Name,
    string ArgumentsJson,
    string? SessionPath = null);
