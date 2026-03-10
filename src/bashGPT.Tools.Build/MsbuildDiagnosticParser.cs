using System.Text.RegularExpressions;

namespace BashGPT.Tools.Build;

/// <summary>
/// Parses MSBuild diagnostic lines from `dotnet build` output.
/// Handles: path(line,col): error/warning CODE: message
/// </summary>
public static class MsbuildDiagnosticParser
{
    // e.g. "/path/to/File.cs(12,5): error CS1234: Some message [Project.csproj]"
    private static readonly Regex DiagRx = new(
        @"^(?<file>.+?)\((?<line>\d+),(?<col>\d+)\):\s+(?<severity>error|warning)\s+(?<code>\w+):\s+(?<msg>.+?)(?:\s+\[.+\])?$",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // MSBUILD : error MSBuildCode: message (no file/line)
    private static readonly Regex MsbuildErrorRx = new(
        @"^MSBUILD\s*:\s*(?<severity>error|warning)\s+(?<code>\w+):\s+(?<msg>.+)$",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static (IReadOnlyList<BuildDiagnostic> Errors, IReadOnlyList<BuildDiagnostic> Warnings) Parse(string output)
    {
        var errors = new List<BuildDiagnostic>();
        var warnings = new List<BuildDiagnostic>();

        foreach (Match m in DiagRx.Matches(output))
        {
            var d = new BuildDiagnostic(
                Severity: m.Groups["severity"].Value.ToLowerInvariant(),
                Code:     m.Groups["code"].Value,
                File:     m.Groups["file"].Value.Trim(),
                Line:     int.Parse(m.Groups["line"].Value),
                Column:   int.Parse(m.Groups["col"].Value),
                Message:  m.Groups["msg"].Value.Trim());

            if (d.Severity == "error") errors.Add(d);
            else warnings.Add(d);
        }

        foreach (Match m in MsbuildErrorRx.Matches(output))
        {
            var d = new BuildDiagnostic(
                Severity: m.Groups["severity"].Value.ToLowerInvariant(),
                Code:     m.Groups["code"].Value,
                File:     string.Empty,
                Line:     0,
                Column:   0,
                Message:  m.Groups["msg"].Value.Trim());

            if (d.Severity == "error") errors.Add(d);
            else warnings.Add(d);
        }

        return (errors, warnings);
    }
}
