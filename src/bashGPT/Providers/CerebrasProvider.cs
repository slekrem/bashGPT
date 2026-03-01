using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BashGPT.Configuration;

namespace BashGPT.Providers;

public class CerebrasProvider(CerebrasConfig config, HttpClient? httpClient = null) : ILlmProvider
{
    private readonly HttpClient _http = httpClient ?? CreateHttpClient();

    public string Name  => "Cerebras";
    public string Model => config.Model;

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            UseCookies = false
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<LlmChatResponse> ChatAsync(LlmChatRequest request, CancellationToken ct = default)
        => await ChatAsyncInternal(request, ct, allowToolChoiceFallback: true);

    private async Task<LlmChatResponse> ChatAsyncInternal(
        LlmChatRequest request,
        CancellationToken ct,
        bool allowToolChoiceFallback)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new LlmProviderException(
                "Kein Cerebras API-Key konfiguriert. " +
                "Setze ihn mit: bashgpt config set cerebras.apiKey <key> " +
                "oder per Umgebungsvariable BASHGPT_CEREBRAS_KEY.");

        var openAiRequest = new OpenAiChatRequest
        {
            Model    = config.Model,
            Messages = request.Messages.Select(MapMessage).ToList(),
            Stream   = request.Stream
        };

        if (request.Tools is { Count: > 0 })
        {
            openAiRequest.Tools = request.Tools.Select(MapTool).ToList();
            openAiRequest.ParallelToolCalls = request.ParallelToolCalls;
            if (!string.IsNullOrWhiteSpace(request.ToolChoiceName))
                openAiRequest.ToolChoice = OpenAiToolChoice.ForFunction(request.ToolChoiceName!);
        }

        // Serialisierung einmalig außerhalb der Retry-Schleife
        var serialized = JsonSerializer.Serialize(openAiRequest, JsonDefaults.Options);
        var url = $"{config.BaseUrl.TrimEnd('/')}/chat/completions";

        const int maxRetries = 3;
        HttpResponseMessage response = null!;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(serialized, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

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

            if (response.IsSuccessStatusCode)
                break;

            var body = await response.Content.ReadAsStringAsync(ct);

            // Einige Modelle lehnen erzwungenes tool_choice in Randfällen mit 422 ab.
            // In dem Fall versuchen wir exakt einmal den gleichen Request ohne tool_choice.
            if (allowToolChoiceFallback &&
                (int)response.StatusCode == 422 &&
                request.ToolChoiceName is not null &&
                body.Contains("wrong_api_format", StringComparison.OrdinalIgnoreCase))
            {
                var fallbackRequest = request with { ToolChoiceName = null };
                return await ChatAsyncInternal(fallbackRequest, ct, allowToolChoiceFallback: false);
            }

            // Bei 429 automatisch wiederholen (bis maxRetries erschöpft sind)
            if ((int)response.StatusCode == 429 && attempt < maxRetries)
            {
                var delay = GetRetryDelay(response, attempt);
                await Task.Delay(delay, ct);
                continue;
            }

            var hint = (int)response.StatusCode switch
            {
                401 => " → API-Key ungültig oder abgelaufen.",
                429 => $" → Rate-Limit nach {maxRetries} Versuchen weiterhin aktiv.",
                _   => ""
            };
            throw new LlmProviderException(
                $"Cerebras API antwortete mit HTTP {(int)response.StatusCode}{hint}\n{body}");
        }

        if (!request.Stream)
        {
            var json = await response.Content.ReadAsStringAsync(ct);
            var full = JsonSerializer.Deserialize<OpenAiChatResponse>(json, JsonDefaults.Options);
            var message = full?.Choices?.FirstOrDefault()?.Message;
            var content = message?.Content ?? "";
            var toolCalls = message?.ToolCalls?.Select(MapToolCall).ToList() ?? [];
            if (!string.IsNullOrEmpty(content))
                request.OnToken?.Invoke(content);
            return new LlmChatResponse(content, toolCalls);
        }

        var contentBuilder = new StringBuilder();
        var toolBuilder = new Dictionary<int, ToolCallBuilder>();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            var json = line["data:".Length..].Trim();
            if (json == "[DONE]") break;

            OpenAiStreamChunk? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OpenAiStreamChunk>(json, JsonDefaults.Options);
            }
            catch (JsonException)
            {
                continue;
            }

            var delta = chunk?.Choices?.FirstOrDefault()?.Delta;
            var content = delta?.Content;
            if (!string.IsNullOrEmpty(content))
            {
                request.OnToken?.Invoke(content);
                contentBuilder.Append(content);
            }

            if (delta?.ToolCalls is { Count: > 0 })
            {
                foreach (var toolDelta in delta.ToolCalls)
                    ApplyToolDelta(toolBuilder, toolDelta);
            }
        }

        var toolCallsFinal = toolBuilder
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value.ToToolCall())
            .ToList();

        return new LlmChatResponse(contentBuilder.ToString(), toolCallsFinal);
    }

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
        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<OpenAiTool>? Tools { get; set; }
        [JsonPropertyName("tool_choice")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OpenAiToolChoice? ToolChoice { get; set; }
        [JsonPropertyName("parallel_tool_calls")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? ParallelToolCalls { get; set; }
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("role")]    public string Role    { get; set; } = "";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
        [JsonPropertyName("tool_calls")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<OpenAiToolCall>? ToolCalls { get; set; }
        [JsonPropertyName("tool_call_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ToolCallId { get; set; }
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
        [JsonPropertyName("tool_calls")] public List<OpenAiToolCallDelta>? ToolCalls { get; set; }
    }

    private sealed class OpenAiChatResponse
    {
        [JsonPropertyName("choices")] public List<OpenAiChatChoice>? Choices { get; set; }
    }

    private sealed class OpenAiChatChoice
    {
        [JsonPropertyName("message")] public OpenAiMessage? Message { get; set; }
    }

    private sealed class OpenAiTool
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "function";
        [JsonPropertyName("function")] public OpenAiToolFunction Function { get; set; } = new();
    }

    private sealed class OpenAiToolFunction
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("parameters")] public object Parameters { get; set; } = new();
    }

    private sealed class OpenAiToolChoice
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "function";
        [JsonPropertyName("function")] public OpenAiToolChoiceFunction Function { get; set; } = new();

        public static OpenAiToolChoice ForFunction(string name) =>
            new() { Function = new OpenAiToolChoiceFunction { Name = name } };
    }

    private sealed class OpenAiToolChoiceFunction
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
    }

    private sealed class OpenAiToolCall
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "function";
        [JsonPropertyName("function")] public OpenAiToolCallFunction Function { get; set; } = new();
    }

    private sealed class OpenAiToolCallDelta
    {
        [JsonPropertyName("index")] public int Index { get; set; }
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("function")] public OpenAiToolCallFunction? Function { get; set; }
    }

    private sealed class OpenAiToolCallFunction
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("arguments")] public string? Arguments { get; set; }
    }

    private sealed class ToolCallBuilder
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public StringBuilder Arguments { get; } = new();

        public int Index { get; init; }

        public ToolCall ToToolCall() =>
            new(Id, Name ?? "", Arguments.ToString(), Index);
    }

    private static OpenAiMessage MapMessage(ChatMessage msg)
    {
        var message = new OpenAiMessage
        {
            Role    = msg.RoleString,
            Content = msg.Content
        };

        if (msg.ToolCalls is { Count: > 0 })
            message.ToolCalls = msg.ToolCalls.Select(MapToolCallDto).ToList();

        if (!string.IsNullOrWhiteSpace(msg.ToolCallId))
            message.ToolCallId = msg.ToolCallId;

        return message;
    }

    private static OpenAiTool MapTool(ToolDefinition tool) =>
        new()
        {
            Type = "function",
            Function = new OpenAiToolFunction
            {
                Name        = tool.Name,
                Description = tool.Description,
                Parameters  = tool.Parameters
            }
        };

    private static OpenAiToolCall MapToolCallDto(ToolCall call) =>
        new()
        {
            Id = call.Id ?? "",
            Type = "function",
            Function = new OpenAiToolCallFunction
            {
                Name = call.Name,
                Arguments = call.ArgumentsJson
            }
        };

    private static ToolCall MapToolCall(OpenAiToolCall call) =>
        new(call.Id, call.Function.Name ?? "", call.Function.Arguments ?? "", null);

    /// Liest den Retry-After-Header (Sekunden) aus der 429-Antwort.
    /// Fallback: exponentielles Backoff (2s, 4s, 8s), maximal 10s.
    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
            return delta < TimeSpan.FromSeconds(30) ? delta : TimeSpan.FromSeconds(30);

        var seconds = Math.Min(Math.Pow(2, attempt + 1), 10);
        return TimeSpan.FromSeconds(seconds);
    }

    private static void ApplyToolDelta(
        Dictionary<int, ToolCallBuilder> builder,
        OpenAiToolCallDelta delta)
    {
        if (!builder.TryGetValue(delta.Index, out var item))
        {
            item = new ToolCallBuilder { Index = delta.Index };
            builder[delta.Index] = item;
        }

        if (!string.IsNullOrWhiteSpace(delta.Id))
            item.Id = delta.Id;

        if (!string.IsNullOrWhiteSpace(delta.Function?.Name))
            item.Name = delta.Function.Name;

        if (!string.IsNullOrWhiteSpace(delta.Function?.Arguments))
            item.Arguments.Append(delta.Function.Arguments);
    }
}
