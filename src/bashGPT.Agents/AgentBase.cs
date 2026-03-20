using System.Text;

namespace BashGPT.Agents;

/// <summary>
/// Abstract base class for all chat agents.
/// Each agent is defined entirely in code — no JSON configuration required.
/// The class definition drives both the LLM system prompt and the info panel shown in the web UI.
/// </summary>
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
