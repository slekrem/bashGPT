namespace BashGPT;

/// <summary>
/// Zentrale Sammlung aller applikationsweiten Standardwerte und Magic Numbers.
/// </summary>
public static class AppDefaults
{
    /// <summary>Maximale Tool-Call-Runden pro LLM-Anfrage in PromptHandler.</summary>
    public const int MaxToolCallRounds = 3;

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
}
