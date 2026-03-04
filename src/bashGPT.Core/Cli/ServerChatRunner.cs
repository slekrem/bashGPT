using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;

namespace BashGPT.Cli;

/// <summary>
/// Verarbeitet Chat-Anfragen im Server-Modus: sammelt alle Ergebnisse in-memory
/// und gibt ein strukturiertes <see cref="ServerChatResult"/> zurück.
/// </summary>
public class ServerChatRunner(
    ConfigurationService configService,
    ShellContextCollector contextCollector,
    ILlmProvider? providerOverride = null) : IPromptHandler
{
    public async Task<ServerChatResult> RunServerChatAsync(
        ServerChatOptions opts,
        CancellationToken ct = default)
    {
        var logs              = new List<string>();
        var commandResults    = new List<CommandResult>();
        var totalInputTokens  = 0;
        var totalOutputTokens = 0;

        ILlmProvider provider;
        if (providerOverride is not null)
        {
            provider = providerOverride;
        }
        else
        {
            AppConfig config;
            try
            {
                config = await configService.LoadAsync();
            }
            catch (InvalidOperationException ex)
            {
                return new ServerChatResult(
                    Response:      $"Konfigurationsfehler: {ex.Message}",
                    Commands:      [],
                    Logs:          [],
                    UsedToolCalls: false);
            }

            ChatOrchestrator.ApplyModelOverride(config, opts.Provider, opts.Model);

            try
            {
                provider = ProviderFactory.Create(config, opts.Provider);
            }
            catch (Exception ex)
            {
                return new ServerChatResult(
                    Response:      $"Provider-Fehler: {ex.Message}",
                    Commands:      [],
                    Logs:          [],
                    UsedToolCalls: false);
            }
        }

        if (opts.Verbose)
            logs.Add($"Provider: {provider.Name}, Modell: {provider.Model}");

        var messages = new List<ChatMessage>();

        if (!opts.NoContext)
        {
            var ctx          = await contextCollector.CollectAsync(opts.IncludeDir);
            var systemPrompt = contextCollector.BuildSystemPrompt(ctx);
            messages.Add(new ChatMessage(ChatRole.System, systemPrompt));

            if (opts.Verbose)
                logs.Add($"Kontext gesammelt: {ctx.WorkingDirectory}, Git: {ctx.Git?.Branch ?? "n/a"}");
        }

        foreach (var msg in opts.History)
            messages.Add(msg);

        messages.Add(new ChatMessage(ChatRole.User, opts.Prompt));

        var tools          = new[] { ToolDefinitions.Bash };
        var toolChoiceName = opts.ForceTools ? "bash" : null;
        var usedToolCalls  = false;

        var firstResponse = await ChatOrchestrator.ChatOnceAsync(provider, messages, tools, toolChoiceName, ct);
        if (firstResponse.Error is not null)
            return new ServerChatResult(firstResponse.Error, commandResults, logs, usedToolCalls);

        totalInputTokens  += firstResponse.Response.Usage?.InputTokens  ?? 0;
        totalOutputTokens += firstResponse.Response.Usage?.OutputTokens ?? 0;

        var currentResponse = firstResponse.Response;

        if (currentResponse.ToolCalls.Count > 0)
        {
            usedToolCalls = true;
            if (opts.Verbose)
                logs.Add($"Tool-Calls empfangen: {currentResponse.ToolCalls.Count}");

            var rounds            = 0;
            var previousToolCalls = (IReadOnlyList<ToolCall>?)null;
            var loopDetected      = false;

            while (currentResponse.ToolCalls.Count > 0 && rounds < AppDefaults.MaxToolCallRounds)
            {
                if (AppDefaults.DetectLoop(previousToolCalls, currentResponse.ToolCalls))
                {
                    loopDetected = true;
                    break;
                }
                previousToolCalls = currentResponse.ToolCalls;
                rounds++;
                var toolCalls = currentResponse.ToolCalls;

                if (opts.Verbose)
                    logs.Add($"Tool-Call-Runde {rounds}: {toolCalls.Count} Call(s)");

                var (commands, errors) = ChatOrchestrator.ParseToolCalls(toolCalls);
                if (opts.Verbose)
                {
                    foreach (var command in commands)
                        logs.Add($"Tool '{command.ToolCall.Name}' -> {command.Command.Command}");
                    foreach (var err in errors)
                        logs.Add($"Tool-Call-Fehler ({err.ToolCall.Name}): {err.Error}");
                }

                var effectiveExecMode = opts.ExecMode;
                if (effectiveExecMode == ExecutionMode.Ask)
                {
                    effectiveExecMode = ExecutionMode.DryRun;
                    if (opts.Verbose)
                        logs.Add("ExecMode 'ask' ist im Server-Modus nicht interaktiv, verwende 'dry-run'.");
                }

                var executor = new CommandExecutor(
                    effectiveExecMode,
                    output: TextWriter.Null,
                    input:  new StringReader(string.Empty));

                var roundResults = await ChatOrchestrator.ExecuteToolCallRoundAsync(
                    toolCalls, commands, errors, currentResponse.Content, messages, executor, ct);

                commandResults.AddRange(roundResults);

                var nextResponse = await ChatOrchestrator.ChatOnceAsync(provider, messages, tools, toolChoiceName, ct);
                if (nextResponse.Error is not null)
                    return new ServerChatResult(nextResponse.Error, commandResults, logs, usedToolCalls);

                totalInputTokens  += nextResponse.Response.Usage?.InputTokens  ?? 0;
                totalOutputTokens += nextResponse.Response.Usage?.OutputTokens ?? 0;
                currentResponse    = nextResponse.Response;
            }

            if (loopDetected || currentResponse.ToolCalls.Count > 0)
            {
                string guardMessage;
                if (loopDetected)
                {
                    if (opts.Verbose)
                        logs.Add("Tool-Call-Schleife erkannt (identische Befehle wiederholt).");
                    guardMessage = AppDefaults.LoopDetectedMessage;
                }
                else
                {
                    if (opts.Verbose)
                        logs.Add($"Maximale Tool-Call-Runden erreicht ({AppDefaults.MaxToolCallRounds}).");
                    guardMessage = AppDefaults.MaxRoundsReachedMessage;
                }
                var responseText = string.IsNullOrWhiteSpace(currentResponse.Content)
                    ? guardMessage
                    : currentResponse.Content;
                return new ServerChatResult(responseText, commandResults, logs, usedToolCalls, BuildUsage());
            }

            return new ServerChatResult(currentResponse.Content, commandResults, logs, usedToolCalls, BuildUsage());
        }

        // Fallback: Befehle aus Text-Codeblöcken extrahieren
        var fallbackCommands = BashCommandExtractor.Extract(currentResponse.Content);
        if (opts.Verbose && fallbackCommands.Count > 0)
            logs.Add($"Fallback aktiv: {fallbackCommands.Count} Befehl(e) aus Text-Codeblöcken extrahiert");

        if (fallbackCommands.Count == 0 || opts.ExecMode == ExecutionMode.NoExec)
            return new ServerChatResult(currentResponse.Content, commandResults, logs, usedToolCalls, BuildUsage());

        var fallbackExecMode = opts.ExecMode == ExecutionMode.Ask
            ? ExecutionMode.DryRun
            : opts.ExecMode;

        var fallbackExecutor = new CommandExecutor(
            fallbackExecMode,
            output: TextWriter.Null,
            input:  new StringReader(string.Empty));

        var fallbackResults = await fallbackExecutor.ProcessAsync(fallbackCommands, ct);
        commandResults.AddRange(fallbackResults);

        var executed = fallbackResults.Where(r => r.WasExecuted).ToList();
        if (executed.Count == 0)
            return new ServerChatResult(currentResponse.Content, commandResults, logs, usedToolCalls, BuildUsage());

        var followUp = CommandExecutor.BuildFollowUpContext(fallbackResults);
        messages.Add(new ChatMessage(ChatRole.Assistant, currentResponse.Content));
        messages.Add(new ChatMessage(ChatRole.User, followUp));

        var followUpResponse = await ChatOrchestrator.ChatOnceAsync(provider, messages, tools, toolChoiceName, ct);
        if (followUpResponse.Error is not null)
            return new ServerChatResult(followUpResponse.Error, commandResults, logs, usedToolCalls, BuildUsage());

        totalInputTokens  += followUpResponse.Response.Usage?.InputTokens  ?? 0;
        totalOutputTokens += followUpResponse.Response.Usage?.OutputTokens ?? 0;
        return new ServerChatResult(followUpResponse.Response.Content, commandResults, logs, usedToolCalls, BuildUsage());

        TokenUsage? BuildUsage() => totalInputTokens > 0 || totalOutputTokens > 0
            ? new TokenUsage(totalInputTokens, totalOutputTokens)
            : null;
    }
}
