using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BashGPT.Configuration;

namespace BashGPT.Providers;

public class CerebrasProvider(CerebrasConfig config, HttpClient? httpClient = null) : ILlmProvider
{
    private readonly HttpClient _http = httpClient ?? new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    public string Name  => "Cerebras";
    public string Model => config.Model;

    public async Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        await foreach (var token in StreamAsync(messages, ct))
            sb.Append(token);
        return sb.ToString();
    }

    public async IAsyncEnumerable<string> StreamAsync(
        IEnumerable<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new LlmProviderException(
                "Kein Cerebras API-Key konfiguriert. " +
                "Setze ihn mit: bashgpt config set cerebras.apiKey <key> " +
                "oder per Umgebungsvariable BASHGPT_CEREBRAS_KEY.");

        var request = new OpenAiChatRequest
        {
            Model    = config.Model,
            Messages = messages.Select(m => new OpenAiMessage { Role = m.RoleString, Content = m.Content }).ToList(),
            Stream   = true
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{config.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(request, JsonDefaults.Options),
                Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(httpRequest,
                HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new LlmProviderException(
                $"Cerebras API nicht erreichbar ({config.BaseUrl}): {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new LlmProviderException("Timeout beim Verbinden mit Cerebras API.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var hint = (int)response.StatusCode switch
            {
                401 => " → API-Key ungültig oder abgelaufen.",
                429 => " → Rate-Limit erreicht. Bitte warte kurz.",
                _   => ""
            };
            throw new LlmProviderException(
                $"Cerebras API antwortete mit HTTP {(int)response.StatusCode}{hint}\n{body}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            // SSE-Format: "data: {...}" oder "data: [DONE]"
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            var json = line["data:".Length..].Trim();
            if (json == "[DONE]") yield break;

            OpenAiStreamChunk? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OpenAiStreamChunk>(json, JsonDefaults.Options);
            }
            catch (JsonException)
            {
                continue;
            }

            var content = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(content))
                yield return content;
        }
    }

    // ── DTOs ────────────────────────────────────────────────────────────────

    private sealed class OpenAiChatRequest
    {
        [JsonPropertyName("model")]    public string Model    { get; set; } = "";
        [JsonPropertyName("messages")] public List<OpenAiMessage> Messages { get; set; } = [];
        [JsonPropertyName("stream")]   public bool   Stream   { get; set; } = true;
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("role")]    public string Role    { get; set; } = "";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }

    private sealed class OpenAiStreamChunk
    {
        [JsonPropertyName("choices")] public List<OpenAiChoice>? Choices { get; set; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("delta")] public OpenAiDelta? Delta { get; set; }
    }

    private sealed class OpenAiDelta
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}
