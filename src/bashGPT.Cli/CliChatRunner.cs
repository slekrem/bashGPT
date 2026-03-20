using bashGPT.Core;
using bashGPT.Core.Chat;
using bashGPT.Core.Configuration;
using bashGPT.Core.Models.Providers;
using bashGPT.Core.Providers.Abstractions;
using bashGPT.Core.Providers;
using bashGPT.Cli.Shell;
using bashGPT.Shell;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Cli;

/// <summary>
/// Processes chat requests in CLI mode by streaming tokens to the console
/// and executing shell commands automatically.
/// </summary>
public class CliChatRunner
{
    private readonly ConfigurationService _configService;
    private readonly IReadOnlyDictionary<string, ITool> _pluginTools;

    public CliChatRunner(ConfigurationService configService, IReadOnlyList<ITool>? pluginTools = null)
    {
        _configService = configService;
        _pluginTools = pluginTools is { Count: > 0 }
            ? pluginTools.ToDictionary(t => t.Definition.Name, StringComparer.Ordinal)
            : new Dictionary<string, ITool>();
    }

    public async Task<int> RunAsync(CliOptions opts, CancellationToken ct = default)
    {
        var pluginToolDefs = _pluginTools.Values
            .Select(t => ToProviderDefinition(t.Definition))
            .ToArray();

        var tools = pluginToolDefs.Length > 0
            ? [CliBashTool.Definition, .. pluginToolDefs]
            : new[] { CliBashTool.Definition };

        var bootstrap = await ChatSessionBootstrap.CreateAsync(
            _configService,
            opts.Model,
            tools,
            [],
            opts.Prompt,
            toolChoiceFactory: config => (opts.ForceTools ?? config.DefaultForceTools) ? "bash" : null);

        if (bootstrap.Error is not null || bootstrap.Provider is null || bootstrap.Session is null)
        {
            Console.Error.WriteLine(bootstrap.Error ?? "Failed to initialize provider.");
            return 1;
        }

        var provider = bootstrap.Provider;

        if (opts.Verbose)
            Console.Error.WriteLine($"[verbose] Provider: {provider.Name}, model: {provider.Model}");

        var chatSession = bootstrap.Session;

        var runResult = await RunChatSessionAsync(chatSession, opts, ct);
        var firstResponse = runResult.Response;

        if (runResult.Error is not null)
        {
            Console.Error.WriteLine($"\n{runResult.Error}");
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

    private async Task<ChatSessionRunResult> RunChatSessionAsync(
        ChatSessionState chatSession,
        CliOptions opts,
        CancellationToken ct)
    {
        Console.WriteLine();
        var result = await ChatSessionRunner.RunAsync(
            chatSession,
            Console.Write,
            enableToolCalls: true,
            async (round, response) =>
            {
                var toolCalls = response.ToolCalls;
                if (opts.Verbose)
                    Console.Error.WriteLine($"[verbose] Tool call round {round}: {toolCalls.Count} call(s)");

                // Separate tool calls: plugin tools are executed directly; everything else is treated as a bash call.
                var pluginCalls = toolCalls.Where(tc => _pluginTools.ContainsKey(tc.Name)).ToList();
                var bashCalls = toolCalls.Where(tc => !_pluginTools.ContainsKey(tc.Name)).ToList();

                var (commands, errors) = CliToolCallOrchestrator.ParseToolCalls(bashCalls);
                if (opts.Verbose)
                {
                    foreach (var command in commands)
                        Console.Error.WriteLine($"[verbose] Tool '{command.ToolCall.Name}' -> {command.Command.Command}");

                    foreach (var err in errors)
                        Console.Error.WriteLine($"[verbose] Tool call error ({err.ToolCall.Name}): {err.Error}");

                    foreach (var call in pluginCalls)
                        Console.Error.WriteLine($"[verbose] Plugin tool '{call.Name}'");
                }

                var executor = new CommandExecutor(commandTimeoutSeconds: AppDefaults.CommandTimeoutSeconds);

                // Execute bash tool calls and build assistant + bash result messages.
                await CliToolCallOrchestrator.ExecuteToolCallRoundAsync(
                    toolCalls,
                    commands,
                    errors,
                    response.Content,
                    chatSession.Messages,
                    executor,
                    ct);

                // Execute plugin tool calls and append their result messages.
                foreach (var call in pluginCalls)
                {
                    var tool = _pluginTools[call.Name];
                    var toolCall = new Tools.Abstractions.ToolCall(call.Name, call.ArgumentsJson);
                    string resultContent;
                    try
                    {
                        var toolResult = await tool.ExecuteAsync(toolCall, ct);
                        resultContent = toolResult.Content;
                    }
                    catch (Exception ex)
                    {
                        resultContent = $"Error: {ex.Message}";
                    }

                    chatSession.Messages.Add(ChatMessage.ToolResult(
                        resultContent,
                        toolCallId: call.Id,
                        toolName: call.Name));
                }

                Console.WriteLine();
            },
            beforeNextCall: null,
            ct);

        Console.WriteLine();
        return result;
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

    private static ProviderToolDefinition ToProviderDefinition(ToolDefinition definition)
    {
        var required = definition.Parameters
            .Where(p => p.Required)
            .Select(p => p.Name)
            .ToArray();

        var properties = definition.Parameters.ToDictionary(
            p => p.Name,
            p => (object)new { type = p.Type, description = p.Description });

        return new ProviderToolDefinition(
            Name: definition.Name,
            Description: definition.Description,
            Parameters: new { type = "object", properties, required });
    }
}
