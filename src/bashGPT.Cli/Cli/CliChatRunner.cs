using BashGPT.Configuration;
using BashGPT.Providers;
using BashGPT.Shell;

namespace BashGPT.Cli;

/// <summary>
/// Verarbeitet Chat-Anfragen im CLI-Modus: streamt Tokens direkt auf die Console
/// und führt Shell-Befehle interaktiv oder automatisch aus.
/// </summary>
public class CliChatRunner(
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

        ChatOrchestrator.ApplyModelOverride(config, opts.Provider, opts.Model);

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

        var messages = new List<ChatMessage>();

        if (!opts.NoContext)
        {
            var ctx          = await contextCollector.CollectAsync(opts.IncludeDir);
            var systemPrompt = contextCollector.BuildSystemPrompt(ctx);
            messages.Add(new ChatMessage(ChatRole.System, systemPrompt));

            if (opts.Verbose)
                Console.Error.WriteLine($"[verbose] Kontext gesammelt: {ctx.WorkingDirectory}, Git: {ctx.Git?.Branch ?? "n/a"}");
        }

        messages.Add(new ChatMessage(ChatRole.User, opts.Prompt));

        var tools          = new[] { ToolDefinitions.Bash };
        var toolChoiceName = opts.ForceTools ? "bash" : null;

        Console.WriteLine();
        var firstResponse = await StreamAndCollectAsync(provider, messages, tools, toolChoiceName, ct);
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

        // Fallback: Befehle aus Text-Codeblöcken extrahieren
        var commands = BashCommandExtractor.Extract(firstResponse.Content);
        if (opts.Verbose && commands.Count > 0)
            Console.Error.WriteLine($"[verbose] Fallback aktiv: {commands.Count} Befehl(e) aus Text-Codeblöcken extrahiert");
        if (commands.Count == 0 || opts.ExecMode == ExecutionMode.NoExec)
            return 0;

        if (opts.Verbose)
            Console.Error.WriteLine($"[verbose] {commands.Count} Befehl(e) gefunden");

        var executor = new CommandExecutor(opts.ExecMode);
        var results  = await executor.ProcessAsync(commands, ct);

        var executed = results.Where(r => r.WasExecuted).ToList();
        if (executed.Count == 0)
            return 0;

        var followUp = CommandExecutor.BuildFollowUpContext(results);
        messages.Add(new ChatMessage(ChatRole.Assistant, firstResponse.Content));
        messages.Add(new ChatMessage(ChatRole.User, followUp));

        if (opts.Verbose)
            Console.Error.WriteLine("[verbose] Follow-up an LLM...");

        Console.WriteLine();
        await StreamAndCollectAsync(provider, messages, tools, toolChoiceName, ct);
        Console.WriteLine();

        return 0;
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
        var rounds   = 0;

        while (response.ToolCalls.Count > 0 && rounds < AppDefaults.MaxToolCallRounds)
        {
            var toolCalls = response.ToolCalls;
            if (opts.Verbose)
                Console.Error.WriteLine($"[verbose] Tool-Call-Runde {rounds + 1}: {toolCalls.Count} Call(s)");

            var (commands, errors) = ChatOrchestrator.ParseToolCalls(toolCalls);
            if (opts.Verbose)
            {
                foreach (var command in commands)
                    Console.Error.WriteLine($"[verbose] Tool '{command.ToolCall.Name}' -> {command.Command.Command}");
                foreach (var err in errors)
                    Console.Error.WriteLine($"[verbose] Tool-Call-Fehler ({err.ToolCall.Name}): {err.Error}");
            }

            var executor = new CommandExecutor(opts.ExecMode);
            await ChatOrchestrator.ExecuteToolCallRoundAsync(
                toolCalls, commands, errors, response.Content, messages, executor, ct);

            Console.WriteLine();
            response = await StreamAndCollectAsync(provider, messages, tools, toolChoiceName, ct);
            Console.WriteLine();

            rounds++;
        }
    }

    private static async Task<LlmChatResponse> StreamAndCollectAsync(
        ILlmProvider provider,
        List<ChatMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        string? toolChoiceName,
        CancellationToken ct)
    {
        var sb       = new System.Text.StringBuilder();
        var response = new LlmChatResponse("", []);
        try
        {
            response = await provider.ChatAsync(
                new LlmChatRequest(
                    Messages:       messages,
                    Tools:          tools,
                    ToolChoiceName: toolChoiceName,
                    ParallelToolCalls: false,
                    Stream:         true,
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
}
