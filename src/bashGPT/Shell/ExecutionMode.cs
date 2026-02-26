namespace BashGPT.Shell;

public enum ExecutionMode
{
    /// <summary>Vor jeder Ausführung nachfragen (Standard).</summary>
    Ask,

    /// <summary>Befehle ohne Rückfrage ausführen (--auto-exec / -y).</summary>
    AutoExec,

    /// <summary>Befehle anzeigen, aber nie ausführen (--dry-run).</summary>
    DryRun,

    /// <summary>Befehle weder anzeigen noch ausführen (--no-exec).</summary>
    NoExec,
}
