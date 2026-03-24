using bashGPT.Server.Services;

namespace bashGPT.Server.Tests;

/// <summary>
/// Unit tests for <see cref="RunningChatRegistry"/>.
/// </summary>
public sealed class RunningChatRegistryTests
{
    // ── Register ─────────────────────────────────────────────────────────────

    [Fact]
    public void Register_ValidId_ReturnsTrue()
    {
        var sut = new RunningChatRegistry();
        using var cts = new CancellationTokenSource();

        var result = sut.Register("req-1", cts);

        Assert.True(result);
    }

    [Fact]
    public void Register_DuplicateId_ReturnsFalse()
    {
        var sut = new RunningChatRegistry();
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        sut.Register("req-1", cts1);

        var result = sut.Register("req-1", cts2);

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_BlankId_ReturnsFalse(string? id)
    {
        var sut = new RunningChatRegistry();
        using var cts = new CancellationTokenSource();

        var result = sut.Register(id!, cts);

        Assert.False(result);
    }

    // ── Cancel ───────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_RegisteredId_CancelsToken()
    {
        var sut = new RunningChatRegistry();
        using var cts = new CancellationTokenSource();
        sut.Register("req-1", cts);

        var result = sut.Cancel("req-1");

        Assert.True(result);
        Assert.True(cts.IsCancellationRequested);
    }

    [Fact]
    public void Cancel_UnknownId_ReturnsFalse()
    {
        var sut = new RunningChatRegistry();

        var result = sut.Cancel("nonexistent");

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancel_BlankId_ReturnsFalse(string? id)
    {
        var sut = new RunningChatRegistry();

        var result = sut.Cancel(id!);

        Assert.False(result);
    }

    // ── Unregister ───────────────────────────────────────────────────────────

    [Fact]
    public void Unregister_RegisteredId_AllowsReRegistration()
    {
        var sut = new RunningChatRegistry();
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        sut.Register("req-1", cts1);

        sut.Unregister("req-1");
        var reregistered = sut.Register("req-1", cts2);

        Assert.True(reregistered);
    }

    [Fact]
    public void Unregister_UnknownId_DoesNotThrow()
    {
        var sut = new RunningChatRegistry();

        var ex = Record.Exception(() => sut.Unregister("nonexistent"));

        Assert.Null(ex);
    }

    [Fact]
    public void Unregister_AfterCancel_CancelReturnsFalse()
    {
        var sut = new RunningChatRegistry();
        using var cts = new CancellationTokenSource();
        sut.Register("req-1", cts);
        sut.Unregister("req-1");

        var result = sut.Cancel("req-1");

        Assert.False(result);
    }
}
