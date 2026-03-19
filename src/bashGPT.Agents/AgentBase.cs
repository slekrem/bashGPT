using System.Text;

namespace BashGPT.Agents;

/// <summary>
/// Abstrakte Basisklasse für alle Chat-Agenten.
/// Jeder Agent definiert sich vollständig durch Code – keine JSON-Konfiguration.
/// Aus der Klassendefinition werden sowohl das LLM-System-Prompt als auch
/// das Info-Panel der Web-UI abgeleitet.
/// </summary>
public abstract class AgentBase
{
    /// <summary>Eindeutige, stabile Kennung des Agenten (z. B. "dev", "shell").</summary>
    public abstract string Id { get; }

    /// <summary>Anzeigename des Agenten in der UI.</summary>
    public abstract string Name { get; }

    /// <summary>Liste der Tool-Namen, die für diesen Agenten aktiv sind.</summary>
    public abstract IReadOnlyList<string> EnabledTools { get; }

    /// <summary>
    /// System-Prompts, die dem LLM bei jedem Chat übergeben werden.
    /// Jeder Eintrag wird als separate System-Nachricht übermittelt.
    /// </summary>
    public abstract IReadOnlyList<string> SystemPrompt { get; }

    /// <summary>
    /// Optionale LLM-Konfiguration des Agenten (Modell, Temperatur, Top-P, etc.).
    /// Wird automatisch als Abschnitt im Info-Panel angezeigt.
    /// </summary>
    public virtual AgentLlmConfig? LlmConfig => null;

    /// <summary>
    /// Agentenspezifischer Markdown-Inhalt für das Info-Panel
    /// (ohne LLM-Konfiguration — dieser Abschnitt wird automatisch ergänzt).
    /// </summary>
    protected abstract string GetAgentMarkdown();

    /// <summary>
    /// Gibt das vollständige Info-Panel-Markdown zurück.
    /// Wenn <paramref name="effectiveConfig"/> angegeben ist, werden diese Werte angezeigt
    /// (z. B. bereits mit Provider-Defaults gemergt). Andernfalls wird <see cref="LlmConfig"/> verwendet.
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
            sb.AppendLine("| Parameter | Wert |");
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
