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

        HttpResponseMessage response;
        try
        {
            response = await Http.PostAsJsonAsync(url, openAiRequest, JsonDefaults.Options, ct);
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
            response = await Http.PostAsJsonAsync(url, request, JsonDefaults.Options, ct);
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
