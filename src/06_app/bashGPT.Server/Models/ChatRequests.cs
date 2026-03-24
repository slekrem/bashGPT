namespace bashGPT.Server.Models;

public sealed record ChatRequest(
    string Prompt,
    bool? Verbose = null,
    string? SessionId = null,
    string[]? EnabledTools = null,
    string? AgentId = null);

public sealed record StreamChatRequest(
    string Prompt,
    bool? Verbose = null,
    string? SessionId = null,
    string[]? EnabledTools = null,
    string? AgentId = null,
    string? RequestId = null);

public sealed record CancelRequest(string RequestId);
