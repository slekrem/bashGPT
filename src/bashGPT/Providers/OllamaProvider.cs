using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using BashGPT.Configuration;

namespace BashGPT.Providers;

public class OllamaProvider(OllamaConfig config, HttpClient? httpClient = null)
    : BaseLlmProvider(httpClient)
{
    public override string Name  => "Ollama";
    public override string Model => config.Model;

    public override async Task<LlmChatResponse> ChatAsync(LlmChatRequest request, CancellationToken ct = default)
    {
        var ollamaRequest = new OllamaChatRequest
        {
            Model    = config.Model,
            Messages = request.Messages.Select(MapMessage).ToList(),
            Stream   = request.Stream,
            Options  = BuildOptions()
        };

        if (request.Tools is { Count: > 0 })
            ollamaRequest.Tools = request.Tools.Select(MapTool).ToList();

        HttpResponseMessage response;
        try
        {
            response = await Http.PostAsJsonAsync(
                $"{config.BaseUrl.TrimEnd('/')}/api/chat", ollamaRequest, ct);
        }
        catch (HttpRequestException ex)
        {
            throw WrapHttpException(ex, config.BaseUrl);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw WrapTimeoutException(ex, $"Ollama ({config.BaseUrl})");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new LlmProviderException(
                $"Ollama antwortete mit HTTP {(int)response.StatusCode}: {body}");
        }

        if (!request.Stream)
        {
            var json = await response.Content.ReadAsStringAsync(ct);
            var full = JsonSerializer.Deserialize<OllamaChatResponse>(json, JsonDefaults.Options);
            var content = full?.Message?.Content ?? "";
            var toolCalls = full?.Message?.ToolCalls?.Select(MapToolCall).ToList() ?? [];
            if (!string.IsNullOrEmpty(content))
                request.OnToken?.Invoke(content);
            var nonStreamUsage = full?.PromptEvalCount is int p && full?.EvalCount is int o
                ? new TokenUsage(p, o, p + o)
                : null;
            return new LlmChatResponse(content, toolCalls, nonStreamUsage);
        }

        var contentBuilder = new System.Text.StringBuilder();
        var toolCallsByIndex = new Dictionary<int, ToolCall>();
        var toolCallsNoIndex = new List<ToolCall>();
        int? promptTokens = null, outputTokens = null;

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
            {
                request.OnToken?.Invoke(content);
                contentBuilder.Append(content);
            }

            if (chunk.Message?.ToolCalls is { Count: > 0 })
            {
                foreach (var tc in chunk.Message.ToolCalls)
                {
                    var mapped = MapToolCall(tc);
                    if (mapped.Index is int idx)
                        toolCallsByIndex[idx] = mapped;
                    else
                        toolCallsNoIndex.Add(mapped);
                }
            }

            if (chunk.Done)
            {
                if (chunk.PromptEvalCount is int p) promptTokens = p;
                if (chunk.EvalCount is int o) outputTokens = o;
                break;
            }
        }

        var ordered = toolCallsByIndex
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value)
            .Concat(toolCallsNoIndex)
            .ToList();

        var usage = promptTokens.HasValue && outputTokens.HasValue
            ? new TokenUsage(promptTokens.Value, outputTokens.Value, promptTokens.Value + outputTokens.Value)
            : null;

        return new LlmChatResponse(contentBuilder.ToString(), ordered, usage);
    }

    public override async IAsyncEnumerable<string> StreamAsync(
        IEnumerable<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new OllamaChatRequest
        {
            Model    = config.Model,
            Messages = messages.Select(m => new OllamaMessage { Role = m.RoleString, Content = m.Content }).ToList(),
            Stream   = true,
            Options  = BuildOptions()
        };

        HttpResponseMessage response;
        try
        {
            response = await Http.PostAsJsonAsync(
                $"{config.BaseUrl.TrimEnd('/')}/api/chat", request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw WrapHttpException(ex, config.BaseUrl);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw WrapTimeoutException(ex, $"Ollama ({config.BaseUrl})");
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
        [JsonPropertyName("options")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OllamaOptions? Options { get; set; }
        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<OllamaTool>? Tools { get; set; }
    }

    private sealed class OllamaOptions
    {
        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }
        [JsonPropertyName("top_p")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? TopP { get; set; }
        [JsonPropertyName("num_ctx")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumCtx { get; set; }
        [JsonPropertyName("num_predict")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumPredict { get; set; }
        [JsonPropertyName("repeat_penalty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? RepeatPenalty { get; set; }
        [JsonPropertyName("seed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Seed { get; set; }
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("role")]    public string Role    { get; set; } = "";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
        [JsonPropertyName("tool_calls")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<OllamaToolCall>? ToolCalls { get; set; }
        [JsonPropertyName("tool_name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ToolName { get; set; }
    }

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")]           public OllamaMessage? Message        { get; set; }
        [JsonPropertyName("done")]              public bool           Done            { get; set; }
        [JsonPropertyName("prompt_eval_count")] public int?           PromptEvalCount { get; set; }
        [JsonPropertyName("eval_count")]        public int?           EvalCount       { get; set; }
    }

    private sealed class OllamaTool
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "function";
        [JsonPropertyName("function")] public OllamaToolFunction Function { get; set; } = new();
    }

    private sealed class OllamaToolFunction
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("parameters")] public object Parameters { get; set; } = new();
    }

    private sealed class OllamaToolCall
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "function";
        [JsonPropertyName("function")] public OllamaToolCallFunction Function { get; set; } = new();
    }

    private sealed class OllamaToolCallFunction
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("arguments")] public JsonElement Arguments { get; set; }
        [JsonPropertyName("index")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Index { get; set; }
    }

    private static OllamaMessage MapMessage(ChatMessage msg)
    {
        var message = new OllamaMessage
        {
            Role    = msg.RoleString,
            Content = msg.Content
        };

        if (msg.ToolCalls is { Count: > 0 })
            message.ToolCalls = msg.ToolCalls.Select(MapToolCallDto).ToList();

        if (!string.IsNullOrWhiteSpace(msg.ToolName))
            message.ToolName = msg.ToolName;

        return message;
    }

    private static OllamaTool MapTool(ToolDefinition tool) =>
        new()
        {
            Type = "function",
            Function = new OllamaToolFunction
            {
                Name        = tool.Name,
                Description = tool.Description,
                Parameters  = tool.Parameters
            }
        };

    private static OllamaToolCall MapToolCallDto(ToolCall call)
    {
        var args = TryParseJson(call.ArgumentsJson, out var element)
            ? element
            : JsonSerializer.Deserialize<JsonElement>("{\"command\":\"" + EscapeJson(call.ArgumentsJson) + "\"}");

        return new OllamaToolCall
        {
            Type = "function",
            Function = new OllamaToolCallFunction
            {
                Name = call.Name,
                Arguments = args,
                Index = call.Index
            }
        };
    }

    private static ToolCall MapToolCall(OllamaToolCall call) =>
        new(null, call.Function.Name, call.Function.Arguments.GetRawText(), call.Function.Index);

    private static bool TryParseJson(string input, out JsonElement element)
    {
        try
        {
            element = JsonSerializer.Deserialize<JsonElement>(input);
            return true;
        }
        catch (JsonException)
        {
            element = default;
            return false;
        }
    }

    private static string EscapeJson(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private OllamaOptions? BuildOptions()
    {
        if (config.Temperature is null
            && config.TopP is null
            && config.NumCtx is null
            && config.NumPredict is null
            && config.RepeatPenalty is null
            && config.Seed is null)
            return null;

        return new OllamaOptions
        {
            Temperature = config.Temperature,
            TopP = config.TopP,
            NumCtx = config.NumCtx,
            NumPredict = config.NumPredict,
            RepeatPenalty = config.RepeatPenalty,
            Seed = config.Seed,
        };
    }
}
