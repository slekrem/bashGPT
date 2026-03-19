namespace bashGPT.Server;

public interface IChatHandler
{
    Task<ServerChatResult> RunServerChatAsync(ServerChatOptions opts, CancellationToken ct = default);
}
