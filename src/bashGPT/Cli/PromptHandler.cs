using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;

namespace BashGPT.Cli;

/// <summary>
/// Kernlogik: Kontext sammeln → LLM anfragen → Befehle ausführen → Follow-up.
/// </summary>
public class PromptHandler(
    ConfigurationService configService,
    ShellContextCollector contextCollector) : IPromptHandler
{
    public async Task<int> RunAsync(CliOptions opts, CancellationToken ct = default)
    {
        AppConfig config;
        try
        {
            config = await configService.LoadAsync();
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Konfigurationsfehler: {ex.Message}");
            return 1;
        }

        // CLI-Overrides anwenden
        if (opts.Model is not null)
        {
            if (opts.Provider is ProviderType.Cerebras || config.DefaultProvider == ProviderType.Cerebras)
                config.Cerebras.Model = opts.Model;
            else
                config.Ollama.Model = opts.Model;
        }

        ILlmProvider provider;
        try
        {
            provider = ProviderFactory.Create(config, opts.Provider);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Provider-Fehler: {ex.Message}");
            return 1;
        }

        if (opts.Verbose)
            Console.Error.WriteLine($"[verbose] Provider: {provider.Name}, Modell: {provider.Model}");

        // Nachrichten aufbauen
        var messages = new List<ChatMessage>();

        if (!opts.NoContext)
        {
            var ctx = await contextCollector.CollectAsync(opts.IncludeDir);
            var systemPrompt = contextCollector.BuildSystemPrompt(ctx);
            messages.Add(new ChatMessage(ChatRole.System, systemPrompt));

            if (opts.Verbose)
                Console.Error.WriteLine($"[verbose] Kontext gesammelt: {ctx.WorkingDirectory}, Git: {ctx.Git?.Branch ?? "n/a"}");
        }

        messages.Add(new ChatMessage(ChatRole.User, opts.Prompt));

        var tools = new[] { ToolDefinitions.Bash };
        var toolChoiceName = opts.ForceTools ? "bash" : null;

        // LLM-Antwort streamen
        Console.WriteLine();
        var firstResponse = await StreamAndCollectAsync(
            provider,
            messages,
            tools,
            toolChoiceName,
            ct);
        Console.WriteLine();

        if (firstResponse.ToolCalls.Count > 0)
        {
            if (opts.Verbose)
                Console.Error.WriteLine($"[verbose] Tool-Calls empfangen: {firstResponse.ToolCalls.Count}");
            await HandleToolCallsAsync(provider, messages, firstResponse, tools, opts, toolChoiceName, ct);
            return 0;
        }

        if (string.IsNullOrWhiteSpace(firstResponse.Content))
            return 0;

        // Bash-Befehle extrahieren und verarbeiten
        var commands = BashCommandExtractor.Extract(firstResponse.Content);
        if (opts.Verbose && commands.Count > 0)
            Console.Error.WriteLine($"[verbose] Fallback aktiv: {commands.Count} Befehl(e) aus Text-Codeblöcken extrahiert");
        if (commands.Count == 0 || opts.ExecMode == ExecutionMode.NoExec)
            return 0;

        if (opts.Verbose)
            Console.Error.WriteLine($"[verbose] {commands.Count} Befehl(e) gefunden");

        var executor = new CommandExecutor(opts.ExecMode);
        var results = await executor.ProcessAsync(commands, ct);

        // Follow-up ans LLM nur wenn Befehle tatsächlich ausgeführt wurden
        var executed = results.Where(r => r.WasExecuted).ToList();
        if (executed.Count == 0)
            return 0;

        var followUp = CommandExecutor.BuildFollowUpContext(results);
        messages.Add(new ChatMessage(ChatRole.Assistant, firstResponse.Content));
        messages.Add(new ChatMessage(ChatRole.User, followUp));

        if (opts.Verbose)
            Console.Error.WriteLine("[verbose] Follow-up an LLM...");

        Console.WriteLine();
        await StreamAndCollectAsync(
            provider,
            messages,
            tools,
            toolChoiceName,
            ct);
        Console.WriteLine();

        return 0;
    }

    public async Task<ServerChatResult> RunServerChatAsync(
        ServerChatOptions opts,
        CancellationToken ct = default)
    {
        var logs = new List<string>();
        var commandResults = new List<CommandResult>();
        var totalInputTokens = 0;
        var totalOutputTokens = 0;

        AppConfig config;
        try
        {
            config = await configService.LoadAsync();
        }
        catch (InvalidOperationException ex)
        {
            return new ServerChatResult(
                Response: $"Konfigurationsfehler: {ex.Message}",
                Commands: [],
                Logs: [],
                UsedToolCalls: false);
        }

        if (opts.Model is not null)
        {
            if (opts.Provider is ProviderType.Cerebras || config.DefaultProvider == ProviderType.Cerebras)
                config.Cerebras.Model = opts.Model;
            else
                config.Ollama.Model = opts.Model;
        }

        ILlmProvider provider;
        try
        {
            provider = ProviderFactory.Create(config, opts.Provider);
        }
        catch (Exception ex)
        {
            return new ServerChatResult(
                Response: $"Provider-Fehler: {ex.Message}",
                Commands: [],
                Logs: [],
                UsedToolCalls: false);
        }

        if (opts.Verbose)
            logs.Add($"Provider: {provider.Name}, Modell: {provider.Model}");

        var messages = new List<ChatMessage>();

        if (!opts.NoContext)
        {
            var ctx = await contextCollector.CollectAsync(opts.IncludeDir);
            var systemPrompt = contextCollector.BuildSystemPrompt(ctx);
            messages.Add(new ChatMessage(ChatRole.System, systemPrompt));

            if (opts.Verbose)
                logs.Add($"Kontext gesammelt: {ctx.WorkingDirectory}, Git: {ctx.Git?.Branch ?? "n/a"}");
        }

        foreach (var msg in opts.History)
            messages.Add(msg);

        messages.Add(new ChatMessage(ChatRole.User, opts.Prompt));

        var tools = new[] { ToolDefinitions.Bash };
        var toolChoiceName = opts.ForceTools ? "bash" : null;
        var usedToolCalls = false;

        var firstResponse = await ChatOnceAsync(provider, messages, tools, toolChoiceName, ct);
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

            const int maxToolRounds = 3;
            var rounds = 0;
            while (currentResponse.ToolCalls.Count > 0 && rounds < maxToolRounds)
            {
                rounds++;
                var toolCalls = currentResponse.ToolCalls;

                if (opts.Verbose)
                    logs.Add($"Tool-Call-Runde {rounds}: {toolCalls.Count} Call(s)");

                var (commands, errors) = ParseToolCalls(toolCalls);
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
                    input: new StringReader(string.Empty));

                var roundResults = commands.Count > 0
                    ? await executor.ProcessAsync(commands.Select(c => c.Command).ToList(), ct)
                    : [];

                commandResults.AddRange(roundResults);
                messages.Add(ChatMessage.AssistantWithToolCalls(toolCalls, currentResponse.Content));

                var toolMessages = BuildToolResultMessages(toolCalls, commands, roundResults, errors);
                foreach (var msg in toolMessages)
                    messages.Add(msg);

                var nextResponse = await ChatOnceAsync(provider, messages, tools, toolChoiceName, ct);
                if (nextResponse.Error is not null)
                    return new ServerChatResult(nextResponse.Error, commandResults, logs, usedToolCalls);

                totalInputTokens  += nextResponse.Response.Usage?.InputTokens  ?? 0;
                totalOutputTokens += nextResponse.Response.Usage?.OutputTokens ?? 0;
                currentResponse = nextResponse.Response;
            }

            if (currentResponse.ToolCalls.Count > 0)
            {
                var loopGuardMessage =
                    "Tool-Call-Schleife erkannt und beendet. " +
                    "Bitte nutze nicht-interaktive Befehle (z. B. 'ps aux --sort=-%cpu | head' statt 'top').";
                if (opts.Verbose)
                    logs.Add($"Maximale Tool-Call-Runden erreicht ({maxToolRounds}).");
                var responseText = string.IsNullOrWhiteSpace(currentResponse.Content)
                    ? loopGuardMessage
                    : currentResponse.Content;
                return new ServerChatResult(responseText, commandResults, logs, usedToolCalls, BuildUsage());
            }

            return new ServerChatResult(currentResponse.Content, commandResults, logs, usedToolCalls, BuildUsage());
        }

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
            input: new StringReader(string.Empty));

        var fallbackResults = await fallbackExecutor.ProcessAsync(fallbackCommands, ct);
        commandResults.AddRange(fallbackResults);

        var executed = fallbackResults.Where(r => r.WasExecuted).ToList();
        if (executed.Count == 0)
            return new ServerChatResult(currentResponse.Content, commandResults, logs, usedToolCalls, BuildUsage());

        var followUp = CommandExecutor.BuildFollowUpContext(fallbackResults);
        messages.Add(new ChatMessage(ChatRole.Assistant, currentResponse.Content));
        messages.Add(new ChatMessage(ChatRole.User, followUp));

        var followUpResponse = await ChatOnceAsync(provider, messages, tools, toolChoiceName, ct);
        if (followUpResponse.Error is not null)
            return new ServerChatResult(followUpResponse.Error, commandResults, logs, usedToolCalls, BuildUsage());

        totalInputTokens  += followUpResponse.Response.Usage?.InputTokens  ?? 0;
        totalOutputTokens += followUpResponse.Response.Usage?.OutputTokens ?? 0;
        return new ServerChatResult(followUpResponse.Response.Content, commandResults, logs, usedToolCalls, BuildUsage());

        TokenUsage? BuildUsage() => totalInputTokens > 0 || totalOutputTokens > 0
            ? new TokenUsage(totalInputTokens, totalOutputTokens)
            : null;
    }

    private static async Task<(LlmChatResponse Response, string? Error)> ChatOnceAsync(
        ILlmProvider provider,
        List<ChatMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        string? toolChoiceName,
        CancellationToken ct)
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
                    OnToken: token => tokenBuffer.Append(token)),
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

    private static async Task<LlmChatResponse> StreamAndCollectAsync(
        ILlmProvider provider,
        List<ChatMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        string? toolChoiceName,
        CancellationToken ct)
    {
        var sb = new System.Text.StringBuilder();
        var response = new LlmChatResponse("", []);
        try
        {
            response = await provider.ChatAsync(
                new LlmChatRequest(
                    Messages: messages,
                    Tools: tools,
                    ToolChoiceName: toolChoiceName,
                    ParallelToolCalls: false,
                    Stream: true,
                    OnToken: token =>
                    {
                        Console.Write(token);
                        sb.Append(token);
                    }),
                ct);
        }
        catch (LlmProviderException ex)
        {
            Console.Error.WriteLine($"\nFehler: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("\nAbgebrochen.");
        }
        return response with { Content = string.IsNullOrEmpty(response.Content) ? sb.ToString() : response.Content };
    }

    private static async Task HandleToolCallsAsync(
        ILlmProvider provider,
        List<ChatMessage> messages,
        LlmChatResponse initialResponse,
        IReadOnlyList<ToolDefinition> tools,
        CliOptions opts,
        string? toolChoiceName,
        CancellationToken ct)
    {
        var response = initialResponse;
        var rounds = 0;

        while (response.ToolCalls.Count > 0 && rounds < 3)
        {
            var toolCalls = response.ToolCalls;
            if (opts.Verbose)
                Console.Error.WriteLine($"[verbose] Tool-Call-Runde {rounds + 1}: {toolCalls.Count} Call(s)");
            var (commands, errors) = ParseToolCalls(toolCalls);
            if (opts.Verbose)
            {
                foreach (var command in commands)
                    Console.Error.WriteLine($"[verbose] Tool '{command.ToolCall.Name}' -> {command.Command.Command}");
                foreach (var err in errors)
                    Console.Error.WriteLine($"[verbose] Tool-Call-Fehler ({err.ToolCall.Name}): {err.Error}");
            }

            var executor = new CommandExecutor(opts.ExecMode);
            var results = commands.Count > 0
                ? await executor.ProcessAsync(commands.Select(c => c.Command).ToList(), ct)
                : [];

            messages.Add(ChatMessage.AssistantWithToolCalls(toolCalls, response.Content));

            var toolMessages = BuildToolResultMessages(toolCalls, commands, results, errors);
            foreach (var msg in toolMessages)
                messages.Add(msg);

            Console.WriteLine();
            response = await StreamAndCollectAsync(
                provider,
                messages,
                tools,
                toolChoiceName,
                ct);
            Console.WriteLine();

            rounds++;
        }
    }

    private sealed record ParsedToolCommand(ToolCall ToolCall, ExtractedCommand Command);
    private sealed record ToolCallError(ToolCall ToolCall, string Error);

    private static (List<ParsedToolCommand> Commands, List<ToolCallError> Errors) ParseToolCalls(
        IReadOnlyList<ToolCall> toolCalls)
    {
        var commands = new List<ParsedToolCommand>();
        var errors = new List<ToolCallError>();

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

    private static IReadOnlyList<ChatMessage> BuildToolResultMessages(
        IReadOnlyList<ToolCall> toolCalls,
        IReadOnlyList<ParsedToolCommand> commands,
        IReadOnlyList<CommandResult> results,
        IReadOnlyList<ToolCallError> errors)
    {
        var messages = new List<ChatMessage>();

        var commandResults = results.ToList();
        for (var i = 0; i < commands.Count; i++)
        {
            var call = commands[i].ToolCall;
            var result = i < commandResults.Count
                ? commandResults[i]
                : new CommandResult(commands[i].Command.Command, -1, "Keine Ausführung.", false);

            var content = FormatToolResult(result);
            messages.Add(ChatMessage.ToolResult(
                content,
                toolCallId: call.Id,
                toolName: call.Name));
        }

        foreach (var err in errors)
        {
            var content = $"Fehler: {err.Error}";
            messages.Add(ChatMessage.ToolResult(
                content,
                toolCallId: err.ToolCall.Id,
                toolName: err.ToolCall.Name));
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
