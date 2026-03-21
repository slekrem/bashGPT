namespace bashGPT.Tools.Build;

public sealed record BuildDiagnostic(
    string Severity,  // "error" | "warning"
    string Code,
    string File,
    int Line,
    int Column,
    string Message);
