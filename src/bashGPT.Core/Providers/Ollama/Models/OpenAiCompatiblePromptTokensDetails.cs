using System.Text.Json.Serialization;

namespace bashGPT.Core.Providers.Ollama;

internal sealed class OpenAiCompatiblePromptTokensDetails
{
    [JsonPropertyName("cached_tokens")] public int? CachedTokens { get; set; }
}
