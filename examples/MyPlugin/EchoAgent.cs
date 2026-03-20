using bashGPT.Agents;

namespace MyPlugin;

/// <summary>
/// Example agent that uses only the custom EchoTool.
/// Drop this DLL into ~/.config/bashgpt/plugins/MyPlugin/ to activate it.
/// </summary>
public sealed class EchoAgent : AgentBase
{
    public override string Id => "echo";

    public override string Name => "Echo Agent";

    public override IReadOnlyList<string> EnabledTools => ["echo"];

    public override IReadOnlyList<string> SystemPrompt =>
    [
        "You are a demo agent. When asked to echo something, use the echo tool.",
    ];

    protected override string GetAgentMarkdown() => """
        # Echo Agent

        A minimal example agent that ships with the MyPlugin example.

        ## Available tools

        | Tool | Description |
        |---|---|
        | `echo` | Echoes a message back to the model. |

        This agent is provided as a starting point — replace it with your own logic.
        """;
}
