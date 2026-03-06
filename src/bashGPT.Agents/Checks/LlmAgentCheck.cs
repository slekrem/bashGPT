using BashGPT.Providers;
using BashGPT.Shell;
using BashGPT.Storage;
using BashGPT.Tools.Abstractions;
using BashGPT.Tools.Execution;

namespace BashGPT.Agents;

public sealed class LlmAgentCheck(ILlmProvider? provider, SessionStore? sessionStore = null, ToolRegistry? toolRegistry = null) : IAgentCheck
{
    private const int MaxRounds = 5;

    public AgentCheckType Type => AgentCheckType.LlmAgent;

    public async Task<AgentCheckResult> RunAsync(AgentRecord agent, CancellationToken ct)
    {
        if (provider is null)
            return new AgentCheckResult("noprovider", Changed: false, "Kein LLM-Provider konfiguriert.", Success: false);

        if (string.IsNullOrWhiteSpace(agent.LoopInstruction))
            return new AgentCheckResult("noinstruction", Changed: false, "Keine Loop-Anweisung konfiguriert.", Success: false);

        var execMode = ParseExecMode(agent.ExecMode);
        var executor = new CommandExecutor(execMode, output: Console.Out);
        var systemPrompt = string.IsNullOrWhiteSpace(agent.SystemPrompt)
            ? "Du bist ein autonomer Assistent. Führe die gegebene Aufgabe präzise aus."
            : agent.SystemPrompt;

        // Bisherige Session als Kontext laden
        SessionRecord? existingSession = null;
        if (sessionStore is not null)
        {
            try { existingSession = await sessionStore.LoadAsync($"agent-llm-{agent.Id}"); }
            catch { /* ignorieren */ }
        }

        var messages = new List<ChatMessage> { new(ChatRole.System, systemPrompt) };

        if (existingSession is not null)
        {
            foreach (var msg in existingSession.Messages)
            {
                if (msg.Role == "user")
                    messages.Add(new(ChatRole.User, msg.Content));
                else if (msg.Role == "assistant")
                    messages.Add(new(ChatRole.Assistant, msg.Content));
            }
        }

        messages.Add(new(ChatRole.User, agent.LoopInstruction));

        var enabledITools = BuildEnabledTools(agent);
        var tools = enabledITools.Count > 0
            ? enabledITools.Select(t => ToLlmToolDefinition(t.Definition)).ToArray()
            : [ToolDefinitions.Bash];
        var lastContent = "";
        var commandsThisRun = new List<SessionCommand>();

        for (var round = 0; round < MaxRounds; round++)
        {
            ct.ThrowIfCancellationRequested();

            var response = await provider.ChatAsync(
                new LlmChatRequest(messages, Tools: tools, Stream: false),
                ct);

            lastContent = response.Content;

            if (response.ToolCalls.Count == 0)
                break;

            messages.Add(ChatMessage.AssistantWithToolCalls(response.ToolCalls, response.Content));

            foreach (var call in response.ToolCalls)
            {
                string toolResult;
                if (enabledITools.Count > 0 && toolRegistry is not null && toolRegistry.TryGet(call.Name, out var iTool) && iTool is not null)
                {
                    var iResult = await iTool.ExecuteAsync(new BashGPT.Tools.Abstractions.ToolCall(call.Name, call.ArgumentsJson ?? "{}"), ct);
                    toolResult = iResult.Content;
                    commandsThisRun.Add(new SessionCommand
                    {
                        Command     = call.Name,
                        Output      = toolResult,
                        ExitCode    = iResult.Success ? 0 : 1,
                        WasExecuted = true,
                    });
                }
                else if (ToolCallParsing.TryGetCommand(call, out var command, out var parseError))
                {
                    toolResult = await ExecuteCommandAsync(executor, command, execMode, ct);
                    var (exitCode, wasExecuted) = ParseToolResult(toolResult);
                    commandsThisRun.Add(new SessionCommand
                    {
                        Command     = command,
                        Output      = toolResult,
                        ExitCode    = exitCode,
                        WasExecuted = wasExecuted,
                    });
                }
                else
                {
                    toolResult = $"Fehler: {parseError}";
                }

                messages.Add(ChatMessage.ToolResult(toolResult, call.Id, call.Name));
            }
        }

        if (sessionStore is not null && !string.IsNullOrEmpty(lastContent))
            await PersistToSessionAsync(agent, agent.LoopInstruction!, lastContent, commandsThisRun, ct);

        var hash = string.IsNullOrEmpty(lastContent)
            ? "empty"
            : Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(lastContent)))[..16];

        return new AgentCheckResult(hash, Changed: false, lastContent, Success: true);
    }

    private async Task PersistToSessionAsync(
        AgentRecord agent, string userMsg, string assistantMsg,
        List<SessionCommand> commands, CancellationToken ct)
    {
        try
        {
            var sessionId = $"agent-llm-{agent.Id}";
            var now       = DateTimeOffset.UtcNow.ToString("o");

            var session = await sessionStore!.LoadAsync(sessionId) ?? new SessionRecord
            {
                Id        = sessionId,
                Title     = $"Agent: {agent.Name}",
                CreatedAt = now,
                UpdatedAt = now,
            };

            session.Messages.Add(new SessionMessage { Role = "user",      Content = userMsg });
            session.Messages.Add(new SessionMessage
            {
                Role     = "assistant",
                Content  = assistantMsg,
                Commands = commands.Count > 0 ? commands : null,
            });
            session.UpdatedAt = now;

            await sessionStore.UpsertAsync(session);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[WARN] Session-Persistenz für Agent '{agent.Name}': {ex.Message}");
        }
    }

    private static async Task<string> ExecuteCommandAsync(
        CommandExecutor executor, string command, ExecutionMode execMode, CancellationToken ct)
    {
        if (execMode == ExecutionMode.NoExec)
            return "Ausführung deaktiviert (no-exec Modus).";

        var results = await executor.ProcessAsync(
            [new ExtractedCommand(command, IsDangerous: false, DangerReason: null)], ct);

        if (results.Count == 0)
            return "Nicht ausgeführt.";

        var r = results[0];
        return r.WasExecuted
            ? $"Exit-Code: {r.ExitCode}\n{r.Output}"
            : $"Nicht ausgeführt (Modus: {execMode}).";
    }

    private static (int ExitCode, bool WasExecuted) ParseToolResult(string toolResult)
    {
        if (toolResult.StartsWith("Exit-Code: ", StringComparison.Ordinal))
        {
            var line = toolResult.Split('\n')[0];
            return int.TryParse(line["Exit-Code: ".Length..].Trim(), out var code)
                ? (code, true)
                : (0, true);
        }
        return (-1, false);
    }

    private static ExecutionMode ParseExecMode(string? mode) => mode?.ToLowerInvariant() switch
    {
        "auto-exec" or "autoexec" => ExecutionMode.AutoExec,
        "dry-run"   or "dryrun"   => ExecutionMode.DryRun,
        _                         => ExecutionMode.NoExec,
    };

    private List<ITool> BuildEnabledTools(AgentRecord agent)
    {
        if (toolRegistry is null || agent.EnabledTools.Count == 0)
            return [];
        var result = new List<ITool>();
        foreach (var name in agent.EnabledTools)
            if (toolRegistry.TryGet(name, out var t) && t is not null)
                result.Add(t);
        return result;
    }

    private static BashGPT.Providers.ToolDefinition ToLlmToolDefinition(BashGPT.Tools.Abstractions.ToolDefinition def)
    {
        var required = def.Parameters
            .Where(p => p.Required)
            .Select(p => p.Name)
            .ToArray();

        var properties = def.Parameters.ToDictionary(
            p => p.Name,
            p => p.Type == "object"
                ? (object)new { type = p.Type, description = p.Description, additionalProperties = new { type = "string" } }
                : (object)new { type = p.Type, description = p.Description });

        return new BashGPT.Providers.ToolDefinition(
            Name: def.Name,
            Description: def.Description,
            Parameters: new { type = "object", properties, required });
    }
}
