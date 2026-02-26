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

        // LLM-Antwort streamen
        Console.WriteLine();
        var fullResponse = await StreamAndCollectAsync(provider, messages, ct);
        Console.WriteLine();

        if (string.IsNullOrWhiteSpace(fullResponse))
            return 0;

        // Bash-Befehle extrahieren und verarbeiten
        var commands = BashCommandExtractor.Extract(fullResponse);
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
        messages.Add(new ChatMessage(ChatRole.Assistant, fullResponse));
        messages.Add(new ChatMessage(ChatRole.User, followUp));

        if (opts.Verbose)
            Console.Error.WriteLine("[verbose] Follow-up an LLM...");

        Console.WriteLine();
        await StreamAndCollectAsync(provider, messages, ct);
        Console.WriteLine();

        return 0;
    }

    private static async Task<string> StreamAndCollectAsync(
        ILlmProvider provider,
        List<ChatMessage> messages,
        CancellationToken ct)
    {
        var sb = new System.Text.StringBuilder();
        try
        {
            await foreach (var token in provider.StreamAsync(messages, ct))
            {
                Console.Write(token);
                sb.Append(token);
            }
        }
        catch (LlmProviderException ex)
        {
            Console.Error.WriteLine($"\nFehler: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("\nAbgebrochen.");
        }
        return sb.ToString();
    }
}
