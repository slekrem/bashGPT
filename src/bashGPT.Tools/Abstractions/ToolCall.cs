namespace BashGPT.Tools.Abstractions;

public sealed record ToolCall(
    string Name,
    string ArgumentsJson);
