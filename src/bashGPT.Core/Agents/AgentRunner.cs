using BashGPT.Providers;
using BashGPT.Storage;

namespace BashGPT.Agents;

/// <summary>
/// Führt aktive Agenten zyklisch aus, meldet Zustandsänderungen und
/// leitet diese optional ans LLM weiter. Die gesamte Session wird gespeichert.
/// </summary>
public sealed class AgentRunner
{
    private const string SystemPrompt =
        "Du bist ein Monitoring-Assistent. Dir werden Zustandsänderungen von überwachten Systemen gemeldet. " +
        "Reagiere knapp und präzise auf die jeweilige Änderung. Keine Befehle ausführen.";

    private readonly AgentStore _store;
    private readonly IReadOnlyDictionary<AgentCheckType, IAgentCheck> _checks;
    private readonly ILlmProvider? _provider;
    private readonly SessionStore? _sessionStore;

    public AgentRunner(
        AgentStore store,
        IEnumerable<IAgentCheck> checks,
        ILlmProvider? provider = null,
        SessionStore? sessionStore = null)
    {
        _store = store;
        _checks = checks.ToDictionary(c => c.Type);
        _provider = provider;
        _sessionStore = sessionStore;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var session = CreateSession();

        while (!ct.IsCancellationRequested)
        {
            var agents = await _store.LoadAllAsync();

            foreach (var agent in agents.Where(a => a.IsActive))
            {
                if (!ShouldRun(agent))
                    continue;

                if (!_checks.TryGetValue(agent.Type, out var check))
                {
                    Console.Error.WriteLine($"[WARN] Kein Check für Typ {agent.Type} registriert.");
                    continue;
                }

                var now = DateTimeOffset.UtcNow;
                try
                {
                    var result = await check.RunAsync(agent, ct);

                    if (result.Changed)
                    {
                        Console.WriteLine($"[{now:HH:mm:ss}] {agent.Name}: {result.Message}");
                        await ReactWithLlmAsync(result.Message, agent, session, ct);
                    }

                    agent.LastHash = result.Hash;
                    agent.LastRun = now;
                    agent.LastMessage = result.Message;
                    agent.LastCheckSucceeded = result.Success;
                    agent.FailureCount = result.Success ? 0 : agent.FailureCount + 1;
                }
                catch (Exception ex)
                {
                    agent.FailureCount++;
                    agent.LastCheckSucceeded = false;
                    agent.LastRun = now;
                    Console.Error.WriteLine($"[ERROR] {agent.Name}: {ex.Message}");
                }

                await _store.UpsertAsync(agent);
            }

            await Task.Delay(1_000, ct).ConfigureAwait(false);
        }
    }

    private async Task ReactWithLlmAsync(string changeMessage, AgentRecord agent, SessionRecord session, CancellationToken ct)
    {
        if (_provider is null || _sessionStore is null)
            return;

        session.Messages.Add(new SessionMessage { Role = "user", Content = changeMessage });
        await _sessionStore.UpsertAsync(session);

        try
        {
            var systemPrompt = agent.SystemPrompt ?? SystemPrompt;
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
            };
            foreach (var m in session.Messages)
                messages.Add(new ChatMessage(m.Role == "user" ? ChatRole.User : ChatRole.Assistant, m.Content));

            Console.Write($"  → {_provider.Name}: ");
            var response = await _provider.ChatAsync(
                new LlmChatRequest(messages, Stream: true, OnToken: token => Console.Write(token)),
                ct);
            Console.WriteLine();

            session.Messages.Add(new SessionMessage { Role = "assistant", Content = response.Content });
            session.UpdatedAt = DateTimeOffset.UtcNow.ToString("o");
            await _sessionStore.UpsertAsync(session);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  [LLM-Fehler] {ex.Message}");
            session.Messages.RemoveAt(session.Messages.Count - 1);
        }
    }

    private static SessionRecord CreateSession()
    {
        var now = DateTimeOffset.UtcNow.ToString("o");
        return new SessionRecord
        {
            Id = $"agent-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            Title = $"Agent-Run {DateTimeOffset.Now:yyyy-MM-dd HH:mm}",
            CreatedAt = now,
            UpdatedAt = now,
            Messages = [],
        };
    }

    private static bool ShouldRun(AgentRecord agent)
    {
        if (agent.LastRun is null)
            return true;
        return DateTimeOffset.UtcNow >= agent.LastRun.Value.AddSeconds(agent.IntervalSeconds);
    }
}
