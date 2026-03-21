using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Models.Providers.Ollama;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Serialization;

namespace bashGPT.Core.Providers.Ollama;

internal static class OllamaStreamParser
{
    public static async Task<LlmChatResponse> ParseChatResponseAsync(
        HttpContent content,
        LlmChatRequest request,
        CancellationToken ct)
    {
        var contentBuilder = new StringBuilder();
        var toolBuilder = new Dictionary<int, OpenAiCompatibleToolCallBuilder>();
        var rawLines = new StringBuilder();
        OpenAiCompatibleUsage? streamUsage = null;
        var streamCompleted = false;

        await using (var stream = await content.ReadAsStreamAsync(ct))
        using (var reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (!TryGetDataLine(line, out var json))
                    continue;

                rawLines.AppendLine(line);

                if (json == "[DONE]")
                {
                    streamCompleted = true;
                    break;
                }

                var chunk = DeserializeChunk(json);
                if (chunk is null)
                    continue;

                var delta = chunk.Choices?.FirstOrDefault()?.Delta;

                var reasoning = delta?.ReasoningContent;
                if (!string.IsNullOrEmpty(reasoning))
                    request.OnReasoningToken?.Invoke(reasoning);

                var token = delta?.Content;
                if (!string.IsNullOrEmpty(token))
                {
                    request.OnToken?.Invoke(token);
                    contentBuilder.Append(token);
                }

                if (delta?.ToolCalls is { Count: > 0 })
                {
                    foreach (var toolDelta in delta.ToolCalls)
                        OllamaRequestMapper.ApplyToolDelta(toolBuilder, toolDelta);
                }

                if (chunk.Usage is not null)
                    streamUsage = chunk.Usage;
            }
        }

        var rawResponse = rawLines.ToString();
        if (request.OnResponseJson is not null)
            await request.OnResponseJson(rawResponse);

        if (!streamCompleted)
        {
            throw new LlmProviderException(
                "Ollama stream ended before [DONE]." +
                (string.IsNullOrWhiteSpace(rawResponse) ? "" : $"\nLast stream chunk:\n{rawResponse}"));
        }

        var toolCalls = toolBuilder
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value.ToToolCall())
            .ToList();

        var usage = streamUsage is not null
            ? new TokenUsage(streamUsage.PromptTokens, streamUsage.CompletionTokens, streamUsage.TotalTokens)
            : null;

        return new LlmChatResponse(contentBuilder.ToString(), toolCalls, usage);
    }

    public static async IAsyncEnumerable<string> StreamTokensAsync(
        HttpContent content,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await using var stream = await content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (!TryGetDataLine(line, out var json))
                continue;

            if (json == "[DONE]")
                yield break;

            var chunk = DeserializeChunk(json);
            var token = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(token))
                yield return token;
        }
    }

    private static OpenAiCompatibleStreamChunk? DeserializeChunk(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<OpenAiCompatibleStreamChunk>(json, JsonDefaults.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryGetDataLine(string? line, out string json)
    {
        json = string.Empty;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        if (!line.StartsWith("data:", StringComparison.Ordinal))
            return false;

        json = line["data:".Length..].Trim();
        return true;
    }
}
