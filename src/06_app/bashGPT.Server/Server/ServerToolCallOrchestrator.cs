using bashGPT.Core.Models.Providers;
using bashGPT.Core.Models.Storage;
using bashGPT.Tools.Registration;

namespace bashGPT.Server;

internal static class ServerToolCallOrchestrator
{
    public static async Task<IReadOnlyList<SessionCommand>> ExecuteRoundAsync(
        IReadOnlyList<ToolCall> toolCalls,
        string assistantContent,
        List<ChatMessage> messages,
        List<ChatMessage> conversationDelta,
        ToolRegistry toolRegistry,
        string? sessionPath,
        Action<SseEvent>? onEvent,
        CancellationToken ct)
    {
        var assistantToolCallMessage = ChatMessage.AssistantWithToolCalls(
            toolCalls,
            content: assistantContent);

        messages.Add(assistantToolCallMessage);
        conversationDelta.Add(assistantToolCallMessage);

        var commandResults = new List<SessionCommand>();

        foreach (var call in toolCalls)
        {
            ct.ThrowIfCancellationRequested();

            var commandLabel = ToolCallArguments.TryGetString(call, "command", out var parsedCommand, out _)
                ? parsedCommand
                : call.Name;
            onEvent?.Invoke(new SseEvent("tool_call", new { name = call.Name, command = commandLabel }));

            var (toolResult, commandResult) = await ExecuteToolCallAsync(call, commandLabel, toolRegistry, sessionPath, ct);

            commandResults.Add(commandResult);
            onEvent?.Invoke(new SseEvent("command_result", new
            {
                command = commandResult.Command,
                output = commandResult.Output,
                exitCode = commandResult.ExitCode,
                wasExecuted = commandResult.WasExecuted,
                status = ClassifyCommandStatus(commandResult),
            }));

            var toolMessage = ChatMessage.ToolResult(toolResult, call.Id, call.Name);
            messages.Add(toolMessage);
            conversationDelta.Add(toolMessage);
        }

        return commandResults;
    }

    public static string ClassifyCommandStatus(SessionCommand result)
        => result.Output.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            ? "timeout"
            : result.WasExecuted
                ? "executed"
                : "failed";

    private static async Task<(string ToolResult, SessionCommand CommandResult)> ExecuteToolCallAsync(
        ToolCall call,
        string commandLabel,
        ToolRegistry toolRegistry,
        string? sessionPath,
        CancellationToken ct)
    {
        if (toolRegistry.TryGet(call.Name, out var tool) && tool is not null)
        {
            try
            {
                var result = await tool.ExecuteAsync(
                    new bashGPT.Tools.Abstractions.ToolCall(call.Name, call.ArgumentsJson ?? "{}", sessionPath), ct);

                return (
                    result.Content,
                    BuildCommandResult(call.Name, commandLabel, result.Content, result.Success));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var toolResult = $"Error: {ex.Message}";
                return (toolResult, new SessionCommand { Command = commandLabel, ExitCode = 1, Output = toolResult, WasExecuted = false });
            }
        }

        var unknownToolResult = $"Error: Unknown tool '{call.Name}'.";
        return (unknownToolResult, new SessionCommand { Command = commandLabel, ExitCode = 1, Output = unknownToolResult, WasExecuted = false });
    }

    private static SessionCommand BuildCommandResult(string toolName, string commandLabel, string content, bool success)
    {
        return new SessionCommand
        {
            Command = $"{toolName}: {commandLabel}",
            ExitCode = success ? 0 : 1,
            Output = content,
            WasExecuted = success,
        };
    }
}
