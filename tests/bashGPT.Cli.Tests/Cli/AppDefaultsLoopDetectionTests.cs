using BashGPT.Providers;

namespace BashGPT.Tests.Cli;

/// <summary>
/// Unit-Tests für AppDefaults.DetectLoop.
/// </summary>
public sealed class AppDefaultsLoopDetectionTests
{
    private static ToolCall Call(string name, string argsJson, string id = "tc-1") =>
        new(id, name, argsJson);

    private static ToolCall BashCall(string cmd, string id = "tc-1") =>
        new(id, "bash", $$"""{"command":"{{cmd}}"}""");

    [Fact]
    public void DetectLoop_NullPrevious_ReturnsFalse()
    {
        var current = new[] { BashCall("ls") };
        Assert.False(AppDefaults.DetectLoop(null, current));
    }

    [Fact]
    public void DetectLoop_EmptyPrevious_ReturnsFalse()
    {
        var current = new[] { BashCall("ls") };
        Assert.False(AppDefaults.DetectLoop([], current));
    }

    [Fact]
    public void DetectLoop_EmptyCurrent_ReturnsFalse()
    {
        var previous = new[] { BashCall("ls") };
        Assert.False(AppDefaults.DetectLoop(previous, []));
    }

    [Fact]
    public void DetectLoop_DifferentCount_ReturnsFalse()
    {
        var previous = new[] { BashCall("ls", "tc-1") };
        var current  = new[] { BashCall("ls", "tc-2"), BashCall("pwd", "tc-3") };
        Assert.False(AppDefaults.DetectLoop(previous, current));
    }

    [Fact]
    public void DetectLoop_IdenticalSingleCall_DifferentId_ReturnsTrue()
    {
        var previous = new[] { BashCall("ls", "tc-1") };
        var current  = new[] { BashCall("ls", "tc-2") };
        Assert.True(AppDefaults.DetectLoop(previous, current));
    }

    [Fact]
    public void DetectLoop_DifferentCommand_ReturnsFalse()
    {
        var previous = new[] { BashCall("ls",  "tc-1") };
        var current  = new[] { BashCall("pwd", "tc-2") };
        Assert.False(AppDefaults.DetectLoop(previous, current));
    }

    [Fact]
    public void DetectLoop_DifferentToolName_ReturnsFalse()
    {
        var previous = new[] { Call("bash",  """{"command":"ls"}""", "tc-1") };
        var current  = new[] { Call("shell", """{"command":"ls"}""", "tc-2") };
        Assert.False(AppDefaults.DetectLoop(previous, current));
    }

    [Fact]
    public void DetectLoop_IdenticalMultipleCalls_ReturnsTrue()
    {
        var previous = new[] { BashCall("ls", "tc-1"), BashCall("pwd", "tc-2") };
        var current  = new[] { BashCall("ls", "tc-3"), BashCall("pwd", "tc-4") };
        Assert.True(AppDefaults.DetectLoop(previous, current));
    }

    [Fact]
    public void DetectLoop_SameCommandsDifferentOrder_ReturnsFalse()
    {
        var previous = new[] { BashCall("ls", "tc-1"), BashCall("pwd", "tc-2") };
        var current  = new[] { BashCall("pwd", "tc-3"), BashCall("ls", "tc-4") };
        Assert.False(AppDefaults.DetectLoop(previous, current));
    }

    [Fact]
    public void DetectLoop_WhitespaceDifferenceInJson_ReturnsFalse()
    {
        var previous = new[] { Call("bash", """{"command":"ls"}""",  "tc-1") };
        var current  = new[] { Call("bash", """{ "command": "ls" }""", "tc-2") };
        Assert.False(AppDefaults.DetectLoop(previous, current));
    }
}
