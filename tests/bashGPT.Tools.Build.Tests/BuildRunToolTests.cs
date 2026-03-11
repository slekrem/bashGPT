using System.Text.Json;
using BashGPT.Tools.Abstractions;
using BashGPT.Tools.Build;

namespace bashGPT.Tools.Build.Tests;

public class BuildRunToolTests
{
    private static ToolCall Call(object args) =>
        new("build_run", JsonSerializer.Serialize(args));

    private static BuildRunTool ToolWith(string output, long durationMs = 100, bool timedOut = false, int exitCode = 0) =>
        new(runOverride: (_, _) => Task.FromResult((output, durationMs, timedOut, exitCode)));

    // ── MsbuildDiagnosticParser ───────────────────────────────────

    [Fact]
    public void Parser_ParsesErrorLine()
    {
        var output = "/repo/src/Foo.cs(12,5): error CS1234: Something went wrong [Project.csproj]";
        var (errors, warnings) = MsbuildDiagnosticParser.Parse(output);

        Assert.Single(errors);
        Assert.Empty(warnings);
        var e = errors[0];
        Assert.Equal("error", e.Severity);
        Assert.Equal("CS1234", e.Code);
        Assert.Equal(12, e.Line);
        Assert.Equal(5, e.Column);
        Assert.Contains("Something went wrong", e.Message);
    }

    [Fact]
    public void Parser_ParsesWarningLine()
    {
        var output = "/repo/src/Bar.cs(3,1): warning CS0168: Variable declared but never used [Proj.csproj]";
        var (errors, warnings) = MsbuildDiagnosticParser.Parse(output);

        Assert.Empty(errors);
        Assert.Single(warnings);
        Assert.Equal("CS0168", warnings[0].Code);
    }

    [Fact]
    public void Parser_ParsesMsbuildErrorLine()
    {
        var output = "MSBUILD : error MSB1008: Only one project can be specified.";
        var (errors, _) = MsbuildDiagnosticParser.Parse(output);

        Assert.Single(errors);
        Assert.Equal("MSB1008", errors[0].Code);
        Assert.Equal(string.Empty, errors[0].File);
    }

    [Fact]
    public void Parser_MultipleLines_ReturnsAll()
    {
        var output = """
            /src/A.cs(1,1): error CS0001: First error [Proj.csproj]
            /src/B.cs(2,2): warning CS0002: First warning [Proj.csproj]
            /src/C.cs(3,3): error CS0003: Second error [Proj.csproj]
            """;
        var (errors, warnings) = MsbuildDiagnosticParser.Parse(output);

        Assert.Equal(2, errors.Count);
        Assert.Single(warnings);
    }

    [Fact]
    public void Parser_NoMatches_ReturnsEmpty()
    {
        var (errors, warnings) = MsbuildDiagnosticParser.Parse("Build succeeded.\n0 errors.");

        Assert.Empty(errors);
        Assert.Empty(warnings);
    }

    // ── BuildRunTool ──────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_UnknownRunner_ReturnsFailure()
    {
        var tool = new BuildRunTool();
        var result = await tool.ExecuteAsync(Call(new { runner = "gradle" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Unknown runner", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ReturnsFailure()
    {
        var tool = new BuildRunTool();
        var result = await tool.ExecuteAsync(new ToolCall("build_run", "{bad}"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_MissingRunner_ReturnsStructuredValidationError()
    {
        var tool = new BuildRunTool();
        var result = await tool.ExecuteAsync(new ToolCall("build_run", """{"project":"x"}"""), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'runner'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTimeoutType_ReturnsStructuredValidationError()
    {
        var tool = new BuildRunTool();
        var result = await tool.ExecuteAsync(
            new ToolCall("build_run", """{"runner":"dotnet","timeoutMs":"slow"}"""),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_type", result.Content, StringComparison.Ordinal);
        Assert.Contains("'timeoutMs'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulBuild_ReturnsSuccess()
    {
        var tool = ToolWith("Build succeeded.\n0 Error(s)\n0 Warning(s)", exitCode: 0);

        var result = await tool.ExecuteAsync(Call(new { runner = "dotnet" }), CancellationToken.None);

        Assert.True(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Empty(output.GetProperty("errors").EnumerateArray());
    }

    [Fact]
    public async Task ExecuteAsync_WithErrors_ReturnsFailureAndParsedErrors()
    {
        var fakeOutput = "/src/Foo.cs(5,3): error CS1001: Identifier expected [Proj.csproj]\nBuild FAILED.";
        var tool = ToolWith(fakeOutput, exitCode: 1);

        var result = await tool.ExecuteAsync(Call(new { runner = "dotnet" }), CancellationToken.None);

        Assert.False(result.Success);
        var output = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(1, output.GetProperty("errors").GetArrayLength());
        Assert.Equal("CS1001", output.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_TimedOut_ReturnsFailure()
    {
        var tool = ToolWith("partial...", timedOut: true, exitCode: -1);

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
