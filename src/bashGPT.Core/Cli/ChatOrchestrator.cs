using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;

namespace BashGPT.Cli;

internal sealed record ParsedToolCommand(ToolCall ToolCall, ExtractedCommand Command);
internal sealed record ToolCallError(ToolCall ToolCall, string Error);

/// <summary>
/// Gemeinsame, ausgabe-agnostische Chat-Hilfslogik: Tool-Call-Parsing,
/// Befehlsausführung, Message-Aufbau und Provider-Aufruf ohne Console-Abhängigkeit.
/// </summary>
internal static class ChatOrchestrator
{
    /// <summary>
    /// Überschreibt das Modell in der Config, wenn <paramref name="modelOverride"/> gesetzt ist.
    /// </summary>
    internal static void ApplyModelOverride(
        AppConfig config,
        ProviderType? providerOverride,
        string? modelOverride)
    {
        if (modelOverride is null) return;
        if (providerOverride is ProviderType.Cerebras || config.DefaultProvider == ProviderType.Cerebras)
            config.Cerebras.Model = modelOverride;
        else
            config.Ollama.Model = modelOverride;
    }

    /// <summary>
    /// Ruft den Provider einmalig auf (ohne Console-Ausgabe) und gibt Antwort oder Fehlermeldung zurück.
    /// </summary>
    internal static async Task<(LlmChatResponse Response, string? Error)> ChatOnceAsync(
        ILlmProvider provider,
        List<ChatMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        string? toolChoiceName,
        CancellationToken ct,
        Action<string>? onToken = null,
        Action<string>? onRequestJson = null,
        Action<string>? onResponseJson = null)
    {
        try
        {
            var tokenBuffer = new System.Text.StringBuilder();
            var response = await provider.ChatAsync(
                new LlmChatRequest(
                    Messages: messages,
                    Tools: tools,
                    ToolChoiceName: toolChoiceName,
                    ParallelToolCalls: false,
                    Stream: true,
                    OnToken: token => { tokenBuffer.Append(token); onToken?.Invoke(token); },
                    OnRequestJson: onRequestJson,
                    OnResponseJson: onResponseJson),
                ct);

            if (string.IsNullOrWhiteSpace(response.Content) && tokenBuffer.Length > 0)
                response = response with { Content = tokenBuffer.ToString() };

            return (response, null);
        }
        catch (LlmProviderException ex)
        {
            return (new LlmChatResponse("", []), $"Fehler: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            return (new LlmChatResponse("", []), "Abgebrochen.");
        }
    }

    /// <summary>
    /// Parst Tool-Calls aus der LLM-Antwort in ausführbare Befehle und Fehler.
    /// </summary>
    internal static (List<ParsedToolCommand> Commands, List<ToolCallError> Errors) ParseToolCalls(
        IReadOnlyList<ToolCall> toolCalls)
    {
        var commands = new List<ParsedToolCommand>();
        var errors   = new List<ToolCallError>();

        foreach (var call in toolCalls)
        {
            if (!ToolCallParsing.TryGetCommand(call, out var command, out var error))
            {
                errors.Add(new ToolCallError(call, error ?? "Unbekannter Fehler."));
                continue;
            }

            var (isDangerous, reason) = BashCommandExtractor.CheckDanger(command);
            commands.Add(new ParsedToolCommand(call, new ExtractedCommand(command, isDangerous, reason)));
        }

        return (commands, errors);
    }

    /// <summary>
    /// Führt eine Tool-Call-Runde aus: Befehle ausführen und Assistant- sowie Tool-Result-Nachrichten
    /// an <paramref name="messages"/> anhängen.
    /// </summary>
    internal static async Task<IReadOnlyList<CommandResult>> ExecuteToolCallRoundAsync(
        IReadOnlyList<ToolCall> toolCalls,
        IReadOnlyList<ParsedToolCommand> commands,
        IReadOnlyList<ToolCallError> errors,
        string currentContent,
        List<ChatMessage> messages,
        CommandExecutor executor,
        CancellationToken ct)
    {
        var results = commands.Count > 0
            ? await executor.ProcessAsync(commands.Select(c => c.Command).ToList(), ct)
            : [];

        messages.Add(ChatMessage.AssistantWithToolCalls(toolCalls, currentContent));

        foreach (var msg in BuildToolResultMessages(toolCalls, commands, results, errors))
            messages.Add(msg);

        return results;
    }

    private static IReadOnlyList<ChatMessage> BuildToolResultMessages(
        IReadOnlyList<ToolCall> toolCalls,
        IReadOnlyList<ParsedToolCommand> commands,
        IReadOnlyList<CommandResult> results,
        IReadOnlyList<ToolCallError> errors)
    {
        var messages       = new List<ChatMessage>();
        var commandResults = results.ToList();

        for (var i = 0; i < commands.Count; i++)
        {
            var call   = commands[i].ToolCall;
            var result = i < commandResults.Count
                ? commandResults[i]
                : new CommandResult(commands[i].Command.Command, -1, "Keine Ausführung.", false);

            messages.Add(ChatMessage.ToolResult(
                FormatToolResult(result),
                toolCallId: call.Id,
                toolName:   call.Name));
        }

        foreach (var err in errors)
        {
            messages.Add(ChatMessage.ToolResult(
                $"Fehler: {err.Error}",
                toolCallId: err.ToolCall.Id,
                toolName:   err.ToolCall.Name));
        }

        return messages;
    }

    private static string FormatToolResult(CommandResult result)
    {
        var output = string.IsNullOrWhiteSpace(result.Output)
            ? "(keine Ausgabe)"
            : result.Output;

        var status = result.WasExecuted
            ? $"Exit-Code: {result.ExitCode}"
            : "Nicht ausgeführt";

        return $"{status}\n{output}";
    }
}
