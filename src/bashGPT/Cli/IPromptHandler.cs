namespace BashGPT.Cli;

public interface IPromptHandler
{
    Task<ServerChatResult> RunServerChatAsync(ServerChatOptions opts, CancellationToken ct = default);
}
