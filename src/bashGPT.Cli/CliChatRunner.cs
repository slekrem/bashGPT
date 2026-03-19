using bashGPT.Core;
using bashGPT.Core.Chat;
using bashGPT.Core.Configuration;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Providers;
using bashGPT.Cli.Shell;
using BashGPT.Shell;

namespace bashGPT.Cli;

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

        var tools = new[] { CliBashTool.Definition };
        var toolChoiceName = forceTools ? "bash" : null;
        var chatSession = ChatSessionFactory.Create(
            provider,
            tools,
            [],
            opts.Prompt,
            toolChoiceName);

        Console.WriteLine();
        var firstResponse = await StreamAndCollectAsync(chatSession, ct);
        Console.WriteLine();

        if (firstResponse.ToolCalls.Count > 0)
        {
            if (opts.Verbose)
                Console.Error.WriteLine($"[verbose] Received tool calls: {firstResponse.ToolCalls.Count}");

            await HandleToolCallsAsync(
                chatSession,
                firstResponse,
                opts,
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
        chatSession.Messages.Add(new ChatMessage(ChatRole.Assistant, firstResponse.Content));
        chatSession.Messages.Add(new ChatMessage(ChatRole.User, followUp));

        if (opts.Verbose)
            Console.Error.WriteLine("[verbose] Sending follow-up to the LLM...");

        Console.WriteLine();
        await StreamAndCollectAsync(chatSession, ct);
        Console.WriteLine();

        return 0;
    }

    private static async Task HandleToolCallsAsync(
        ChatSessionState chatSession,
        LlmChatResponse initialResponse,
        CliOptions opts,
        int commandTimeoutSeconds,
        CancellationToken ct)
    {
        var loopResult = await chatSession.RunToolCallLoopAsync(
            initialResponse,
            async (round, response) =>
            {
                var toolCalls = response.ToolCalls;
                if (opts.Verbose)
                    Console.Error.WriteLine($"[verbose] Tool call round {round}: {toolCalls.Count} call(s)");

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
                    chatSession.Messages,
                    executor,
                    ct);

                Console.WriteLine();
            },
            beforeNextCall: null,
            ct);

        if (loopResult.Error is not null)
            Console.Error.WriteLine($"\n{loopResult.Error}");

        Console.WriteLine();
    }

    private static async Task<LlmChatResponse> StreamAndCollectAsync(
        ChatSessionState chatSession,
        CancellationToken ct)
    {
        var response = await chatSession.CallOnceAsync(Console.Write, ct);
        if (response.Error is not null)
            Console.Error.WriteLine($"\n{response.Error}");

        return response.Response;
    }
}
