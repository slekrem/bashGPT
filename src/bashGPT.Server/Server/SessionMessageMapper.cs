using bashGPT.Core.Models.Providers;
using bashGPT.Core.Models.Storage;
using bashGPT.Core.Providers;

namespace BashGPT.Server;

/// <summary>
/// Konvertiert zwischen <see cref="SessionMessage"/> (Persistenz) und
/// <see cref="ChatMessage"/> (LLM-Provider-Interface).
/// </summary>
internal static class SessionMessageMapper
{
    /// <summary>
    /// Wandelt eine gespeicherte <see cref="SessionMessage"/> in eine <see cref="ChatMessage"/> um.
    /// Gibt null zurück, wenn die Rolle unbekannt ist (z.B. "system").
    /// </summary>
    public static ChatMessage? ToChatMessage(SessionMessage m) => m.Role switch
    {
        "user"      => new ChatMessage(ChatRole.User, m.Content),
        "assistant" => m.ToolCalls is { Count: > 0 }
            ? ChatMessage.AssistantWithToolCalls(
                m.ToolCalls.Select(tc => new ToolCall(tc.Id, tc.Name, tc.ArgumentsJson)).ToList(),
                m.Content)
            : new ChatMessage(ChatRole.Assistant, m.Content),
        "tool"      => ChatMessage.ToolResult(m.Content, m.ToolCallId, m.ToolName),
        _           => null,
    };

    /// <summary>
    /// Wandelt eine <see cref="ChatMessage"/> (Tool-Call-Runde) in eine <see cref="SessionMessage"/> um.
    /// </summary>
    public static SessionMessage FromChatMessage(ChatMessage m) => m.Role switch
    {
        ChatRole.Assistant => new SessionMessage
        {
            Role      = "assistant",
            Content   = m.Content,
            ToolCalls = m.ToolCalls is { Count: > 0 }
                ? m.ToolCalls.Select(tc => new SessionToolCall
                  {
                      Id            = tc.Id,
                      Name          = tc.Name,
                      ArgumentsJson = tc.ArgumentsJson,
                  }).ToList()
                : null,
        },
        ChatRole.Tool => new SessionMessage
        {
            Role       = "tool",
            Content    = m.Content,
            ToolCallId = m.ToolCallId,
            ToolName   = m.ToolName,
        },
        _ => new SessionMessage { Role = m.RoleString, Content = m.Content },
    };
}
