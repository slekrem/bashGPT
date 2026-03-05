using BashGPT.Agents;

namespace BashGPT.Tests.Agents;

public class AgentRunnerTests : IDisposable
{
    private readonly string _tempFile;
    private readonly AgentStore _store;

    public AgentRunnerTests()
    {
        _tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"agents-runner-test-{Guid.NewGuid():N}.json");
        _store = new AgentStore(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
        if (File.Exists(_tempFile + ".tmp")) File.Delete(_tempFile + ".tmp");
    }

    [Fact]
    public async Task AgentRunner_ChangeDetected_EmitsOutput()
    {
        var agent = new AgentRecord
        {
            Id = "ag-test0001",
            Name = "change-agent",
            Type = AgentCheckType.GitStatus,
            IntervalSeconds = 0,
            IsActive = true,
            LastHash = "old-hash",
        };
        await _store.UpsertAsync(agent);

        var check = new FakeCheck(AgentCheckType.GitStatus,
            new AgentCheckResult("new-hash", Changed: true, "Etwas hat sich geändert", Success: true));

        var output = await CaptureConsoleAsync(async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));
            var runner = new AgentRunner(_store, [check]);
            try { await runner.RunAsync(cts.Token); }
            catch (OperationCanceledException) { }
        });

        Assert.Contains("change-agent", output);
        Assert.Contains("Etwas hat sich geändert", output);
    }

    [Fact]
    public async Task AgentRunner_NoChange_EmitsNothing()
    {
        var agent = new AgentRecord
        {
            Id = "ag-test0002",
            Name = "nochange-agent",
            Type = AgentCheckType.GitStatus,
            IntervalSeconds = 0,
            IsActive = true,
            LastHash = "same-hash",
        };
        await _store.UpsertAsync(agent);

        var check = new FakeCheck(AgentCheckType.GitStatus,
            new AgentCheckResult("same-hash", Changed: false, "Keine Änderungen.", Success: true));

        var output = await CaptureConsoleAsync(async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));
            var runner = new AgentRunner(_store, [check]);
            try { await runner.RunAsync(cts.Token); }
            catch (OperationCanceledException) { }
        });

        Assert.DoesNotContain("nochange-agent", output);
    }

    [Fact]
    public async Task AgentRunner_CheckFails_DoesNotStopLoop()
    {
        var agent = new AgentRecord
        {
            Id = "ag-test0003",
            Name = "failing-agent",
            Type = AgentCheckType.GitStatus,
            IntervalSeconds = 0,
            IsActive = true,
        };
        await _store.UpsertAsync(agent);

        var check = new ThrowingCheck(AgentCheckType.GitStatus);

        // Runner should complete the run without throwing
        var ranToCompletion = false;
        await CaptureConsoleAsync(async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));
            var runner = new AgentRunner(_store, [check]);
            try { await runner.RunAsync(cts.Token); ranToCompletion = true; }
            catch (OperationCanceledException) { ranToCompletion = true; }
        });

        Assert.True(ranToCompletion, "Runner sollte nicht durch Check-Exception abbrechen.");

        var updated = await _store.LoadAsync(agent.Id);
        Assert.NotNull(updated);
        Assert.True(updated.FailureCount > 0);
        Assert.False(updated.LastCheckSucceeded);
    }

    // ── Test Doubles ──────────────────────────────────────────────────────────

    private sealed class FakeCheck(AgentCheckType type, AgentCheckResult result) : IAgentCheck
    {
        public AgentCheckType Type => type;
        public Task<AgentCheckResult> RunAsync(AgentRecord agent, CancellationToken ct) =>
            Task.FromResult(result);
    }

    private sealed class ThrowingCheck(AgentCheckType type) : IAgentCheck
    {
        public AgentCheckType Type => type;
        public Task<AgentCheckResult> RunAsync(AgentRecord agent, CancellationToken ct) =>
            throw new InvalidOperationException("Simulierter Check-Fehler");
    }

    private static async Task<string> CaptureConsoleAsync(Func<Task> action)
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            await action();
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
