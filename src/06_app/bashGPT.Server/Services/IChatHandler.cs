using bashGPT.Server.Models;

namespace bashGPT.Server.Services;

public interface IChatHandler
{
    Task<ServerChatResult> RunServerChatAsync(ServerChatOptions opts, CancellationToken ct = default);
}
