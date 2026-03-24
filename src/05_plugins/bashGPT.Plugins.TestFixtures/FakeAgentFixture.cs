using bashGPT.Agents;

namespace bashGPT.Plugins.TestFixtures;

/// <summary>
/// Minimal <see cref="AgentBase"/> subclass used only by plugin loader tests.
/// </summary>
public sealed class FakeAgentFixture : AgentBase
{
    public override string Id => "fake-agent";
    public override string Name => "Fake Agent";
    public override IReadOnlyList<string> EnabledTools => [];
    public override IReadOnlyList<string> SystemPrompt => ["You are a fake agent."];
    protected override string GetAgentMarkdown(string? sessionPath = null) => "# Fake Agent\n\nUsed in plugin loader tests.";
}
