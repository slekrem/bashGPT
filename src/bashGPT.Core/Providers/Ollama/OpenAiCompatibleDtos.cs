using System.Text;
using System.Text.Json.Serialization;

namespace bashGPT.Core.Providers.Ollama;

internal sealed class OpenAiCompatibleChatRequest
{
    [JsonPropertyName("model")] public string Model { get; set; } = "";
    [JsonPropertyName("messages")] public List<OpenAiCompatibleMessage> Messages { get; set; } = [];
    [JsonPropertyName("stream")] public bool Stream { get; set; } = true;
    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }
    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TopP { get; set; }
    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }
    [JsonPropertyName("max_completion_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxCompletionTokens { get; set; }
    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Seed { get; set; }
    [JsonPropertyName("reasoning_effort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningEffort { get; set; }
    [JsonPropertyName("frequency_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? FrequencyPenalty { get; set; }
    [JsonPropertyName("presence_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PresencePenalty { get; set; }
    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Stop { get; set; }
    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAiCompatibleResponseFormat? ResponseFormat { get; set; }
    [JsonPropertyName("stream_options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAiCompatibleStreamOptions? StreamOptions { get; set; }
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OpenAiCompatibleTool>? Tools { get; set; }
    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAiCompatibleToolChoice? ToolChoice { get; set; }
    [JsonPropertyName("parallel_tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ParallelToolCalls { get; set; }
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAiCompatibleOllamaOptions? Options { get; set; }
}

internal sealed class OpenAiCompatibleResponseFormat
{
    [JsonPropertyName("type")] public string Type { get; set; } = "text";

    public static OpenAiCompatibleResponseFormat? FromString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new OpenAiCompatibleResponseFormat { Type = value };
}

internal sealed class OpenAiCompatibleOllamaOptions
{
    [JsonPropertyName("num_ctx")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumCtx { get; set; }
}

internal sealed class OpenAiCompatibleStreamOptions
{
    [JsonPropertyName("include_usage")] public bool IncludeUsage { get; set; } = true;
}

internal sealed class OpenAiCompatibleMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("content")] public string Content { get; set; } = "";
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OpenAiCompatibleToolCall>? ToolCalls { get; set; }
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }
}

internal sealed class OpenAiCompatibleStreamChunk
{
    [JsonPropertyName("choices")] public List<OpenAiCompatibleChoice>? Choices { get; set; }
    [JsonPropertyName("usage")] public OpenAiCompatibleUsage? Usage { get; set; }
}

internal sealed class OpenAiCompatibleChoice
{
    [JsonPropertyName("delta")] public OpenAiCompatibleDelta? Delta { get; set; }
}

internal sealed class OpenAiCompatibleDelta
{
    [JsonPropertyName("content")] public string? Content { get; set; }
    [JsonPropertyName("reasoning")] public string? ReasoningContent { get; set; }
    [JsonPropertyName("tool_calls")] public List<OpenAiCompatibleToolCallDelta>? ToolCalls { get; set; }
}

internal sealed class OpenAiCompatibleChatResponse
{
    [JsonPropertyName("choices")] public List<OpenAiCompatibleChatChoice>? Choices { get; set; }
    [JsonPropertyName("usage")] public OpenAiCompatibleUsage? Usage { get; set; }
}

internal sealed class OpenAiCompatibleUsage
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
    [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
    [JsonPropertyName("total_tokens")] public int? TotalTokens { get; set; }
    [JsonPropertyName("prompt_tokens_details")] public OpenAiCompatiblePromptTokensDetails? PromptTokensDetails { get; set; }
}

internal sealed class OpenAiCompatiblePromptTokensDetails
{
    [JsonPropertyName("cached_tokens")] public int? CachedTokens { get; set; }
}

internal sealed class OpenAiCompatibleChatChoice
{
    [JsonPropertyName("message")] public OpenAiCompatibleMessage? Message { get; set; }
}

internal sealed class OpenAiCompatibleTool
{
    [JsonPropertyName("type")] public string Type { get; set; } = "function";
    [JsonPropertyName("function")] public OpenAiCompatibleToolFunction Function { get; set; } = new();
}

internal sealed class OpenAiCompatibleToolFunction
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("parameters")] public object Parameters { get; set; } = new();
}

internal sealed class OpenAiCompatibleToolChoice
{
    [JsonPropertyName("type")] public string Type { get; set; } = "function";
    [JsonPropertyName("function")] public OpenAiCompatibleToolChoiceFunction Function { get; set; } = new();

    public static OpenAiCompatibleToolChoice ForFunction(string name) =>
        new() { Function = new OpenAiCompatibleToolChoiceFunction { Name = name } };
}

internal sealed class OpenAiCompatibleToolChoiceFunction
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

internal sealed class OpenAiCompatibleToolCall
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "function";
    [JsonPropertyName("function")] public OpenAiCompatibleToolCallFunction Function { get; set; } = new();
}

internal sealed class OpenAiCompatibleToolCallDelta
{
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("function")] public OpenAiCompatibleToolCallFunction? Function { get; set; }
}

internal sealed class OpenAiCompatibleToolCallFunction
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("arguments")] public string? Arguments { get; set; }
}

internal sealed class OpenAiCompatibleToolCallBuilder
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public StringBuilder Arguments { get; } = new();
    public int Index { get; init; }

    public ToolCall ToToolCall() =>
        new(Id, Name ?? "", Arguments.ToString(), Index);
}
