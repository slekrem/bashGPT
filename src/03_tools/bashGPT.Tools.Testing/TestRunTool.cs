using System.Diagnostics;
using System.Text;
using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Testing;

public sealed class TestRunTool : ITool
{
    private static readonly IReadOnlyDictionary<string, (string Executable, Func<TestRunInput, string> Args, string MissingDependencyHelp)> DefaultRunners =
        new Dictionary<string, (string, Func<TestRunInput, string>, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["dotnet"] = ("dotnet", i =>
            {
                var args = "test";
                if (!string.IsNullOrWhiteSpace(i.Project)) args += $" \"{i.Project}\"";
                if (!string.IsNullOrWhiteSpace(i.Filter))  args += $" --filter \"{i.Filter}\"";
                args += " --logger \"console;verbosity=normal\"";
                return args;
            }, "Install the .NET SDK and verify 'dotnet --info' works in your shell."),
            ["npm"] = ("npm", i =>
            {
                var args = "test";
                if (!string.IsNullOrWhiteSpace(i.Project)) args += $" -- {i.Project}";
                return args;
            }, "Install Node.js including npm and verify 'npm --version' works in your shell."),
            ["pytest"] = ("pytest", i =>
            {
                var args = string.IsNullOrWhiteSpace(i.Project) ? "." : i.Project;
                if (!string.IsNullOrWhiteSpace(i.Filter)) args += $" -k \"{i.Filter}\"";
                return args;
            }, "Install Python plus pytest and verify 'pytest --version' works in your shell."),
        };

    private readonly IReadOnlyDictionary<string, (string Executable, Func<TestRunInput, string> Args, string MissingDependencyHelp)> _runners;
    private readonly Func<TestRunInput, CancellationToken, Task<(string Output, long DurationMs, bool TimedOut)>>? _runOverride;

    public TestRunTool(
        Func<TestRunInput, CancellationToken, Task<(string, long, bool)>>? runOverride = null,
        IReadOnlyDictionary<string, (string Executable, Func<TestRunInput, string> Args, string MissingDependencyHelp)>? runners = null)
    {
        _runOverride = runOverride;
        _runners = runners ?? DefaultRunners;
    }

    public ToolDefinition Definition { get; } = new(
        Name: "test_run",
        Description: "Runs tests using a configured runner (dotnet, npm, pytest) and returns structured results.",
        Parameters:
        [
            new ToolParameter("runner", "string", "Test runner: 'dotnet', 'npm', 'pytest'.", Required: true),
            new ToolParameter("project", "string", "Project path or test filter target.", Required: false),
            new ToolParameter("filter", "string", "Test filter expression.", Required: false),
            new ToolParameter("cwd", "string", "Working directory.", Required: false),
            new ToolParameter("timeoutMs", "integer", "Timeout in ms. Default: 120000.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        TestRunInput input;
        try
        {
            input = ParseInput(call.ArgumentsJson);
        }
        catch (JsonException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }

        if (call.WorkingDirectory is not null)
            input = input with { Cwd = input.Cwd ?? call.WorkingDirectory };

        if (!_runners.ContainsKey(input.Runner))
            return new ToolResult(Success: false, Content: $"Unknown runner '{input.Runner}'. Supported: {string.Join(", ", _runners.Keys)}");

        string rawOutput;
        long durationMs;
        bool timedOut;

        if (_runOverride is not null)
        {
            (rawOutput, durationMs, timedOut) = await _runOverride(input, ct);
        }
        else
        {
            try
            {
                (rawOutput, durationMs, timedOut) = await RunProcessAsync(input, ct);
            }
            catch (MissingExecutableException ex)
            {
                return new ToolResult(Success: false, Content: ex.Message);
            }
        }

        var parser = GetParser(input.Runner);
        var output = parser.Parse(rawOutput, durationMs, timedOut);

        return new ToolResult(
            Success: output.Success,
            Content: JsonSerializer.Serialize(output, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private async Task<(string Output, long DurationMs, bool TimedOut)> RunProcessAsync(
        TestRunInput input, CancellationToken externalCt)
    {
        var (executable, argsFactory, missingDependencyHelp) = _runners[input.Runner];
        var args = argsFactory(input);

        using var timeoutCts = new CancellationTokenSource(input.TimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt, timeoutCts.Token);

        var psi = new ProcessStartInfo(executable, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (!string.IsNullOrWhiteSpace(input.Cwd))
            psi.WorkingDirectory = input.Cwd;

        var sb = new StringBuilder();
        var sw = Stopwatch.StartNew();
        bool timedOut = false;

        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw ExternalDependencyErrors.TryCreateMissingExecutableException(executable, missingDependencyHelp, ex) ?? ex;
        }

        var outTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
        var errTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = timeoutCts.IsCancellationRequested;
            if (!timedOut && externalCt.IsCancellationRequested)
                throw;
            try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }
        }

        sb.Append(await outTask.ConfigureAwait(false));
        sb.Append(await errTask.ConfigureAwait(false));
        sw.Stop();

        return (sb.ToString(), sw.ElapsedMilliseconds, timedOut);
    }

    private static ITestOutputParser GetParser(string runner) => runner.ToLowerInvariant() switch
    {
        "dotnet" => new DotnetTestOutputParser(),
        _        => new RawOutputParser(),
    };

    private static TestRunInput ParseInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("runner", out var runnerEl))
            throw new ArgumentException("missing_required_field: 'runner' is required. Supported: dotnet, npm, pytest.");
        if (runnerEl.ValueKind != JsonValueKind.String)
            throw new ArgumentException("invalid_type: 'runner' must be a string.");
        var runner = runnerEl.GetString();
        if (string.IsNullOrWhiteSpace(runner))
            throw new ArgumentException("invalid_value: 'runner' must not be empty.");

        var project = ReadOptionalString(root, "project");
        var filter  = ReadOptionalString(root, "filter");
        var cwd     = ReadOptionalString(root, "cwd");
        int timeout = ReadOptionalInt(root, "timeoutMs") ?? 120_000;
        if (timeout <= 0)
            throw new ArgumentException("invalid_value: 'timeoutMs' must be greater than 0.");

        return new TestRunInput(runner, project, filter, cwd, timeout);
    }

    private static string? ReadOptionalString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var valueEl)) return null;
        return valueEl.ValueKind switch
        {
            JsonValueKind.String => valueEl.GetString(),
            JsonValueKind.Null => null,
            _ => throw new ArgumentException($"invalid_type: '{name}' must be a string."),
        };
    }

    private static int? ReadOptionalInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var valueEl)) return null;
        return valueEl.ValueKind switch
        {
            JsonValueKind.Number when valueEl.TryGetInt32(out var i) => i,
            JsonValueKind.Null => null,
            _ => throw new ArgumentException($"invalid_type: '{name}' must be an integer."),
        };
    }
}
