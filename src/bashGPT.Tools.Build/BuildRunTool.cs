using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BashGPT.Tools.Abstractions;

namespace BashGPT.Tools.Build;

public sealed class BuildRunTool : ITool
{
    private static readonly IReadOnlyDictionary<string, (string Executable, Func<BuildRunInput, string> Args)> Runners =
        new Dictionary<string, (string, Func<BuildRunInput, string>)>(StringComparer.OrdinalIgnoreCase)
        {
            ["dotnet"] = ("dotnet", i =>
            {
                var args = "build";
                if (!string.IsNullOrWhiteSpace(i.Project))       args += $" \"{i.Project}\"";
                if (!string.IsNullOrWhiteSpace(i.Configuration)) args += $" --configuration {i.Configuration}";
                return args;
            }),
            ["npm"] = ("npm", i =>
            {
                var script = string.IsNullOrWhiteSpace(i.Project) ? "build" : i.Project;
                return $"run {script}";
            }),
        };

    private readonly Func<BuildRunInput, CancellationToken, Task<(string Output, long DurationMs, bool TimedOut, int ExitCode)>>? _runOverride;

    public BuildRunTool(Func<BuildRunInput, CancellationToken, Task<(string, long, bool, int)>>? runOverride = null)
    {
        _runOverride = runOverride;
    }

    public ToolDefinition Definition { get; } = new(
        Name: "build_run",
        Description: "Runs a build command (dotnet, npm) and returns structured diagnostics (errors, warnings).",
        Parameters:
        [
            new ToolParameter("runner", "string", "Build runner: 'dotnet', 'npm'.", Required: true),
            new ToolParameter("project", "string", "Project path or build target.", Required: false),
            new ToolParameter("configuration", "string", "Build configuration, e.g. 'Release'. Default: 'Debug'.", Required: false),
            new ToolParameter("cwd", "string", "Working directory.", Required: false),
            new ToolParameter("timeoutMs", "integer", "Timeout in ms. Default: 120000.", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        BuildRunInput input;
        try
        {
            input = ParseInput(call.ArgumentsJson);
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }

        if (!Runners.ContainsKey(input.Runner))
            return new ToolResult(Success: false, Content: $"Unknown runner '{input.Runner}'. Supported: {string.Join(", ", Runners.Keys)}");

        string rawOutput;
        long durationMs;
        bool timedOut;
        int exitCode;

        if (_runOverride is not null)
        {
            (rawOutput, durationMs, timedOut, exitCode) = await _runOverride(input, ct);
        }
        else
        {
            (rawOutput, durationMs, timedOut, exitCode) = await RunProcessAsync(input, ct);
        }

        var (errors, warnings) = input.Runner.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            ? MsbuildDiagnosticParser.Parse(rawOutput)
            : (new List<BuildDiagnostic>(), new List<BuildDiagnostic>());

        const int maxRaw = 16_384;
        var raw = rawOutput.Length > maxRaw ? rawOutput[..maxRaw] + "\n[truncated]" : rawOutput;

        var output = new BuildRunOutput(
            Success:    !timedOut && exitCode == 0,
            Errors:     errors,
            Warnings:   warnings,
            DurationMs: durationMs,
            TimedOut:   timedOut,
            RawOutput:  raw);

        return new ToolResult(
            Success: output.Success,
            Content: JsonSerializer.Serialize(output, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static async Task<(string Output, long DurationMs, bool TimedOut, int ExitCode)> RunProcessAsync(
        BuildRunInput input, CancellationToken externalCt)
    {
        var (executable, argsFactory) = Runners[input.Runner];
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
        int exitCode = 0;

        using var process = new Process { StartInfo = psi };
        process.Start();

        var outTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
        var errTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
            exitCode = process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            timedOut = timeoutCts.IsCancellationRequested;
            exitCode = -1;
            try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }
        }

        sb.Append(await outTask.ConfigureAwait(false));
        sb.Append(await errTask.ConfigureAwait(false));
        sw.Stop();

        return (sb.ToString(), sw.ElapsedMilliseconds, timedOut, exitCode);
    }

    private static BuildRunInput ParseInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var runner = root.GetProperty("runner").GetString()
            ?? throw new ArgumentException("runner must not be null");
        var project  = root.TryGetProperty("project",       out var p) ? p.GetString() : null;
        var config   = root.TryGetProperty("configuration", out var c) ? c.GetString() : null;
        var cwd      = root.TryGetProperty("cwd",           out var w) ? w.GetString() : null;
        int timeout  = root.TryGetProperty("timeoutMs",     out var t) ? t.GetInt32()  : 120_000;

        return new BuildRunInput(runner, project, config, cwd, timeout);
    }
}
