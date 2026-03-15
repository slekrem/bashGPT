namespace BashGPT.Agents;

/// <summary>
/// Standard-Agent für den generischen Chat ohne agentenspezifischen Kontext.
/// Wird im Info-Panel angezeigt, wenn kein spezialisierter Agent ausgewählt ist.
/// </summary>
public sealed class GenericAgent : AgentBase
{
    public override string Id => "generic";

    public override string Name => "Generischer Chat";

    public override IReadOnlyList<string> EnabledTools => [];

    public override AgentLlmConfig LlmConfig => new(
        Temperature: 0.2,
        TopP:        0.9,
        Stream:      true
    );

    public override IReadOnlyList<string> SystemPrompt =>
        ["Du bist ein hilfreicher Assistent. Beantworte Fragen klar und praezise."];

    protected override string GetAgentMarkdown() => """
        # Generischer Chat

        Standardmodus ohne spezialisierte Tools oder agentenspezifischen System-Prompt.

        ## Hinweise

        - Kein agentenspezifischer System-Prompt aktiv
        - Tools können manuell über den Tool-Picker aktiviert werden
        - Für spezialisierte Workflows einen Agenten aus der Agenten-Übersicht auswählen
        """;
}
