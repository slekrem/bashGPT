using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;

namespace BashGPT.Cli;

/// <summary>
/// Kernlogik: Kontext sammeln → LLM anfragen → Befehle ausführen → Follow-up.
/// </summary>
public class PromptHandler(
    ConfigurationService configService,
    ShellContextCollector contextCollector)
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

        // LLM-Antwort streamen
        Console.WriteLine();
        var firstResponse = await StreamAndCollectAsync(
            provider,
            messages,
            tools,
            toolChoiceName: "bash",
            ct);
        Console.WriteLine();

        if (firstResponse.ToolCalls.Count > 0)
        {
            if (opts.Verbose)
                Console.Error.WriteLine($"[verbose] Tool-Calls empfangen: {firstResponse.ToolCalls.Count}");
            await HandleToolCallsAsync(provider, messages, firstResponse, tools, opts, ct);
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
            toolChoiceName: "bash",
            ct);
        Console.WriteLine();

        return 0;
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
                toolChoiceName: "bash",
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
