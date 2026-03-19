using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using BashGPT.Configuration;
using bashGPT.Core.Models.Providers.Ollama;
using bashGPT.Core.Models.Providers;

namespace bashGPT.Core.Providers.Ollama;

public class OllamaProvider(OllamaConfig config, HttpClient? httpClient = null)
    : BaseLlmProvider(httpClient)
{
    public override string Name => "Ollama";
    public override string Model => config.Model;

    public override async Task<LlmChatResponse> ChatAsync(LlmChatRequest request, CancellationToken ct = default)
    {
        var openAiRequest = OllamaRequestMapper.MapChatRequest(request, config.Model);
        var url = $"{config.BaseUrl.TrimEnd('/')}/v1/chat/completions";

        var serialized = JsonSerializer.Serialize(openAiRequest, JsonDefaults.Options);
        if (request.OnRequestJson is not null)
            await request.OnRequestJson(serialized);

        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(serialized, Encoding.UTF8, "application/json")
            };

            response = await Http.SendAsync(
                httpRequest,
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

            if ((int)response.StatusCode == 500)
            {
                var firstToolName = openAiRequest.Tools?.FirstOrDefault()?.Function?.Name;
                var recovered = OllamaToolCallRecovery.TryRecover(body, firstToolName);
                if (recovered is not null)
                    return recovered;
            }

            throw new LlmProviderException(
                $"Ollama antwortete mit HTTP {(int)response.StatusCode}: {body}");
        }

        if (!request.Stream)
        {
            var json = await response.Content.ReadAsStringAsync(ct);
            if (request.OnResponseJson is not null)
                await request.OnResponseJson(json);

            var full = JsonSerializer.Deserialize<OpenAiCompatibleChatResponse>(json, JsonDefaults.Options);
            var message = full?.Choices?.FirstOrDefault()?.Message;
            var content = message?.Content ?? "";
            var toolCalls = message?.ToolCalls?.Select(OllamaRequestMapper.MapToolCall).ToList() ?? [];

            if (!string.IsNullOrEmpty(content))
                request.OnToken?.Invoke(content);

            var usage = full?.Usage is { } nonStreamUsage
                ? new TokenUsage(nonStreamUsage.PromptTokens, nonStreamUsage.CompletionTokens, nonStreamUsage.TotalTokens)
                : null;

            return new LlmChatResponse(content, toolCalls, usage);
        }

        var contentBuilder = new StringBuilder();
        var toolBuilder = new Dictionary<int, OpenAiCompatibleToolCallBuilder>();
        var rawLines = new StringBuilder();
        OpenAiCompatibleUsage? streamUsage = null;
        var streamCompleted = false;

        await using (var stream = await response.Content.ReadAsStreamAsync(ct))
        using (var reader = new System.IO.StreamReader(stream))
        {
            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!line.StartsWith("data:", StringComparison.Ordinal))
                    continue;

                rawLines.AppendLine(line);

                var json = line["data:".Length..].Trim();
                if (json == "[DONE]")
                {
                    streamCompleted = true;
                    break;
                }

                OpenAiCompatibleStreamChunk? chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<OpenAiCompatibleStreamChunk>(json, JsonDefaults.Options);
                }
                catch (JsonException)
                {
                    continue;
                }

                var delta = chunk?.Choices?.FirstOrDefault()?.Delta;

                var reasoning = delta?.ReasoningContent;
                if (!string.IsNullOrEmpty(reasoning))
                    request.OnReasoningToken?.Invoke(reasoning);

                var content = delta?.Content;
                if (!string.IsNullOrEmpty(content))
                {
                    request.OnToken?.Invoke(content);
                    contentBuilder.Append(content);
                }

                if (delta?.ToolCalls is { Count: > 0 })
                {
                    foreach (var toolDelta in delta.ToolCalls)
                        OllamaRequestMapper.ApplyToolDelta(toolBuilder, toolDelta);
                }

                if (chunk?.Usage is not null)
                    streamUsage = chunk.Usage;
            }
        }

        var rawResponse = rawLines.ToString();

        if (streamCompleted)
        {
            if (request.OnResponseJson is not null)
                await request.OnResponseJson(rawResponse);

            var toolCallsFinal = toolBuilder
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => kvp.Value.ToToolCall())
                .ToList();

            var usage = streamUsage is not null
                ? new TokenUsage(streamUsage.PromptTokens, streamUsage.CompletionTokens, streamUsage.TotalTokens)
                : null;

            return new LlmChatResponse(contentBuilder.ToString(), toolCallsFinal, usage);
        }

        if (request.OnResponseJson is not null)
            await request.OnResponseJson(rawResponse);

        throw new LlmProviderException(
            "Ollama stream ended before [DONE]." +
            (string.IsNullOrWhiteSpace(rawResponse) ? "" : $"\nLast stream chunk:\n{rawResponse}"));
    }

    public override async IAsyncEnumerable<string> StreamAsync(
        IEnumerable<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = OllamaRequestMapper.MapStreamingRequest(messages, config.Model);
        var url = $"{config.BaseUrl.TrimEnd('/')}/v1/chat/completions";

        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(request, JsonDefaults.Options),
                    Encoding.UTF8,
                    "application/json")
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
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!line.StartsWith("data:", StringComparison.Ordinal))
                continue;

            var json = line["data:".Length..].Trim();
            if (json == "[DONE]")
                yield break;

            OpenAiCompatibleStreamChunk? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OpenAiCompatibleStreamChunk>(json, JsonDefaults.Options);
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
}
