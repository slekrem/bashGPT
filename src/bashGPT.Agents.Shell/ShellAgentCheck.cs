using BashGPT.Providers;
using BashGPT.Shell;
using BashGPT.Storage;

namespace BashGPT.Agents;

public sealed class ShellAgentCheck(ILlmProvider? provider, ShellContextCollector? contextCollector = null, SessionStore? sessionStore = null) : IAgentCheck
{
    private const int MaxRounds = 5;

    public AgentCheckType Type => AgentCheckType.Shell;

    public async Task<AgentCheckResult> RunAsync(AgentRecord agent, CancellationToken ct)
    {
        if (provider is null)
            return new AgentCheckResult("noprovider", Changed: false, "Kein LLM-Provider konfiguriert.", Success: false);

        if (string.IsNullOrWhiteSpace(agent.LoopInstruction))
            return new AgentCheckResult("noinstruction", Changed: false, "Keine Loop-Anweisung konfiguriert.", Success: false);

        var execMode = ParseExecMode(agent.ExecMode);
        var executor = new CommandExecutor(execMode, output: Console.Out);

        // Shell-Kontext sammeln und System-Prompt aufbauen
        string systemPrompt;
        if (string.IsNullOrWhiteSpace(agent.SystemPrompt) && contextCollector is not null)
        {
            var ctx = await contextCollector.CollectAsync();
            systemPrompt = contextCollector.BuildSystemPrompt(ctx);
        }
        else
        {
            systemPrompt = string.IsNullOrWhiteSpace(agent.SystemPrompt)
                ? "Du bist ein autonomer Shell-Assistent. Führe die gegebene Aufgabe präzise aus."
                : agent.SystemPrompt;
        }

        var messages = new List<ChatMessage> { new(ChatRole.System, systemPrompt) };
        messages.Add(new(ChatRole.User, agent.LoopInstruction));

        var tools = new[] { ToolDefinitions.Bash };
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
                if (ToolCallParsing.TryGetCommand(call, out var command, out var parseError))
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
            await PersistToSessionAsync(agent, agent.LoopInstruction!, lastContent, commandsThisRun);

        var hash = string.IsNullOrEmpty(lastContent)
            ? "empty"
            : Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(lastContent)))[..16];

        return new AgentCheckResult(hash, Changed: false, lastContent, Success: true);
    }

    private async Task PersistToSessionAsync(
        AgentRecord agent, string userMsg, string assistantMsg,
        List<SessionCommand> commands)
    {
        try
        {
            var sessionId = $"agent-shell-{agent.Id}";
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

            await sessionStore!.UpsertAsync(session);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[WARN] Session-Persistenz für Shell-Agent '{agent.Name}': {ex.Message}");
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
}
