using System.Text.Json.Serialization;

namespace bashGPT.Core.Providers.Ollama;

internal sealed class OpenAiCompatibleOllamaOptions
{
    [JsonPropertyName("num_ctx")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumCtx { get; set; }
}
