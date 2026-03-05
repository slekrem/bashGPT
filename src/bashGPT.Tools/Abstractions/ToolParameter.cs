namespace BashGPT.Tools.Abstractions;

public sealed record ToolParameter(
    string Name,
    string Type,
    string Description,
    bool Required);
