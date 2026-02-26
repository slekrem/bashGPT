namespace BashGPT.Providers;

public enum ChatRole
{
    System,
    User,
    Assistant,
    Tool
}

public record ChatMessage(
    ChatRole Role,
    string Content,
    IReadOnlyList<ToolCall>? ToolCalls = null,
    string? ToolCallId = null,
    string? ToolName = null)
{
    public string RoleString => Role switch
    {
        ChatRole.System    => "system",
        ChatRole.User      => "user",
        ChatRole.Assistant => "assistant",
        ChatRole.Tool      => "tool",
        _                  => throw new ArgumentOutOfRangeException()
    };

    public static ChatMessage AssistantWithToolCalls(
        IReadOnlyList<ToolCall> toolCalls,
        string content = "") =>
        new(ChatRole.Assistant, content, ToolCalls: toolCalls);

    public static ChatMessage ToolResult(
        string content,
        string? toolCallId = null,
        string? toolName = null) =>
        new(ChatRole.Tool, content, ToolCalls: null, ToolCallId: toolCallId, ToolName: toolName);
}
