namespace BashGPT.Providers;

public enum ChatRole
{
    System,
    User,
    Assistant
}

public record ChatMessage(ChatRole Role, string Content)
{
    public string RoleString => Role switch
    {
        ChatRole.System    => "system",
        ChatRole.User      => "user",
        ChatRole.Assistant => "assistant",
        _                  => throw new ArgumentOutOfRangeException()
    };
}
