using BashGPT.Shell;

namespace BashGPT.Core.Tests.Shell;

public class ExecModeConverterTests
{
    // ── ToString ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ExecutionMode.Ask,      "ask")]
    [InlineData(ExecutionMode.AutoExec, "auto-exec")]
    [InlineData(ExecutionMode.DryRun,   "dry-run")]
    [InlineData(ExecutionMode.NoExec,   "no-exec")]
    public void ToString_ReturnsExpectedString(ExecutionMode mode, string expected)
        => Assert.Equal(expected, ExecModeConverter.ToString(mode));

    // ── Parse ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("ask",       ExecutionMode.Ask)]
    [InlineData("auto-exec", ExecutionMode.AutoExec)]
    [InlineData("dry-run",   ExecutionMode.DryRun)]
    [InlineData("no-exec",   ExecutionMode.NoExec)]
    [InlineData("AUTO-EXEC", ExecutionMode.AutoExec)] // case-insensitive
    public void Parse_ReturnsExpectedMode(string input, ExecutionMode expected)
        => Assert.Equal(expected, ExecModeConverter.Parse(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void Parse_ReturnsNull_ForInvalidInput(string? input)
        => Assert.Null(ExecModeConverter.Parse(input));

    // ── Round-trip ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ExecutionMode.Ask)]
    [InlineData(ExecutionMode.AutoExec)]
    [InlineData(ExecutionMode.DryRun)]
    [InlineData(ExecutionMode.NoExec)]
    public void RoundTrip_ToStringThenParse_PreservesMode(ExecutionMode mode)
        => Assert.Equal(mode, ExecModeConverter.Parse(ExecModeConverter.ToString(mode)));
}
