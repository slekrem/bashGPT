namespace bashGPT.Server.Models;

public sealed record SettingsRequest(
    string? Provider,
    string? Model,
    string? OllamaHost,
    ProviderConfigRequest? Ollama);

public sealed record ProviderConfigRequest(string? Model, string? Host);
