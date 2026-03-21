using System.Text;

namespace bashGPT.Agents;

/// <summary>
/// Abstract base class for all chat agents.
/// Each agent is defined entirely in code — no JSON configuration required.
/// The class definition drives both the LLM system prompt and the info panel shown in the web UI.
/// </summary>
/// <remarks>
/// <para><b>How to create a custom agent:</b></para>
/// <para>
/// Subclass <see cref="AgentBase"/>, implement the required abstract members, and register
/// your agent with <see cref="AgentRegistry"/>. No other wiring is needed.
/// </para>
/// <code>
/// public sealed class MyAgent : AgentBase
/// {
///     public override string Id           => "my-agent";
///     public override string Name         => "My Agent";
///     public override IReadOnlyList&lt;string&gt; EnabledTools  => ["shell_exec"];
///     public override IReadOnlyList&lt;string&gt; SystemPrompt  => ["You are my custom agent."];
///     protected override string GetAgentMarkdown()        => "# My Agent\n\nDoes awesome things.";
/// }
/// </code>
/// <para><b>Extension points:</b></para>
/// <list type="bullet">
///   <item><term><see cref="Id"/></term><description>Unique, stable identifier — never change this after deployment.</description></item>
///   <item><term><see cref="Name"/></term><description>Human-readable display name shown in the UI.</description></item>
///   <item><term><see cref="EnabledTools"/></term><description>Tool names available to this agent. Must match registered tool names.</description></item>
///   <item><term><see cref="SystemPrompt"/></term><description>One or more system messages sent to the LLM at the start of every request. Can be dynamic (computed properties).</description></item>
///   <item><term><see cref="LlmConfig"/></term><description>Optional: override temperature, top-p, context size, etc. Return null to use the server default.</description></item>
///   <item><term><see cref="GetAgentMarkdown"/></term><description>Markdown shown in the info panel. The LLM configuration table is appended automatically.</description></item>
/// </list>
/// </remarks>
public abstract class AgentBase
{
    /// <summary>Unique, stable identifier for the agent (e.g. "dev", "shell").</summary>
    public abstract string Id { get; }

    /// <summary>Display name shown in the UI.</summary>
    public abstract string Name { get; }

    /// <summary>Names of the tools that are active for this agent.</summary>
    public abstract IReadOnlyList<string> EnabledTools { get; }

    /// <summary>
    /// System prompts sent to the LLM at the start of every chat.
    /// Each entry is transmitted as a separate system message.
    /// </summary>
    public abstract IReadOnlyList<string> SystemPrompt { get; }

    /// <summary>
    /// Returns the system prompts for a specific session.
    /// Override this method when the system prompt depends on session-scoped state
    /// (e.g. files loaded into context). The default implementation ignores
    /// <paramref name="sessionPath"/> and delegates to <see cref="SystemPrompt"/>.
    /// </summary>
    public virtual IReadOnlyList<string> GetSystemPrompt(string? sessionPath = null) => SystemPrompt;

    /// <summary>
    /// Optional LLM configuration for this agent (model, temperature, top-p, etc.).
    /// Automatically included as a section in the info panel.
    /// </summary>
    public virtual AgentLlmConfig? LlmConfig => null;

    /// <summary>
    /// Agent-specific markdown content for the info panel,
    /// excluding the LLM configuration section which is appended automatically.
    /// </summary>
    protected abstract string GetAgentMarkdown();

    /// <summary>
    /// Returns the full info-panel markdown for this agent.
    /// When <paramref name="effectiveConfig"/> is provided those values are used
    /// (e.g. already merged with provider defaults); otherwise <see cref="LlmConfig"/> is used.
    /// </summary>
    public string GetInfoPanelMarkdown(AgentLlmConfig? effectiveConfig = null)
    {
        var sb = new StringBuilder(GetAgentMarkdown().TrimEnd());

        var cfg = effectiveConfig ?? LlmConfig;
        if (cfg is not null)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("## LLM Configuration");
            sb.AppendLine();
            sb.AppendLine("| Parameter | Value |");
            sb.AppendLine("|---|---|");

            if (cfg.Model is not null)
                sb.AppendLine($"| `model` | `{cfg.Model}` |");
            sb.AppendLine($"| `temperature` | `{cfg.Temperature?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-"}` |");
            sb.AppendLine($"| `top_p` | `{cfg.TopP?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-"}` |");
            if (cfg.NumCtx is not null)
                sb.AppendLine($"| `num_ctx` | `{cfg.NumCtx}` |");
            if (cfg.MaxTokens is not null)
                sb.AppendLine($"| `max_tokens` | `{cfg.MaxTokens}` |");
            if (cfg.Seed is not null)
                sb.AppendLine($"| `seed` | `{cfg.Seed}` |");
            if (cfg.ReasoningEffort is not null)
                sb.AppendLine($"| `reasoning_effort` | `{cfg.ReasoningEffort}` |");
            if (cfg.ParallelToolCalls is not null)
                sb.AppendLine($"| `parallel_tool_calls` | `{(cfg.ParallelToolCalls.Value ? "true" : "false")}` |");
            sb.AppendLine($"| `stream` | `{(cfg.Stream ? "true" : "false")}` |");
            sb.AppendLine($"| `stream_options` | `{{\"include_usage\": true}}` |");
        }

        return sb.ToString();
    }
}
