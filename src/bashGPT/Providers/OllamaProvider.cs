using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using BashGPT.Configuration;

namespace BashGPT.Providers;

public class OllamaProvider(OllamaConfig config, HttpClient? httpClient = null) : ILlmProvider
{
    private readonly HttpClient _http = httpClient ?? new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    public string Name  => "Ollama";
    public string Model => config.Model;

    public async Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var sb = new System.Text.StringBuilder();
        await foreach (var token in StreamAsync(messages, ct))
            sb.Append(token);
        return sb.ToString();
    }

    public async IAsyncEnumerable<string> StreamAsync(
        IEnumerable<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new OllamaChatRequest
        {
            Model    = config.Model,
            Messages = messages.Select(m => new OllamaMessage { Role = m.RoleString, Content = m.Content }).ToList(),
            Stream   = true
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(
                $"{config.BaseUrl.TrimEnd('/')}/api/chat", request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new LlmProviderException(
                $"Ollama ist nicht erreichbar unter '{config.BaseUrl}'. " +
                $"Läuft 'ollama serve'? (Details: {ex.Message})", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new LlmProviderException(
                $"Timeout beim Verbinden mit Ollama ({config.BaseUrl}).", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new LlmProviderException(
                $"Ollama antwortete mit HTTP {(int)response.StatusCode}: {body}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            OllamaChatResponse? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, JsonDefaults.Options);
            }
            catch (JsonException)
            {
                continue;
            }

            if (chunk is null) continue;

            var content = chunk.Message?.Content;
            if (!string.IsNullOrEmpty(content))
                yield return content;

            if (chunk.Done) yield break;
        }
    }

    // ── DTOs ────────────────────────────────────────────────────────────────

    private sealed class OllamaChatRequest
    {
        [JsonPropertyName("model")]    public string Model    { get; set; } = "";
        [JsonPropertyName("messages")] public List<OllamaMessage> Messages { get; set; } = [];
        [JsonPropertyName("stream")]   public bool   Stream   { get; set; } = true;
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("role")]    public string Role    { get; set; } = "";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")] public OllamaMessage? Message { get; set; }
        [JsonPropertyName("done")]    public bool Done               { get; set; }
    }
}
