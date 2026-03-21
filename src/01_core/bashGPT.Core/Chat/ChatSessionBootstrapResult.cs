using bashGPT.Core.Configuration;
using bashGPT.Core.Providers.Abstractions;

namespace bashGPT.Core.Chat;

public sealed record ChatSessionBootstrapResult(
    AppConfig? Config,
    ILlmProvider? Provider,
    ChatSessionState? Session,
    string? Error);
