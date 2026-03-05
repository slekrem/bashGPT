namespace BashGPT.Tools.Abstractions;

public sealed record ToolDefinition(
    string Name,
    string Description,
    IReadOnlyList<ToolParameter> Parameters);
