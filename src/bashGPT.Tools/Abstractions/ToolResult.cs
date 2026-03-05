namespace BashGPT.Tools.Abstractions;

public sealed record ToolResult(
    bool Success,
    string Content);
