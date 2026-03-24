using bashGPT.Core.Models.Providers;
using bashGPT.Core.Models.Storage;

namespace bashGPT.Server.Services;

/// <summary>
/// Converts between <see cref="SessionMessage"/> persistence models and
/// <see cref="ChatMessage"/> provider-facing models.
/// </summary>
internal static class SessionMessageMapper
{
    /// <summary>
    /// Converts a stored <see cref="SessionMessage"/> into a <see cref="ChatMessage"/>.
    /// Returns null when the role is unknown, for example <c>system</c>.
    /// </summary>
    public static ChatMessage? ToChatMessage(SessionMessage m) => m.Role switch
    {
        "user" => new ChatMessage(ChatRole.User, m.Content),
        "assistant" => m.ToolCalls is { Count: > 0 }
            ? ChatMessage.AssistantWithToolCalls(
                m.ToolCalls.Select(tc => new ToolCall(tc.Id, tc.Name, tc.ArgumentsJson)).ToList(),
                m.Content)
            : new ChatMessage(ChatRole.Assistant, m.Content),
        "tool" => ChatMessage.ToolResult(m.Content, m.ToolCallId, m.ToolName),
        _ => null,
    };

    /// <summary>
    /// Converts a <see cref="ChatMessage"/> into a stored <see cref="SessionMessage"/>.
    /// </summary>
    public static SessionMessage FromChatMessage(ChatMessage m) => m.Role switch
    {
        ChatRole.Assistant => new SessionMessage
        {
            Role = "assistant",
            Content = m.Content,
            ToolCalls = m.ToolCalls is { Count: > 0 }
                ? m.ToolCalls.Select(tc => new SessionToolCall
                {
                    Id = tc.Id,
                    Name = tc.Name,
                    ArgumentsJson = tc.ArgumentsJson,
                }).ToList()
                : null,
        },
        ChatRole.Tool => new SessionMessage
        {
            Role = "tool",
            Content = m.Content,
            ToolCallId = m.ToolCallId,
            ToolName = m.ToolName,
        },
        _ => new SessionMessage { Role = m.RoleString, Content = m.Content },
    };
}
