using System.Diagnostics;
using System.Text;
using System.Text.Json;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Shell;

/// <summary>
/// Abstract base class for shell execution tools.
/// Subclasses provide the shell-specific process arguments via <see cref="GetShellArgs"/>;
/// all parsing, policy, output capture and timeout logic lives here.
/// </summary>
public abstract class ShellExecBase : ITool
{
    private const int MaxOutputChars = 32_768;

    private readonly IShellExecPolicy _policy;
    private readonly Action<ShellExecInput, ShellExecOutput>? _onExecuted;

    protected ShellExecBase(IShellExecPolicy? policy = null, Action<ShellExecInput, ShellExecOutput>? onExecuted = null)
    {
        _policy    = policy ?? new DefaultShellExecPolicy();
        _onExecuted = onExecuted;
    }

    public abstract ToolDefinition Definition { get; }

    /// <summary>Returns the executable and arguments used to run <paramref name="command"/>.</summary>
    protected abstract (string FileName, string[] Arguments) GetShellArgs(string command);

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

        if (call.WorkingDirectory is not null)
            input = input with { Cwd = input.Cwd ?? call.WorkingDirectory };

        if (!_policy.Allow(input))
            return new ToolResult(Success: false, Content: "Command blocked by policy.");

        ShellExecOutput output;
        try
        {
            output = await RunAsync(input, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Execution failed: {ex.Message}");
        }

        _onExecuted?.Invoke(input, output);

        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new ToolResult(Success: !output.TimedOut && output.ExitCode == 0, Content: json);
    }

    private async Task<ShellExecOutput> RunAsync(ShellExecInput input, CancellationToken externalCt)
    {
        using var timeoutCts = new CancellationTokenSource(input.TimeoutMs);
        using var linkedCts  = CancellationTokenSource.CreateLinkedTokenSource(externalCt, timeoutCts.Token);

        var (shellFile, shellArgs) = GetShellArgs(input.Command);
        var psi = new ProcessStartInfo(shellFile, shellArgs)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
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
        var stderrTask = ReadLimitedAsync(process.StandardError,  stderrBuilder, MaxOutputChars, linkedCts.Token);

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

        return new ShellExecOutput(
            Stdout:     stdoutBuilder.ToString(),
            Stderr:     stderrBuilder.ToString(),
            ExitCode:   timedOut ? -1 : process.ExitCode,
            DurationMs: sw.ElapsedMilliseconds,
            TimedOut:   timedOut);
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

    protected static ShellExecInput ParseInput(string json)
    {
        using var doc  = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("command", out var commandEl))
            throw new ArgumentException("missing_required_field: 'command' is required.");
        if (commandEl.ValueKind != JsonValueKind.String)
            throw new ArgumentException("invalid_type: 'command' must be a string.");
        var command = commandEl.GetString();
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("invalid_value: 'command' must not be empty.");

        var cwd       = ReadOptionalString(root, "cwd");
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
            JsonValueKind.Null   => null,
            _                    => throw new ArgumentException($"invalid_type: '{name}' must be a string."),
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
