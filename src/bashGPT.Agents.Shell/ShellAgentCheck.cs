using BashGPT.Providers;
using BashGPT.Shell;
using BashGPT.Storage;

namespace BashGPT.Agents;

public sealed class ShellAgentCheck(ILlmProvider? provider, ShellContextCollector? contextCollector = null, SessionStore? sessionStore = null) : IAgentCheck
{
    public AgentCheckType Type => AgentCheckType.Shell;

    public async Task<AgentCheckResult> RunAsync(AgentRecord agent, CancellationToken ct)
    {
        if (provider is null)
            return new AgentCheckResult("noprovider", Changed: false, "Kein LLM-Provider konfiguriert.", Success: false);

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

        var loopInstruction = string.IsNullOrWhiteSpace(agent.LoopInstruction)
            ? "Analysiere den aktuellen Systemzustand und gib einen kurzen Statusbericht aus."
            : agent.LoopInstruction;

        ct.ThrowIfCancellationRequested();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, loopInstruction),
        };

        var response = await provider.ChatAsync(
            new LlmChatRequest(messages, Stream: false),
            ct);

        var content = response.Content;

        if (sessionStore is not null && !string.IsNullOrEmpty(content))
            await PersistToSessionAsync(agent, loopInstruction, content);

        var hash = string.IsNullOrEmpty(content)
            ? "empty"
            : Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(content)))[..16];

        return new AgentCheckResult(hash, Changed: false, content, Success: true);
    }

    private async Task PersistToSessionAsync(AgentRecord agent, string userMsg, string assistantMsg)
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
            session.Messages.Add(new SessionMessage { Role = "assistant", Content = assistantMsg });
            session.UpdatedAt = now;

            await sessionStore!.UpsertAsync(session);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[WARN] Session-Persistenz für Shell-Agent '{agent.Name}': {ex.Message}");
        }
    }
}
