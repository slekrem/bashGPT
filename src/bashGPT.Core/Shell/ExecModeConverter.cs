namespace BashGPT.Shell;

/// <summary>
/// Zentrale Konvertierung zwischen dem C#-Enum <see cref="ExecutionMode"/> und den
/// Frontend-Strings ('ask' | 'auto-exec' | 'dry-run' | 'no-exec').
///
/// Mapping:
///   ExecutionMode.Ask      ↔  "ask"
///   ExecutionMode.AutoExec ↔  "auto-exec"
///   ExecutionMode.DryRun   ↔  "dry-run"
///   ExecutionMode.NoExec   ↔  "no-exec"
///
/// Bei Erweiterung des Enums schlägt der Compiler im switch-Ausdruck an.
/// </summary>
public static class ExecModeConverter
{
    public static string ToString(ExecutionMode mode) =>
        mode switch
        {
            ExecutionMode.Ask      => "ask",
            ExecutionMode.AutoExec => "auto-exec",
            ExecutionMode.DryRun   => "dry-run",
            ExecutionMode.NoExec   => "no-exec",
            _                      => "ask"
        };

    public static ExecutionMode? Parse(string? mode) =>
        mode?.ToLowerInvariant() switch
        {
            "ask"       => ExecutionMode.Ask,
            "auto-exec" => ExecutionMode.AutoExec,
            "dry-run"   => ExecutionMode.DryRun,
            "no-exec"   => ExecutionMode.NoExec,
            _           => null
        };
}
