namespace bashGPT.Core.Providers;

public record ToolCall(string? Id, string Name, string ArgumentsJson, int? Index = null);
