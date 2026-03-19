namespace BashGPT.Providers;

internal static class OllamaRequestMapper
{
    public static OpenAiCompatibleChatRequest MapChatRequest(LlmChatRequest request, string model)
    {
        var openAiRequest = new OpenAiCompatibleChatRequest
        {
            Model = model,
            Messages = request.Messages.Select(MapMessage).ToList(),
            Stream = request.Stream,
            Temperature = request.Temperature,
            TopP = request.TopP,
            MaxTokens = request.MaxTokens,
            Seed = request.Seed,
            ReasoningEffort = string.IsNullOrWhiteSpace(request.ReasoningEffort) ? null : request.ReasoningEffort,
            FrequencyPenalty = request.FrequencyPenalty,
            PresencePenalty = request.PresencePenalty,
            Stop = request.Stop?.Count > 0 ? [.. request.Stop] : null,
            ResponseFormat = OpenAiCompatibleResponseFormat.FromString(request.ResponseFormat),
            Options = request.NumCtx is > 0
                ? new OpenAiCompatibleOllamaOptions { NumCtx = request.NumCtx }
                : null,
        };

        if (request.Tools is { Count: > 0 })
            openAiRequest.Tools = request.Tools.Select(MapTool).ToList();

        if (request.Stream)
            openAiRequest.StreamOptions = new OpenAiCompatibleStreamOptions();

        return openAiRequest;
    }

    public static OpenAiCompatibleChatRequest MapStreamingRequest(IEnumerable<ChatMessage> messages, string model) =>
        new()
        {
            Model = model,
            Messages = messages.Select(message => new OpenAiCompatibleMessage
            {
                Role = message.RoleString,
                Content = message.Content
            }).ToList(),
            Stream = true,
        };

    public static ToolCall MapToolCall(OpenAiCompatibleToolCall call) =>
        new(call.Id, call.Function.Name ?? "", call.Function.Arguments ?? "", null);

    public static void ApplyToolDelta(
        Dictionary<int, OpenAiCompatibleToolCallBuilder> builder,
        OpenAiCompatibleToolCallDelta delta)
    {
        if (!builder.TryGetValue(delta.Index, out var item))
        {
            item = new OpenAiCompatibleToolCallBuilder { Index = delta.Index };
            builder[delta.Index] = item;
        }

        if (!string.IsNullOrWhiteSpace(delta.Id))
            item.Id = delta.Id;

        if (!string.IsNullOrWhiteSpace(delta.Function?.Name))
            item.Name = delta.Function.Name;

        if (!string.IsNullOrWhiteSpace(delta.Function?.Arguments))
            item.Arguments.Append(delta.Function.Arguments);
    }

    private static OpenAiCompatibleMessage MapMessage(ChatMessage msg)
    {
        var message = new OpenAiCompatibleMessage
        {
            Role = msg.RoleString,
            Content = msg.Content
        };

        if (msg.ToolCalls is { Count: > 0 })
            message.ToolCalls = msg.ToolCalls.Select(MapToolCallDto).ToList();

        if (!string.IsNullOrWhiteSpace(msg.ToolCallId))
            message.ToolCallId = msg.ToolCallId;

        return message;
    }

    private static OpenAiCompatibleTool MapTool(ToolDefinition tool) =>
        new()
        {
            Type = "function",
            Function = new OpenAiCompatibleToolFunction
            {
                Name = tool.Name,
                Description = tool.Description,
                Parameters = tool.Parameters
            }
        };

    private static OpenAiCompatibleToolCall MapToolCallDto(ToolCall call) =>
        new()
        {
            Id = call.Id ?? "",
            Type = "function",
            Function = new OpenAiCompatibleToolCallFunction
            {
                Name = call.Name,
                Arguments = call.ArgumentsJson
            }
        };
}
