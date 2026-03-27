using System.Text.Json;
using bashGPT.Tools.Abstractions;
using bashGPT.Tools.GitHub;
using bashGPT.Tools.GitHub.Comments;
using bashGPT.Tools.GitHub.Issues;
using bashGPT.Tools.GitHub.PullRequests;

namespace bashGPT.Tools.GitHub.Tests;

public class GhToolTests
{
    // ── GhIssueListTool ───────────────────────────────────────────

    [Fact]
    public async Task GhIssueList_DefaultPolicy_AllowsRead()
    {
        var tool = new GhIssueListTool(new DefaultGhPolicy());
        // Should not be blocked by policy (will fail if gh is not installed, but not due to policy)
        var result = await tool.ExecuteAsync(Call("gh_issue_list", new { }), CancellationToken.None);
        Assert.False(result.Content.Contains("blocked by policy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GhIssueList_InvalidStateValue_ReturnsStructuredValidationError()
    {
        var tool = new GhIssueListTool();
        var result = await tool.ExecuteAsync(Call("gh_issue_list", new { state = "invalid" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_value", result.Content, StringComparison.Ordinal);
        Assert.Contains("'state'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GhIssueList_InvalidLimitType_ReturnsStructuredValidationError()
    {
        var tool = new GhIssueListTool();
        var result = await tool.ExecuteAsync(new ToolCall("gh_issue_list", """{"limit":"ten"}"""), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_type", result.Content, StringComparison.Ordinal);
        Assert.Contains("'limit'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GhIssueList_NonPositiveLimit_ReturnsStructuredValidationError()
    {
        var tool = new GhIssueListTool();
        var result = await tool.ExecuteAsync(Call("gh_issue_list", new { limit = 0 }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_value", result.Content, StringComparison.Ordinal);
        Assert.Contains("'limit'", result.Content, StringComparison.Ordinal);
    }

    // ── GhIssueViewTool ───────────────────────────────────────────

    [Fact]
    public async Task GhIssueView_MissingNumber_ReturnsStructuredValidationError()
    {
        var tool = new GhIssueViewTool();
        var result = await tool.ExecuteAsync(Call("gh_issue_view", new { }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'number'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GhIssueView_NegativeNumber_ReturnsStructuredValidationError()
    {
        var tool = new GhIssueViewTool();
        var result = await tool.ExecuteAsync(Call("gh_issue_view", new { number = -1 }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'number'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GhIssueView_DefaultPolicy_AllowsRead()
    {
        var tool = new GhIssueViewTool(new DefaultGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_issue_view", new { number = 1 }), CancellationToken.None);
        Assert.False(result.Content.Contains("blocked by policy", StringComparison.OrdinalIgnoreCase));
    }

    // ── GhIssueCreateTool ─────────────────────────────────────────

    [Fact]
    public async Task GhIssueCreate_DefaultPolicy_BlocksWrite()
    {
        var tool = new GhIssueCreateTool(new DefaultGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_issue_create", new { title = "test" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhIssueCreate_MissingTitle_ReturnsStructuredValidationError()
    {
        var tool = new GhIssueCreateTool(new PermissiveGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_issue_create", new { }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'title'", result.Content, StringComparison.Ordinal);
    }

    // ── GhPrListTool ──────────────────────────────────────────────

    [Fact]
    public async Task GhPrList_DefaultPolicy_AllowsRead()
    {
        var tool = new GhPrListTool(new DefaultGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_list", new { }), CancellationToken.None);
        Assert.False(result.Content.Contains("blocked by policy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GhPrList_InvalidStateValue_ReturnsStructuredValidationError()
    {
        var tool = new GhPrListTool();
        var result = await tool.ExecuteAsync(Call("gh_pr_list", new { state = "invalid" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_value", result.Content, StringComparison.Ordinal);
        Assert.Contains("'state'", result.Content, StringComparison.Ordinal);
    }

    // ── GhPrViewTool ──────────────────────────────────────────────

    [Fact]
    public async Task GhPrView_DefaultPolicy_AllowsRead()
    {
        var tool = new GhPrViewTool(new DefaultGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_view", new { }), CancellationToken.None);
        Assert.False(result.Content.Contains("blocked by policy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GhPrView_InvalidNumberValue_ReturnsStructuredValidationError()
    {
        var tool = new GhPrViewTool();
        var result = await tool.ExecuteAsync(Call("gh_pr_view", new { number = -5 }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_value", result.Content, StringComparison.Ordinal);
        Assert.Contains("'number'", result.Content, StringComparison.Ordinal);
    }

    // ── GhPrCreateTool ────────────────────────────────────────────

    [Fact]
    public async Task GhPrCreate_DefaultPolicy_BlocksWrite()
    {
        var tool = new GhPrCreateTool(new DefaultGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_create", new { title = "feat: test" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhPrCreate_MissingTitle_ReturnsStructuredValidationError()
    {
        var tool = new GhPrCreateTool(new PermissiveGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_create", new { }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'title'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GhPrCreate_InvalidJson_ReturnsInvalidJsonError()
    {
        var tool = new GhPrCreateTool(new PermissiveGhPolicy());
        var result = await tool.ExecuteAsync(new ToolCall("gh_pr_create", "not json"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_json", result.Content, StringComparison.Ordinal);
    }

    // ── GhPrDiffTool ──────────────────────────────────────────────

    [Fact]
    public async Task GhPrDiff_DefaultPolicy_AllowsRead()
    {
        var tool = new GhPrDiffTool(new DefaultGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_diff", new { }), CancellationToken.None);
        Assert.False(result.Content.Contains("blocked by policy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GhPrDiff_InvalidNumberValue_ReturnsStructuredValidationError()
    {
        var tool = new GhPrDiffTool();
        var result = await tool.ExecuteAsync(Call("gh_pr_diff", new { number = 0 }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_value", result.Content, StringComparison.Ordinal);
        Assert.Contains("'number'", result.Content, StringComparison.Ordinal);
    }

    // ── GhPrReviewTool ────────────────────────────────────────────

    [Fact]
    public async Task GhPrReview_DefaultPolicy_BlocksWrite()
    {
        var tool = new GhPrReviewTool(new DefaultGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_review", new { @event = "approve" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhPrReview_MissingEvent_ReturnsStructuredValidationError()
    {
        var tool = new GhPrReviewTool(new PermissiveGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_review", new { }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'event'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GhPrReview_InvalidEventValue_ReturnsStructuredValidationError()
    {
        var tool = new GhPrReviewTool(new PermissiveGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_review", new { @event = "like" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_value", result.Content, StringComparison.Ordinal);
        Assert.Contains("'event'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GhPrReview_RequestChangesWithoutBody_ReturnsStructuredValidationError()
    {
        var tool = new GhPrReviewTool(new PermissiveGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_review", new { @event = "request-changes" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'body'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GhPrReview_CommentWithoutBody_ReturnsStructuredValidationError()
    {
        var tool = new GhPrReviewTool(new PermissiveGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_review", new { @event = "comment" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'body'", result.Content, StringComparison.Ordinal);
    }

    // ── GhPrMergeTool ─────────────────────────────────────────────

    [Fact]
    public async Task GhPrMerge_DefaultPolicy_BlocksWrite()
    {
        var tool = new GhPrMergeTool(new DefaultGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_merge", new { }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhPrMerge_InvalidMethodValue_ReturnsStructuredValidationError()
    {
        var tool = new GhPrMergeTool(new PermissiveGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_merge", new { method = "fast-forward" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_value", result.Content, StringComparison.Ordinal);
        Assert.Contains("'method'", result.Content, StringComparison.Ordinal);
    }

    // ── GhPrChecksTool ────────────────────────────────────────────

    [Fact]
    public async Task GhPrChecks_DefaultPolicy_AllowsRead()
    {
        var tool = new GhPrChecksTool(new DefaultGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_pr_checks", new { }), CancellationToken.None);
        Assert.False(result.Content.Contains("blocked by policy", StringComparison.OrdinalIgnoreCase));
    }

    // ── GhCommentTool ─────────────────────────────────────────────

    [Fact]
    public async Task GhComment_DefaultPolicy_BlocksWrite()
    {
        var tool = new GhCommentTool(new DefaultGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_comment", new { number = 1, body = "hello" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("blocked by policy", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GhComment_MissingNumber_ReturnsStructuredValidationError()
    {
        var tool = new GhCommentTool(new PermissiveGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_comment", new { body = "hello" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'number'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GhComment_MissingBody_ReturnsStructuredValidationError()
    {
        var tool = new GhCommentTool(new PermissiveGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_comment", new { number = 1 }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing_required_field", result.Content, StringComparison.Ordinal);
        Assert.Contains("'body'", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GhComment_InvalidTypeValue_ReturnsStructuredValidationError()
    {
        var tool = new GhCommentTool(new PermissiveGhPolicy());
        var result = await tool.ExecuteAsync(Call("gh_comment", new { number = 1, body = "hello", type = "discussion" }), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("invalid_value", result.Content, StringComparison.Ordinal);
        Assert.Contains("'type'", result.Content, StringComparison.Ordinal);
    }

    // ── GhRunner ──────────────────────────────────────────────────

    [Fact]
    public async Task GhRunner_MissingGhExecutable_ReturnsHelpfulFailure()
    {
        // Temporarily test by calling a non-existent executable path through a tool
        // We test via the runner directly using internal access through a known-bad path.
        // Since GhRunner is internal, we invoke a tool with a guaranteed-missing executable name
        // by verifying the missing_dependency message format the tools would emit.

        // Use the actual GhRunner indirectly: if gh is not found, the error contains missing_dependency.
        // This test documents the contract rather than fully testing the private runner.
        // The pattern mirrors GitRunner test in the Git tests project.
        var (_, stderr, exitCode) = await GhRunner.RunAsync(
            ["--version"],
            null,
            CancellationToken.None,
            executable: "bashgpt-missing-gh");

        Assert.Equal(-1, exitCode);
        Assert.Contains("missing_dependency", stderr, StringComparison.Ordinal);
        Assert.Contains("bashgpt-missing-gh", stderr, StringComparison.Ordinal);
        Assert.Contains("gh auth login", stderr, StringComparison.Ordinal);
    }

    private static ToolCall Call(string name, object args) =>
        new(name, JsonSerializer.Serialize(args));
}
