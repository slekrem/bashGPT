using System.Text.Json;
using System.Runtime.InteropServices;
using bashGPT.Tools.Abstractions;
using bashGPT.Tools.Shell;

namespace bashGPT.Tools.Shell.Tests;

public class ShellExecToolTests
{
    private static ToolCall Call(string command, string? cwd = null, int? timeoutMs = null)
    {
        var args = new Dictionary<string, object?> { ["command"] = command };
        if (cwd is not null) args["cwd"] = cwd;
        if (timeoutMs is not null) args["timeoutMs"] = timeoutMs;
        return new ToolCall("shell_exec", JsonSerializer.Serialize(args));
    }

    [Fact]
    public async Task ExecuteAsync_SimpleEcho_ReturnsStdout()
    {
        var tool = new ShellExecTool();
        var result = await tool.ExecuteAsync(Call("echo hello"), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<ShellExecOutput>(result.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal("hello", output.Stdout.TrimEnd('\r', '\n'));
        Assert.Equal(0, output.ExitCode);
        Assert.False(output.TimedOut);
    }

    [Fact]
    public async Task ExecuteAsync_PwdWithCwd_ReturnsCorrectPath()
    {
        // Use a unique subdirectory so the folder name appears in both Windows and Unix-style paths
        var uniqueName = "bashgpt-test-" + Path.GetRandomFileName().Replace(".", "");
        var cwd = Path.Combine(Path.GetTempPath(), uniqueName);
        Directory.CreateDirectory(cwd);
        try
        {
            var tool = new ShellExecTool();
            // Use pwd when bash is the shell (even on Windows via SHELL env), otherwise use cd
            var pwdCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.GetEnvironmentVariable("SHELL") is null
                ? "cd"
                : "pwd";
            var result = await tool.ExecuteAsync(Call(pwdCommand, cwd: cwd), CancellationToken.None);

            Assert.True(result.Success);
            var output = JsonSerializer.Deserialize<ShellExecOutput>(result.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            Assert.Contains(uniqueName, output.Stdout, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(cwd);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NonZeroExit_ReturnsFailureWithExitCode()
    {
        var tool = new ShellExecTool();
        var result = await tool.ExecuteAsync(Call("exit 42"), CancellationToken.None);

        Assert.False(result.Success);
        var output = JsonSerializer.Deserialize<ShellExecOutput>(result.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal(42, output.ExitCode);
        Assert.False(output.TimedOut);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidCommand_ReturnsNonZeroExit()
    {
        var tool = new ShellExecTool();
        var result = await tool.ExecuteAsync(Call("this-command-does-not-exist-xyz"), CancellationToken.None);

        Assert.False(result.Success);
        var output = JsonSerializer.Deserialize<ShellExecOutput>(result.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.NotEqual(0, output.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_SetsTimedOutTrue()
    {
        var tool = new ShellExecTool();
        var sleepCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "powershell -NoProfile -Command \"Start-Sleep -Seconds 60\""
            : "sleep 60";
        var result = await tool.ExecuteAsync(Call(sleepCommand, timeoutMs: 200), CancellationToken.None);

        Assert.False(result.Success);
        var output = JsonSerializer.Deserialize<ShellExecOutput>(result.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.True(output.TimedOut);
        Assert.Equal(-1, output.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_BlockedByPolicy_ReturnsFailure()
    {
        var tool = new ShellExecTool();
        var result = await tool.ExecuteAsync(Call("rm -rf /tmp/test"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_CustomPolicy_AllowsAll()
    {
        var tool = new ShellExecTool(policy: new AlwaysAllowPolicy());
        var result = await tool.ExecuteAsync(Call("echo allowed"), CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_LoggingHook_IsCalled()
    {
        ShellExecInput? capturedInput = null;
        ShellExecOutput? capturedOutput = null;

        var tool = new ShellExecTool(onExecuted: (i, o) => { capturedInput = i; capturedOutput = o; });
        await tool.ExecuteAsync(Call("echo log-test"), CancellationToken.None);

        Assert.NotNull(capturedInput);
        Assert.NotNull(capturedOutput);
        Assert.Equal("echo log-test", capturedInput!.Command);
        Assert.Contains("log-test", capturedOutput!.Stdout);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ReturnsFailure()
    {
        var tool = new ShellExecTool();
        var result = await tool.ExecuteAsync(new ToolCall("shell.exec", "{not-valid-json}"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_MissingCommand_ReturnsStructuredError()
    {
        var tool = new ShellExecTool();
        var result = await tool.ExecuteAsync(new ToolCall("shell_exec", "{}"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_CommandWrongType_ReturnsStructuredError()
    {
        var tool = new ShellExecTool();
        var args = JsonSerializer.Serialize(new Dictionary<string, object?> { ["command"] = 123 });
        var result = await tool.ExecuteAsync(new ToolCall("shell_exec", args), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_type", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutMsInvalidValue_ReturnsStructuredError()
    {
        var tool = new ShellExecTool();
        var args = JsonSerializer.Serialize(new Dictionary<string, object?> { ["command"] = "echo hi", ["timeoutMs"] = 0 });
        var result = await tool.ExecuteAsync(new ToolCall("shell_exec", args), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_value", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_EnvWrongType_ReturnsStructuredError()
    {
        var tool = new ShellExecTool();
        var args = JsonSerializer.Serialize(new Dictionary<string, object?> { ["command"] = "echo hi", ["env"] = "NOPE" });
        var result = await tool.ExecuteAsync(new ToolCall("shell_exec", args), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_type", result.Content, StringComparison.Ordinal);
    }

    private sealed class AlwaysAllowPolicy : IShellExecPolicy
    {
        public bool Allow(ShellExecInput input) => true;
    }
}
