using System.Text.Json.Serialization;

namespace bashGPT.Core.Models.Providers.Ollama;

internal sealed class OpenAiCompatibleStreamOptions
{
    [JsonPropertyName("include_usage")] public bool IncludeUsage { get; set; } = true;
}
