using bashGPT.Core.Models.Providers;
using bashGPT.Core.Models.Storage;
using bashGPT.Server.Services;

namespace bashGPT.Server.Tests;

/// <summary>
/// Unit tests for <see cref="SessionMessageMapper"/>.
/// </summary>
public sealed class SessionMessageMapperTests
{
    // ── ToChatMessage ────────────────────────────────────────────────────────

    [Fact]
    public void ToChatMessage_UserRole_ReturnsChatMessageWithUserRole()
    {
        var m = new SessionMessage { Role = "user", Content = "Hello" };

        var result = SessionMessageMapper.ToChatMessage(m);

        Assert.NotNull(result);
        Assert.Equal(ChatRole.User, result!.Role);
        Assert.Equal("Hello", result.Content);
    }

    [Fact]
    public void ToChatMessage_AssistantRole_ReturnsAssistantMessage()
    {
        var m = new SessionMessage { Role = "assistant", Content = "Hi there" };

        var result = SessionMessageMapper.ToChatMessage(m);

        Assert.NotNull(result);
        Assert.Equal(ChatRole.Assistant, result!.Role);
        Assert.Equal("Hi there", result.Content);
        Assert.Null(result.ToolCalls);
    }

    [Fact]
    public void ToChatMessage_AssistantWithToolCalls_ReturnsAssistantWithToolCalls()
    {
        var m = new SessionMessage
        {
            Role = "assistant",
            Content = "Calling tool",
            ToolCalls =
            [
                new SessionToolCall { Id = "c1", Name = "my_tool", ArgumentsJson = "{}" },
            ],
        };

        var result = SessionMessageMapper.ToChatMessage(m);

        Assert.NotNull(result);
        Assert.Equal(ChatRole.Assistant, result!.Role);
        Assert.NotNull(result.ToolCalls);
        Assert.Single(result.ToolCalls!);
        Assert.Equal("c1", result.ToolCalls![0].Id);
        Assert.Equal("my_tool", result.ToolCalls![0].Name);
    }

    [Fact]
    public void ToChatMessage_ToolRole_ReturnsToolResultMessage()
    {
        var m = new SessionMessage
        {
            Role = "tool",
            Content = "tool output",
            ToolCallId = "c1",
            ToolName = "my_tool",
        };

        var result = SessionMessageMapper.ToChatMessage(m);

        Assert.NotNull(result);
        Assert.Equal(ChatRole.Tool, result!.Role);
        Assert.Equal("tool output", result.Content);
        Assert.Equal("c1", result.ToolCallId);
        Assert.Equal("my_tool", result.ToolName);
    }

    [Fact]
    public void ToChatMessage_UnknownRole_ReturnsNull()
    {
        var m = new SessionMessage { Role = "system", Content = "system prompt" };

        var result = SessionMessageMapper.ToChatMessage(m);

        Assert.Null(result);
    }

    // ── FromChatMessage ──────────────────────────────────────────────────────

    [Fact]
    public void FromChatMessage_AssistantRole_ReturnsSessionMessageWithAssistantRole()
    {
        var m = new ChatMessage(ChatRole.Assistant, "Response text");

        var result = SessionMessageMapper.FromChatMessage(m);

        Assert.Equal("assistant", result.Role);
        Assert.Equal("Response text", result.Content);
        Assert.Null(result.ToolCalls);
    }

    [Fact]
    public void FromChatMessage_AssistantWithToolCalls_IncludesToolCalls()
    {
        var m = ChatMessage.AssistantWithToolCalls(
            [new ToolCall("c1", "my_tool", "{\"x\":1}")],
            "Reasoning text");

        var result = SessionMessageMapper.FromChatMessage(m);

        Assert.Equal("assistant", result.Role);
        Assert.NotNull(result.ToolCalls);
        Assert.Single(result.ToolCalls!);
        Assert.Equal("c1", result.ToolCalls![0].Id);
        Assert.Equal("my_tool", result.ToolCalls![0].Name);
        Assert.Equal("{\"x\":1}", result.ToolCalls![0].ArgumentsJson);
    }

    [Fact]
    public void FromChatMessage_ToolRole_SetsToolCallIdAndName()
    {
        var m = ChatMessage.ToolResult("tool output", "c1", "my_tool");

        var result = SessionMessageMapper.FromChatMessage(m);

        Assert.Equal("tool", result.Role);
        Assert.Equal("tool output", result.Content);
        Assert.Equal("c1", result.ToolCallId);
        Assert.Equal("my_tool", result.ToolName);
    }

    [Fact]
    public void FromChatMessage_UserRole_UsesRoleString()
    {
        var m = new ChatMessage(ChatRole.User, "User message");

        var result = SessionMessageMapper.FromChatMessage(m);

        Assert.Equal("user", result.Role);
        Assert.Equal("User message", result.Content);
    }

    // ── round-trip ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("user", "Hello")]
    [InlineData("assistant", "World")]
    public void RoundTrip_BasicRoles_PreservesContent(string role, string content)
    {
        var session = new SessionMessage { Role = role, Content = content };

        var chat = SessionMessageMapper.ToChatMessage(session);
        Assert.NotNull(chat);
        var back = SessionMessageMapper.FromChatMessage(chat!);

        Assert.Equal(role, back.Role);
        Assert.Equal(content, back.Content);
    }
}
