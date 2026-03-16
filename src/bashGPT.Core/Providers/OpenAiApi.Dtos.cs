using System.Text;
using System.Text.Json.Serialization;

namespace BashGPT.Providers;

// ── OpenAI-kompatible DTOs für Provider-Integrationen ───────────────────────

internal sealed class OpenAiChatRequest
{
    [JsonPropertyName("model")]    public string Model    { get; set; } = "";
    [JsonPropertyName("messages")] public List<OpenAiMessage> Messages { get; set; } = [];
    [JsonPropertyName("stream")]   public bool   Stream   { get; set; } = true;
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
    public OpenAiResponseFormat? ResponseFormat { get; set; }
    [JsonPropertyName("stream_options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAiStreamOptions? StreamOptions { get; set; }
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OpenAiTool>? Tools { get; set; }
    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAiToolChoice? ToolChoice { get; set; }
    [JsonPropertyName("parallel_tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ParallelToolCalls { get; set; }
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAiOllamaOptions? Options { get; set; }
}

internal sealed class OpenAiResponseFormat
{
    [JsonPropertyName("type")] public string Type { get; set; } = "text";

    public static OpenAiResponseFormat? FromString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new OpenAiResponseFormat { Type = value };
}

internal sealed class OpenAiOllamaOptions
{
    [JsonPropertyName("num_ctx")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NumCtx { get; set; }
}

internal sealed class OpenAiStreamOptions
{
    [JsonPropertyName("include_usage")] public bool IncludeUsage { get; set; } = true;
}

internal sealed class OpenAiMessage
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

internal sealed class OpenAiStreamChunk
{
    [JsonPropertyName("choices")] public List<OpenAiChoice>? Choices { get; set; }
    [JsonPropertyName("usage")] public OpenAiUsage? Usage { get; set; }
}

internal sealed class OpenAiChoice
{
    [JsonPropertyName("delta")] public OpenAiDelta? Delta { get; set; }
}

internal sealed class OpenAiDelta
{
    [JsonPropertyName("content")] public string? Content { get; set; }
    [JsonPropertyName("reasoning")] public string? ReasoningContent { get; set; }
    [JsonPropertyName("tool_calls")] public List<OpenAiToolCallDelta>? ToolCalls { get; set; }
}

internal sealed class OpenAiChatResponse
{
    [JsonPropertyName("choices")] public List<OpenAiChatChoice>? Choices { get; set; }
    [JsonPropertyName("usage")] public OpenAiUsage? Usage { get; set; }
}

internal sealed class OpenAiUsage
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
    [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
    [JsonPropertyName("total_tokens")] public int? TotalTokens { get; set; }
    [JsonPropertyName("prompt_tokens_details")] public OpenAiPromptTokensDetails? PromptTokensDetails { get; set; }
}

internal sealed class OpenAiPromptTokensDetails
{
    [JsonPropertyName("cached_tokens")] public int? CachedTokens { get; set; }
}

internal sealed class OpenAiChatChoice
{
    [JsonPropertyName("message")] public OpenAiMessage? Message { get; set; }
}

internal sealed class OpenAiTool
{
    [JsonPropertyName("type")] public string Type { get; set; } = "function";
    [JsonPropertyName("function")] public OpenAiToolFunction Function { get; set; } = new();
}

internal sealed class OpenAiToolFunction
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("parameters")] public object Parameters { get; set; } = new();
}

internal sealed class OpenAiToolChoice
{
    [JsonPropertyName("type")] public string Type { get; set; } = "function";
    [JsonPropertyName("function")] public OpenAiToolChoiceFunction Function { get; set; } = new();

    public static OpenAiToolChoice ForFunction(string name) =>
        new() { Function = new OpenAiToolChoiceFunction { Name = name } };
}

internal sealed class OpenAiToolChoiceFunction
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

internal sealed class OpenAiToolCall
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "function";
    [JsonPropertyName("function")] public OpenAiToolCallFunction Function { get; set; } = new();
}

internal sealed class OpenAiToolCallDelta
{
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("function")] public OpenAiToolCallFunction? Function { get; set; }
}

internal sealed class OpenAiToolCallFunction
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("arguments")] public string? Arguments { get; set; }
}

internal sealed class ToolCallBuilder
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public StringBuilder Arguments { get; } = new();
    public int Index { get; init; }

    public ToolCall ToToolCall() =>
        new(Id, Name ?? "", Arguments.ToString(), Index);
}
