using System.Runtime.InteropServices;
using BashGPT.Shell;

namespace BashGPT.Core.Tests.Shell;

public class CommandExecutorTests
{
    private static (CommandExecutor Executor, StringWriter Out) CreateExecutor(
        ExecutionMode mode,
        string userInput = "")
    {
        var output = new StringWriter();
        var input = new StringReader(userInput);
        return (new CommandExecutor(mode, output, input), output);
    }

    [Fact]
    public async Task ProcessAsync_DryRun_ShowsCommandButDoesNotExecute()
    {
        var (exec, outWriter) = CreateExecutor(ExecutionMode.DryRun);
        var cmds = new[] { new ExtractedCommand("echo hallo", false, null) };

        var results = await exec.ProcessAsync(cmds);

        Assert.Single(results);
        Assert.False(results[0].WasExecuted);
        Assert.Contains("echo hallo", outWriter.ToString());
        Assert.Contains("dry-run", outWriter.ToString());
    }

    [Fact]
    public async Task ProcessAsync_NoExec_ReturnsEmpty()
    {
        var (exec, _) = CreateExecutor(ExecutionMode.NoExec);
        var cmds = new[] { new ExtractedCommand("echo hallo", false, null) };

        var results = await exec.ProcessAsync(cmds);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("n")]
    [InlineData("N")]
    [InlineData("nein")]
    [InlineData("")]
    public async Task ProcessAsync_Ask_UserDeclines_CommandNotExecuted(string answer)
    {
        var (exec, _) = CreateExecutor(ExecutionMode.Ask, answer);
        var cmds = new[] { new ExtractedCommand("echo hallo", false, null) };

        var results = await exec.ProcessAsync(cmds);

        Assert.Single(results);
        Assert.False(results[0].WasExecuted);
    }

    [Theory]
    [InlineData("j")]
    [InlineData("J")]
    [InlineData("ja")]
    [InlineData("y")]
    [InlineData("yes")]
    public async Task ProcessAsync_Ask_UserConfirms_CommandExecuted(string answer)
    {
        var (exec, _) = CreateExecutor(ExecutionMode.Ask, answer);
        var cmds = new[] { new ExtractedCommand("echo hallo", false, null) };

        var results = await exec.ProcessAsync(cmds);

        Assert.Single(results);
        Assert.True(results[0].WasExecuted);
        Assert.Equal(0, results[0].ExitCode);
        Assert.Contains("hallo", results[0].Output);
    }

    [Fact]
    public async Task ProcessAsync_AutoExec_ExecutesWithoutAsking()
    {
        var (exec, outWriter) = CreateExecutor(ExecutionMode.AutoExec);
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
        var (exec, outWriter) = CreateExecutor(ExecutionMode.DryRun);
        var cmds = new[] { new ExtractedCommand("sudo rm -rf /", true, "rm mit -r oder -f") };

        await exec.ProcessAsync(cmds);

        var text = outWriter.ToString();
        Assert.Contains("GEFaeHRLICHER", text);
        Assert.Contains("rm mit -r oder -f", text);
    }

    [Fact]
    public async Task ProcessAsync_StripsAnsiEscapeCodes()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var (exec, _) = CreateExecutor(ExecutionMode.AutoExec);
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
    public async Task ProcessAsync_AutoExec_TimesOutLongRunningCommand()
    {
        var output = new StringWriter();
        var exec = new CommandExecutor(
            ExecutionMode.AutoExec,
            output,
            Console.In,
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
    public async Task ProcessAsync_AutoExec_SkipsInteractiveTopCommand()
    {
        var (exec, _) = CreateExecutor(ExecutionMode.AutoExec);
        var cmds = new[] { new ExtractedCommand("top", false, null) };

        var results = await exec.ProcessAsync(cmds);

        Assert.Single(results);
        Assert.False(results[0].WasExecuted);
        Assert.Equal(-1, results[0].ExitCode);
        Assert.Contains("Interaktiver 'top'-Aufruf", results[0].Output);
    }

    [Fact]
    public async Task ProcessAsync_AutoExec_AllowsTopOneShotMode()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var (exec, _) = CreateExecutor(ExecutionMode.AutoExec);
        var cmds = new[] { new ExtractedCommand("top -l 1 | head -n 5", false, null) };

        var results = await exec.ProcessAsync(cmds);

        Assert.Single(results);
        Assert.True(results[0].WasExecuted);
    }

    [Fact]
    public async Task ProcessAsync_EmptyList_ReturnsEmpty()
    {
        var (exec, _) = CreateExecutor(ExecutionMode.AutoExec);
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
        var results = new[] { new CommandResult("sudo rm -rf /", -1, "", WasExecuted: false) };
        var ctx = CommandExecutor.BuildFollowUpContext(results);
        Assert.Contains("nicht ausgefuehrt", ctx);
    }
}
