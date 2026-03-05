using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BashGPT.Tools.Abstractions;

namespace BashGPT.Tools.Shell;

public sealed class ShellExecTool : ITool
{
    private const int MaxOutputChars = 32_768;

    private readonly IShellExecPolicy _policy;
    private readonly Action<ShellExecInput, ShellExecOutput>? _onExecuted;

    public ShellExecTool(IShellExecPolicy? policy = null, Action<ShellExecInput, ShellExecOutput>? onExecuted = null)
    {
        _policy = policy ?? new DefaultShellExecPolicy();
        _onExecuted = onExecuted;
    }

    public ToolDefinition Definition { get; } = new(
        Name: "shell_exec",
        Description: "Executes a shell command and returns stdout, stderr, exit code, duration and timeout status.",
        Parameters:
        [
            new ToolParameter("command", "string", "The shell command to execute.", Required: true),
            new ToolParameter("cwd", "string", "Working directory for the command.", Required: false),
            new ToolParameter("timeoutMs", "integer", "Timeout in milliseconds (default: 5000).", Required: false),
            new ToolParameter("env", "object", "Additional environment variables (key-value pairs).", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        ShellExecInput input;
        try
        {
            input = ParseInput(call.ArgumentsJson);
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }

        if (!_policy.Allow(input))
            return new ToolResult(Success: false, Content: "Command blocked by policy.");

        var output = await RunAsync(input, ct);
        _onExecuted?.Invoke(input, output);

        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new ToolResult(Success: !output.TimedOut && output.ExitCode == 0, Content: json);
    }

    private static ShellExecInput ParseInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var command = root.GetProperty("command").GetString()
            ?? throw new ArgumentException("command must not be null");

        string? cwd = root.TryGetProperty("cwd", out var cwdEl) ? cwdEl.GetString() : null;
        int timeoutMs = root.TryGetProperty("timeoutMs", out var toEl) ? toEl.GetInt32() : 5000;

        Dictionary<string, string>? env = null;
        if (root.TryGetProperty("env", out var envEl) && envEl.ValueKind == JsonValueKind.Object)
        {
            env = [];
            foreach (var prop in envEl.EnumerateObject())
                env[prop.Name] = prop.Value.GetString() ?? string.Empty;
        }

        return new ShellExecInput(command, cwd, timeoutMs, env);
    }

    private static async Task<ShellExecOutput> RunAsync(ShellExecInput input, CancellationToken externalCt)
    {
        using var timeoutCts = new CancellationTokenSource(input.TimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt, timeoutCts.Token);

        var psi = new ProcessStartInfo("bash", ["-c", input.Command])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (input.Cwd is not null)
            psi.WorkingDirectory = input.Cwd;

        if (input.Env is not null)
        {
            foreach (var (key, value) in input.Env)
                psi.Environment[key] = value;
        }

        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();
        var sw = Stopwatch.StartNew();
        bool timedOut = false;

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = ReadLimitedAsync(process.StandardOutput, stdoutBuilder, MaxOutputChars, linkedCts.Token);
        var stderrTask = ReadLimitedAsync(process.StandardError, stderrBuilder, MaxOutputChars, linkedCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = timeoutCts.IsCancellationRequested;
            try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }
        }

        await Task.WhenAll(stdoutTask, stderrTask);
        sw.Stop();

        int exitCode = timedOut ? -1 : process.ExitCode;

        return new ShellExecOutput(
            Stdout: stdoutBuilder.ToString(),
            Stderr: stderrBuilder.ToString(),
            ExitCode: exitCode,
            DurationMs: sw.ElapsedMilliseconds,
            TimedOut: timedOut);
    }

    private static async Task ReadLimitedAsync(StreamReader reader, StringBuilder target, int maxChars, CancellationToken ct)
    {
        var buffer = new char[4096];
        try
        {
            int read;
            while ((read = await reader.ReadAsync(buffer, ct)) > 0)
            {
                var remaining = maxChars - target.Length;
                if (remaining <= 0) break;
                target.Append(buffer, 0, Math.Min(read, remaining));
            }
        }
        catch (OperationCanceledException) { /* expected on timeout */ }
    }
}
