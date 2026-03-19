using System.Text.Json.Serialization;

namespace bashGPT.Core.Providers.Ollama;

internal sealed class OpenAiCompatibleUsage
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
    [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
    [JsonPropertyName("total_tokens")] public int? TotalTokens { get; set; }
    [JsonPropertyName("prompt_tokens_details")] public OpenAiCompatiblePromptTokensDetails? PromptTokensDetails { get; set; }
}
