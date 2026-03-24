using bashGPT.Agents;

namespace bashGPT.Server.Agents;

/// <summary>
/// Default agent used for general-purpose chat without agent-specific context.
/// This is a built-in server default, not part of the public agent SDK surface.
/// </summary>
internal sealed class GenericAgent : AgentBase
{
    public override string Id => "generic";

    public override string Name => "Generic Chat";

    public override IReadOnlyList<string> EnabledTools => [];

    public override AgentLlmConfig LlmConfig => new(
        Temperature: 0.2,
        TopP:        0.9,
        Stream:      true
    );

    public override IReadOnlyList<string> SystemPrompt =>
        ["You are a helpful assistant. Answer clearly and concisely."];

    protected override string GetAgentMarkdown(string? sessionPath = null) => """
        # Generic Chat

        Default mode without specialized tools or an agent-specific system prompt.

        ## Notes

        - No agent-specific system prompt is active
        - Tools can be enabled manually through the tool picker
        - Choose a specialized agent from the agents view for focused workflows
        """;
}
