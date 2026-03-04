using BashGPT.Providers;

namespace BashGPT;

/// <summary>
/// Zentrale Sammlung aller applikationsweiten Standardwerte und Magic Numbers.
/// </summary>
public static class AppDefaults
{
    /// <summary>Maximale Tool-Call-Runden pro LLM-Anfrage in PromptHandler.</summary>
    public const int MaxToolCallRounds = 8;

    /// <summary>Timeout pro Shell-Befehl in Sekunden (CommandExecutor).</summary>
    public const int CommandTimeoutSeconds = 30;

    /// <summary>Maximale Ausgabe-Zeichen pro Shell-Befehl (CommandExecutor).</summary>
    public const int MaxCommandOutputChars = 10_000;

    /// <summary>Maximale Anzahl beibehaltener Nachrichten im In-Memory-Verlauf (LegacyHistory).</summary>
    public const int MaxHistoryMessages = 40;

    /// <summary>Maximale Anzahl HTTP-Wiederholungen bei 429-Fehlern (CerebrasProvider).</summary>
    public const int MaxProviderRetries = 3;

    /// <summary>Präfix für automatisch generierte Session-IDs.</summary>
    public const string SessionIdPrefix = "s-";

    /// <summary>Meldung bei echter Tool-Call-Schleife (identische Befehle wiederholen sich).</summary>
    public const string LoopDetectedMessage =
        "Tool-Call-Schleife erkannt und beendet. " +
        "Bitte nutze nicht-interaktive Befehle (z. B. 'ps aux --sort=-%cpu | head' statt 'top').";

    /// <summary>Meldung wenn die maximale Anzahl an Tool-Call-Runden legitim erreicht wird.</summary>
    public static readonly string MaxRoundsReachedMessage =
        $"Maximale Anzahl Tool-Call-Runden ({MaxToolCallRounds}) erreicht. " +
        "Die Aufgabe wurde möglicherweise nicht vollständig abgeschlossen.";

    /// <summary>
    /// Erkennt eine Tool-Call-Schleife: Gibt true zurück, wenn previous und current
    /// dieselbe Anzahl Tool-Calls mit identischen Namen und ArgumentsJson (positionsbasiert) haben.
    /// </summary>
    public static bool DetectLoop(IReadOnlyList<ToolCall>? previous, IReadOnlyList<ToolCall> current)
    {
        if (previous is null || previous.Count == 0)
            return false;
        if (current.Count == 0)
            return false;
        if (previous.Count != current.Count)
            return false;

        for (var i = 0; i < current.Count; i++)
        {
            if (previous[i].Name != current[i].Name)
                return false;
            if (previous[i].ArgumentsJson != current[i].ArgumentsJson)
                return false;
        }

        return true;
    }
}
