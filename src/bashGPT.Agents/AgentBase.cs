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

    /// <summary>System-Prompt, der dem LLM bei jedem Chat übergeben wird.</summary>
    public abstract string SystemPrompt { get; }

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
    /// Besteht aus dem agentenspezifischen Inhalt gefolgt von der LLM-Konfiguration,
    /// sofern <see cref="LlmConfig"/> definiert ist.
    /// </summary>
    public string GetInfoPanelMarkdown()
    {
        var sb = new StringBuilder(GetAgentMarkdown().TrimEnd());

        if (LlmConfig is { } cfg)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("## LLM-Konfiguration");
            sb.AppendLine();
            sb.AppendLine("| Parameter | Wert |");
            sb.AppendLine("|---|---|");

            if (cfg.Model is not null)
                sb.AppendLine($"| `model` | `{cfg.Model}` |");
            if (cfg.Temperature is not null)
                sb.AppendLine($"| `temperature` | `{cfg.Temperature}` |");
            if (cfg.TopP is not null)
                sb.AppendLine($"| `top_p` | `{cfg.TopP}` |");
            if (cfg.NumCtx is not null)
                sb.AppendLine($"| `num_ctx` | `{cfg.NumCtx}` |");
            if (cfg.MaxTokens is not null)
                sb.AppendLine($"| `max_tokens` | `{cfg.MaxTokens}` |");

            sb.AppendLine($"| `stream` | `{(cfg.Stream ? "true" : "false")}` |");
            sb.AppendLine($"| `stream_options` | `{{\"include_usage\": true}}` |");
        }

        return sb.ToString();
    }
}
