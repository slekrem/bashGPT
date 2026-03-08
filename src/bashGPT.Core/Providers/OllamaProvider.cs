using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using BashGPT.Configuration;

namespace BashGPT.Providers;

public class OllamaProvider(OllamaConfig config, HttpClient? httpClient = null)
    : BaseLlmProvider(httpClient)
{
    public override string Name  => "Ollama";
    public override string Model => config.Model;

    public override async Task<LlmChatResponse> ChatAsync(LlmChatRequest request, CancellationToken ct = default)
    {
        var openAiRequest = new OpenAiChatRequest
        {
            Model       = config.Model,
            Messages    = request.Messages.Select(MapMessage).ToList(),
            Stream      = request.Stream,
            Temperature = config.Temperature,
            TopP        = config.TopP,
            Seed        = config.Seed,
        };

        if (request.Tools is { Count: > 0 })
            openAiRequest.Tools = request.Tools.Select(MapTool).ToList();

        if (request.Stream)
            openAiRequest.StreamOptions = new OpenAiStreamOptions();

        var url = $"{config.BaseUrl.TrimEnd('/')}/v1/chat/completions";

        var serialized = JsonSerializer.Serialize(openAiRequest, JsonDefaults.Options);
        request.OnRequestJson?.Invoke(serialized);

        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(serialized, Encoding.UTF8, "application/json")
            };
            // ResponseHeadersRead ermöglicht echtes Streaming (kein Puffern des Response-Body)
            response = await Http.SendAsync(httpRequest,
                request.Stream
                    ? HttpCompletionOption.ResponseHeadersRead
                    : HttpCompletionOption.ResponseContentRead,
                ct);
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

            // Reasoning-Modelle können Denktext vor das Tool-Call-JSON schreiben.
            // Ollama scheitert dann beim Parsen und liefert HTTP 500.
            // Fallback: JSON aus dem raw-Feld extrahieren und Tool-Call rekonstruieren.
            if ((int)response.StatusCode == 500)
            {
                var recovered = TryRecoverToolCall(body);
                if (recovered is not null)
                    return recovered;
            }

            throw new LlmProviderException(
                $"Ollama antwortete mit HTTP {(int)response.StatusCode}: {body}");
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
            var nonStreamUsage = full?.Usage is { } u
                ? new TokenUsage(u.PromptTokens, u.CompletionTokens, u.TotalTokens)
                : null;
            return new LlmChatResponse(content, toolCalls, nonStreamUsage);
        }

        var contentBuilder = new StringBuilder();
        var toolBuilder = new Dictionary<int, ToolCallBuilder>();
        OpenAiUsage? streamUsage = null;

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

            if (chunk?.Usage is not null)
                streamUsage = chunk.Usage;
        }

        var toolCallsFinal = toolBuilder
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value.ToToolCall())
            .ToList();

        var usage = streamUsage is not null
            ? new TokenUsage(streamUsage.PromptTokens, streamUsage.CompletionTokens, streamUsage.TotalTokens)
            : null;

        return new LlmChatResponse(contentBuilder.ToString(), toolCallsFinal, usage);
    }

    public override async IAsyncEnumerable<string> StreamAsync(
        IEnumerable<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new OpenAiChatRequest
        {
            Model       = config.Model,
            Messages    = messages.Select(m => new OpenAiMessage { Role = m.RoleString, Content = m.Content }).ToList(),
            Stream      = true,
            Temperature = config.Temperature,
            TopP        = config.TopP,
            Seed        = config.Seed,
        };

        var url = $"{config.BaseUrl.TrimEnd('/')}/v1/chat/completions";

        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(request, JsonDefaults.Options), Encoding.UTF8, "application/json")
            };
            response = await Http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
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

    // ── Fehler-Fallback ─────────────────────────────────────────────────────

    /// <summary>
    /// Versucht einen Tool-Call aus einer Ollama-HTTP-500-Antwort zu retten,
    /// die durch Reasoning-Text vor dem JSON-Argument entstanden ist.
    /// Format der Fehlermeldung: "error parsing tool call: raw='&lt;text&gt;{json}', err=..."
    /// </summary>
    private static LlmChatResponse? TryRecoverToolCall(string errorBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(errorBody);
            var message = doc.RootElement
                .GetProperty("error")
                .GetProperty("message")
                .GetString();

            if (message is null || !message.Contains("error parsing tool call", StringComparison.Ordinal))
                return null;

            var rawStart = message.IndexOf("raw='", StringComparison.Ordinal);
            if (rawStart < 0) return null;
            rawStart += "raw='".Length;

            var rawEnd = message.LastIndexOf("', err=", StringComparison.Ordinal);
            if (rawEnd < 0 || rawEnd <= rawStart) return null;

            var raw = message[rawStart..rawEnd];

            // Letztes JSON-Objekt im raw-String finden
            var jsonStart = raw.LastIndexOf('{');
            if (jsonStart < 0) return null;
            var jsonStr = raw[jsonStart..];

            // Prüfen ob das JSON parsebar ist
            JsonDocument.Parse(jsonStr).Dispose();

            var reasoningText = raw[..jsonStart].Trim();
            var toolCall = new ToolCall("recovered-0", "bash", jsonStr);
            return new LlmChatResponse(reasoningText, [toolCall], null);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // ── Mapping-Funktionen ──────────────────────────────────────────────────

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
