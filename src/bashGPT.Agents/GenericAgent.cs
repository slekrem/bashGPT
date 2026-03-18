namespace BashGPT.Agents;

/// <summary>
/// Default agent for general chat without agent-specific context.
/// Displayed in the info panel when no specialized agent is selected.
/// </summary>
public sealed class GenericAgent : AgentBase
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

    protected override string GetAgentMarkdown() => """
        # Generic Chat

        Default mode without specialized tools or an agent-specific system prompt.

        ## Notes

        - No agent-specific system prompt is active
        - Tools can be enabled manually through the tool picker
        - Choose a specialized agent from the agents view for focused workflows
        """;
}
