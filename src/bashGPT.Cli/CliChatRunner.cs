using bashGPT.Core;
using bashGPT.Core.Chat;
using bashGPT.Core.Configuration;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Providers;
using BashGPT.Shell;

namespace BashGPT.Cli;

/// <summary>
/// Processes chat requests in CLI mode by streaming tokens to the console
/// and executing shell commands automatically.
/// </summary>
public class CliChatRunner(ConfigurationService configService)
{
    public async Task<int> RunAsync(CliOptions opts, CancellationToken ct = default)
    {
        var bootstrap = await LlmProviderBootstrap.CreateAsync(configService, opts.Model);
        if (bootstrap.Error is not null || bootstrap.Config is null || bootstrap.Provider is null)
        {
            Console.Error.WriteLine(bootstrap.Error ?? "Failed to initialize provider.");
            return 1;
        }

        var config = bootstrap.Config;
        var provider = bootstrap.Provider;
        var forceTools = opts.ForceTools ?? config.DefaultForceTools;

        if (opts.Verbose)
            Console.Error.WriteLine($"[verbose] Provider: {provider.Name}, model: {provider.Model}");

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, opts.Prompt)
        };

        var tools = new[] { CliBashTool.Definition };
        var toolChoiceName = forceTools ? "bash" : null;

        Console.WriteLine();
        var firstResponse = await StreamAndCollectAsync(provider, messages, tools, toolChoiceName, ct);
        Console.WriteLine();

        if (firstResponse.ToolCalls.Count > 0)
        {
            if (opts.Verbose)
                Console.Error.WriteLine($"[verbose] Received tool calls: {firstResponse.ToolCalls.Count}");

            await HandleToolCallsAsync(
                provider,
                messages,
                firstResponse,
                tools,
                opts,
                toolChoiceName,
                AppDefaults.CommandTimeoutSeconds,
                ct);
            return 0;
        }

        if (string.IsNullOrWhiteSpace(firstResponse.Content))
            return 0;

        var commands = BashCommandExtractor.Extract(firstResponse.Content);
        if (opts.Verbose && commands.Count > 0)
        {
            Console.Error.WriteLine(
                $"[verbose] Fallback active: extracted {commands.Count} command(s) from text code blocks");
        }

        if (commands.Count == 0)
            return 0;

        if (opts.Verbose)
            Console.Error.WriteLine($"[verbose] Found {commands.Count} command(s)");

        var executor = new CommandExecutor(commandTimeoutSeconds: AppDefaults.CommandTimeoutSeconds);
        var results = await executor.ProcessAsync(commands, ct);

        var executed = results.Where(r => r.WasExecuted).ToList();
        if (executed.Count == 0)
            return 0;

        var followUp = CommandExecutor.BuildFollowUpContext(results);
        messages.Add(new ChatMessage(ChatRole.Assistant, firstResponse.Content));
        messages.Add(new ChatMessage(ChatRole.User, followUp));

        if (opts.Verbose)
            Console.Error.WriteLine("[verbose] Sending follow-up to the LLM...");

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
        int commandTimeoutSeconds,
        CancellationToken ct)
    {
        var response = initialResponse;
        var rounds = 0;

        while (response.ToolCalls.Count > 0)
        {
            rounds++;

            var toolCalls = response.ToolCalls;
            if (opts.Verbose)
                Console.Error.WriteLine($"[verbose] Tool call round {rounds}: {toolCalls.Count} call(s)");

            var (commands, errors) = CliToolCallOrchestrator.ParseToolCalls(toolCalls);
            if (opts.Verbose)
            {
                foreach (var command in commands)
                    Console.Error.WriteLine($"[verbose] Tool '{command.ToolCall.Name}' -> {command.Command.Command}");

                foreach (var err in errors)
                    Console.Error.WriteLine($"[verbose] Tool call error ({err.ToolCall.Name}): {err.Error}");
            }

            var executor = new CommandExecutor(commandTimeoutSeconds: commandTimeoutSeconds);
            await CliToolCallOrchestrator.ExecuteToolCallRoundAsync(
                toolCalls,
                commands,
                errors,
                response.Content,
                messages,
                executor,
                ct);

            Console.WriteLine();
            response = await StreamAndCollectAsync(provider, messages, tools, toolChoiceName, ct);
            Console.WriteLine();
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
            Console.Error.WriteLine($"\nError: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("\nCancelled.");
        }

        return response with
        {
            Content = string.IsNullOrEmpty(response.Content) ? sb.ToString() : response.Content
        };
    }
}
