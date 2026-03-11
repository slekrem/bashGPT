using System.Text.Json;
using BashGPT.Tools.Abstractions;
using BashGPT.Tools.Testing;

namespace bashGPT.Tools.Testing.Tests;

public class TestRunToolTests
{
    private static ToolCall Call(object args) =>
        new("test_run", JsonSerializer.Serialize(args));

    private static TestRunTool ToolWith(string output, long durationMs = 100, bool timedOut = false) =>
        new(runOverride: (_, _) => Task.FromResult((output, durationMs, timedOut)));

    // ── Parser: DotnetTestOutputParser ────────────────────────────

    [Fact]
    public void DotnetParser_AllPassed_ReturnsCorrectCounts()
    {
        var parser = new DotnetTestOutputParser();
        var output = "Passed!  - Failed:     0, Passed:     9, Skipped:     0, Total:     9, Duration: 127 ms";

        var result = parser.Parse(output, 127, false);

        Assert.True(result.Success);
        Assert.Equal(9, result.Passed);
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.Skipped);
    }

    [Fact]
    public void DotnetParser_WithFailures_ReturnsCorrectCounts()
    {
        var parser = new DotnetTestOutputParser();
        var output = """
            Failed SomeTest.MethodName [12 ms]
              Error message here
            Passed!  - Failed:     1, Passed:     8, Skipped:     0, Total:     9, Duration: 200 ms
            """;

        var result = parser.Parse(output, 200, false);

        Assert.False(result.Success);
        Assert.Equal(8, result.Passed);
        Assert.Equal(1, result.Failed);
        Assert.Single(result.Failures);
        Assert.Equal("SomeTest.MethodName", result.Failures[0].Name);
    }

    [Fact]
    public void DotnetParser_TimedOut_SetsTimedOutTrue()
    {
        var parser = new DotnetTestOutputParser();
        var result = parser.Parse("partial output...", 5000, timedOut: true);

        Assert.False(result.Success);
        Assert.True(result.TimedOut);
    }

    [Fact]
    public void DotnetParser_EmptyOutput_ReturnsZeroCounts()
    {
        var parser = new DotnetTestOutputParser();
        var result = parser.Parse(string.Empty, 0, false);

        Assert.True(result.Success); // no failures = success
        Assert.Equal(0, result.Passed);
        Assert.Equal(0, result.Failed);
    }

    // ── TestRunTool ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_UnknownRunner_ReturnsFailure()
    {
        var tool = new TestRunTool();
        var result = await tool.ExecuteAsync(Call(new { runner = "gradle" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Unknown runner", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ReturnsFailure()
    {
        var tool = new TestRunTool();
        var result = await tool.ExecuteAsync(new ToolCall("test_run", "{bad}"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid arguments", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_MissingRunner_ReturnsStructuredValidationError()
    {
        var tool = new TestRunTool();
        var result = await tool.ExecuteAsync(new ToolCall("test_run", """{"project":"x"}"""), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'runner'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTimeoutType_ReturnsStructuredValidationError()
    {
        var tool = new TestRunTool();
        var result = await tool.ExecuteAsync(
            new ToolCall("test_run", """{"runner":"dotnet","timeoutMs":"fast"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_type", result.Content, StringComparison.Ordinal);
        Assert.Contains("'timeoutMs'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_DotnetAllPassed_ReturnsSuccess()
    {
        var fakeOutput = "Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 50 ms";
        var tool = ToolWith(fakeOutput, 50);

        var result = await tool.ExecuteAsync(Call(new { runner = "dotnet", project = "MyTests.csproj" }), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(5, output.GetProperty("passed").GetInt32());
        Assert.Equal(0, output.GetProperty("failed").GetInt32());
    }

    [Fact]
    public async Task ExecuteAsync_DotnetWithFailures_ReturnsFailure()
    {
        var fakeOutput = """
            Failed SomeTest.FailingTest [8 ms]
              Assert.Equal() Failure
            Passed!  - Failed:     1, Passed:     4, Skipped:     0, Total:     5, Duration: 60 ms
            """;
        var tool = ToolWith(fakeOutput, 60);

        var result = await tool.ExecuteAsync(Call(new { runner = "dotnet" }), CancellationToken.None);

        Assert.False(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(1, output.GetProperty("failed").GetInt32());
        Assert.Equal(1, output.GetProperty("failures").GetArrayLength());
    }

    [Fact]
    public async Task ExecuteAsync_TimedOut_ReturnsFailureWithTimedOut()
    {
        var tool = ToolWith("partial...", 120_000, timedOut: true);

        var result = await tool.ExecuteAsync(Call(new { runner = "dotnet" }), CancellationToken.None);

        Assert.False(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.True(output.GetProperty("timedOut").GetBoolean());
    }

    [Fact]
    public async Task ExecuteAsync_RawOutputTruncated_WhenTooLong()
    {
        var longOutput = new string('x', 20_000);
        var tool = ToolWith(longOutput);

        var result = await tool.ExecuteAsync(Call(new { runner = "dotnet" }), CancellationToken.None);

        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Contains("[truncated]", output.GetProperty("rawOutput").GetString());
    }
}
