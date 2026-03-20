namespace bashGPT.Tools.Abstractions;

/// <summary>
/// Describes a single tool parameter that is exposed to the model.
/// </summary>
/// <param name="Name">Parameter name.</param>
/// <param name="Type">JSON-schema-like parameter type such as string, integer, object, or array.</param>
/// <param name="Description">Human-readable parameter description.</param>
/// <param name="Required">Whether the parameter is required.</param>
public sealed record ToolParameter(
    string Name,
    string Type,
    string Description,
    bool Required);
