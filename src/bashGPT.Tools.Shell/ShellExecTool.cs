using System.Diagnostics;
using System.Runtime.InteropServices;
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

        if (!_policy.Allow(input))
            return new ToolResult(Success: false, Content: "Command blocked by policy.");

        ShellExecOutput output;
        try
        {
            output = await RunAsync(input, ct);
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Execution failed: {ex.Message}");
        }
        _onExecuted?.Invoke(input, output);

        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new ToolResult(Success: !output.TimedOut && output.ExitCode == 0, Content: json);
    }

    private static ShellExecInput ParseInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("command", out var commandEl))
            throw new ArgumentException("missing_required_field: 'command' is required.");
        if (commandEl.ValueKind != JsonValueKind.String)
            throw new ArgumentException("invalid_type: 'command' must be a string.");
        var command = commandEl.GetString();
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("invalid_value: 'command' must not be empty.");

        var cwd = ReadOptionalString(root, "cwd");
        int timeoutMs = ReadOptionalInt(root, "timeoutMs") ?? 5000;
        if (timeoutMs <= 0)
            throw new ArgumentException("invalid_value: 'timeoutMs' must be greater than 0.");

        Dictionary<string, string>? env = null;
        if (root.TryGetProperty("env", out var envEl))
        {
            if (envEl.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null))
                throw new ArgumentException("invalid_type: 'env' must be an object.");

            if (envEl.ValueKind == JsonValueKind.Object)
            {
                env = [];
                foreach (var prop in envEl.EnumerateObject())
                {
                    if (prop.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                        throw new ArgumentException($"invalid_type: env '{prop.Name}' must be a string.");

                    env[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }
        }

        return new ShellExecInput(command, cwd, timeoutMs, env);
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

    private static async Task<ShellExecOutput> RunAsync(ShellExecInput input, CancellationToken externalCt)
    {
        using var timeoutCts = new CancellationTokenSource(input.TimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt, timeoutCts.Token);

        var (shellFile, shellArgs) = GetShellArgs(input.Command);
        var psi = new ProcessStartInfo(shellFile, shellArgs)
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

    private static (string FileName, string[] Arguments) GetShellArgs(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (shell is not null)
                return (shell, ["-c", command]);
            return ("cmd.exe", ["/c", command]);
        }
        var unixShell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";
        return (unixShell, ["-c", command]);
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
