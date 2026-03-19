using bashGPT.Core.Models.Providers;

namespace BashGPT.Core.Tests.Providers;

public sealed class ToolCallArgumentsTests
{
    [Fact]
    public void TryGetString_WithValidStringArgument_ReturnsValue()
    {
        var call = new ToolCall("call-1", "bash", """{"command":"pwd"}""");

        var ok = ToolCallArguments.TryGetString(call, "command", out var value, out var error);

        Assert.True(ok);
        Assert.Equal("pwd", value);
        Assert.Null(error);
    }

    [Fact]
    public void TryGetString_WithMissingField_ReturnsFalse()
    {
        var call = new ToolCall("call-1", "bash", """{"cwd":"/tmp"}""");

        var ok = ToolCallArguments.TryGetString(call, "command", out var value, out var error);

        Assert.False(ok);
        Assert.Equal(string.Empty, value);
        Assert.Contains("'command' field", error);
    }

    [Fact]
    public void TryGetString_WithInvalidJson_ReturnsFalse()
    {
        var call = new ToolCall("call-1", "bash", "{");

        var ok = ToolCallArguments.TryGetString(call, "command", out var value, out var error);

        Assert.False(ok);
        Assert.Equal(string.Empty, value);
        Assert.Contains("Invalid JSON", error);
    }
}
