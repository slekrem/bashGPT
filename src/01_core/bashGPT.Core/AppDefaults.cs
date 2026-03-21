namespace bashGPT.Core;

/// <summary>
/// Central collection of application-wide default values and magic numbers.
/// </summary>
public static class AppDefaults
{
    /// <summary>Timeout per shell command in seconds (CommandExecutor).</summary>
    public const int CommandTimeoutSeconds = 300;

    /// <summary>Prefix for automatically generated session IDs.</summary>
    public const string SessionIdPrefix = "s-";
}
