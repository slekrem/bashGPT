using System.Runtime.InteropServices;
using bashGPT.Core.Shell;

namespace bashGPT.Tools.Shell.Tests;

public class CommandExecutorTests
{
    private static (CommandExecutor Executor, StringWriter Out) CreateExecutor()
    {
        var output = new StringWriter();
        return (new CommandExecutor(output), output);
    }

    [Fact]
    public async Task ProcessAsync_ExecutesWithoutPrompt()
    {
        var (exec, outWriter) = CreateExecutor();
        var cmds = new[] { new ExtractedCommand("echo autoexec", false, null) };

        var results = await exec.ProcessAsync(cmds);

        Assert.Single(results);
        Assert.True(results[0].WasExecuted);
        Assert.Contains("autoexec", results[0].Output);
        Assert.DoesNotContain("[j/N]", outWriter.ToString());
    }

    [Fact]
    public async Task ProcessAsync_DangerousCommand_ShowsWarning()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var tempFile = Path.GetTempFileName();
        try
        {
            var (exec, outWriter) = CreateExecutor();
            var cmds = new[] { new ExtractedCommand($"chmod 777 \"{tempFile}\"", true, "chmod 777 / a+x (unsichere Berechtigungen)") };

            await exec.ProcessAsync(cmds);

            var text = outWriter.ToString();
            Assert.Contains("GEFaeHRLICHER", text);
            Assert.Contains("chmod 777", text);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ProcessAsync_StripsAnsiEscapeCodes()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var (exec, _) = CreateExecutor();
        var cmds = new[] { new ExtractedCommand(
            @"awk 'BEGIN{printf ""%c[31mhello%c[0m\n"", 27, 27}'",
            false, null) };

        var results = await exec.ProcessAsync(cmds);

        Assert.Single(results);
        Assert.True(results[0].WasExecuted);
        Assert.Contains("hello", results[0].Output);
        Assert.DoesNotContain("\x1b[", results[0].Output);
    }

    [Fact]
    public async Task ProcessAsync_TimesOutLongRunningCommand()
    {
        var output = new StringWriter();
        var exec = new CommandExecutor(
            output,
            commandTimeoutSeconds: 1);

        var sleepCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Environment.GetEnvironmentVariable("SHELL") is not null
                ? "sleep 5"
                : "ping -n 10 127.0.0.1 > nul"
            : "sleep 2";
        var cmds = new[] { new ExtractedCommand(sleepCmd, false, null) };
        var results = await exec.ProcessAsync(cmds);

        Assert.Single(results);
        Assert.True(results[0].WasExecuted);
        Assert.Equal(-1, results[0].ExitCode);
        Assert.Contains("abgebrochen", results[0].Output);
    }

    [Fact]
    public async Task ProcessAsync_SkipsInteractiveTopCommand()
    {
        var (exec, _) = CreateExecutor();
        var cmds = new[] { new ExtractedCommand("top", false, null) };

        var results = await exec.ProcessAsync(cmds);

        Assert.Single(results);
        Assert.False(results[0].WasExecuted);
        Assert.Equal(-1, results[0].ExitCode);
        Assert.Contains("Interaktiver 'top'-Aufruf", results[0].Output);
    }

    [Fact]
    public async Task ProcessAsync_AllowsTopOneShotMode()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var (exec, _) = CreateExecutor();
        var cmds = new[] { new ExtractedCommand("top -l 1 | head -n 5", false, null) };

        var results = await exec.ProcessAsync(cmds);

        Assert.Single(results);
        Assert.True(results[0].WasExecuted);
    }

    [Fact]
    public async Task ProcessAsync_EmptyList_ReturnsEmpty()
    {
        var (exec, _) = CreateExecutor();
        var results = await exec.ProcessAsync([]);
        Assert.Empty(results);
    }

    [Fact]
    public void BuildFollowUpContext_EmptyResults_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, CommandExecutor.BuildFollowUpContext([]));
    }

    [Fact]
    public void BuildFollowUpContext_IncludesCommandAndOutput()
    {
        var results = new[]
        {
            new CommandResult("ls -la", 0, "total 42\ndrwxr-xr-x ...", WasExecuted: true)
        };

        var ctx = CommandExecutor.BuildFollowUpContext(results);
        Assert.Contains("ls -la", ctx);
        Assert.Contains("total 42", ctx);
        Assert.Contains("Exit-Code: 0", ctx);
    }

    [Fact]
    public void BuildFollowUpContext_NotExecuted_ShowsSkippedMarker()
    {
        var results = new[] { new CommandResult("top", -1, "", WasExecuted: false) };
        var ctx = CommandExecutor.BuildFollowUpContext(results);
        Assert.Contains("nicht ausgefuehrt", ctx);
    }
}
