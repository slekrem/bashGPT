using BashGPT.Providers;

namespace bashGPT.Core;

/// <summary>
/// Zentrale Sammlung aller applikationsweiten Standardwerte und Magic Numbers.
/// </summary>
public static class AppDefaults
{
    /// <summary>Timeout pro Shell-Befehl in Sekunden (CommandExecutor).</summary>
    public const int CommandTimeoutSeconds = 300;

    /// <summary>Präfix für automatisch generierte Session-IDs.</summary>
    public const string SessionIdPrefix = "s-";
}
