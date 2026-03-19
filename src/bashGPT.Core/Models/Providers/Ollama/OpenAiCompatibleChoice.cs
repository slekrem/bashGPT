using System.Text.Json.Serialization;

namespace bashGPT.Core.Models.Providers.Ollama;

internal sealed class OpenAiCompatibleChoice
{
    [JsonPropertyName("delta")] public OpenAiCompatibleDelta? Delta { get; set; }
}
