using System.Text.RegularExpressions;

namespace bashGPT.Shell;

public record ExtractedCommand(string Command, bool IsDangerous, string? DangerReason);

public static class BashCommandExtractor
{
    // Trifft ```bash ... ```, ```powershell ... ```, ```cmd ... ``` und ``` ... ``` (ohne Sprachangabe)
    private static readonly Regex CodeBlockRegex = new(
        @"```(?:bash|sh|shell|zsh|powershell|ps1|cmd|bat|batch)?\s*\n(.*?)\n\s*```",
        RegexOptions.Singleline | RegexOptions.Compiled);

    // Muster für gefährliche Befehle
    private static readonly IReadOnlyList<(Regex Pattern, string Reason)> DangerPatterns =
    [
        (new(@"\brm\s+(-\w*f\w*|-\w*r\w*){1,}\b", RegexOptions.Compiled),
            "rm mit -r oder -f (löscht Dateien unwiderruflich)"),
        (new(@"\bsudo\b", RegexOptions.Compiled),
            "sudo (erhöhte Rechte)"),
        (new(@"\bdd\b", RegexOptions.Compiled),
            "dd (direkter Gerätezugriff)"),
        (new(@"\bmkfs\b", RegexOptions.Compiled),
            "mkfs (Dateisystem formatieren)"),
        (new(@">\s*/dev/", RegexOptions.Compiled),
            "Schreiben auf Gerätedatei"),
        (new(@"\bchmod\s+.*(777|a\+[rwx])", RegexOptions.Compiled),
            "chmod 777 / a+x (unsichere Berechtigungen)"),
        (new(@"\bcurl\b.*\|\s*(ba)?sh\b", RegexOptions.Compiled),
            "curl | sh (ungeprüfte Ausführung aus dem Netz)"),
        (new(@"\bwget\b.*-O\s*-.*\|\s*(ba)?sh\b", RegexOptions.Compiled),
            "wget | sh (ungeprüfte Ausführung aus dem Netz)"),
        (new(@":\s*\(\s*\)\s*\{.*:\|:.*\}", RegexOptions.Compiled),
            "Fork-Bomb-Muster"),
        (new(@"\bformat\s+[a-zA-Z]:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "format (Laufwerk formatieren)"),
        (new(@"\brd\b.*(/s|/q)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "rd /s /q (Verzeichnis unwiderruflich löschen)"),
        (new(@"\brmdir\b.*(/s|/q)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "rmdir /s /q (Verzeichnis unwiderruflich löschen)"),
        (new(@"\bReg\s+(Delete|Add)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "Registry-Änderung (kann System beschädigen)"),
    ];

    public static IReadOnlyList<ExtractedCommand> Extract(string llmResponse)
    {
        var results = new List<ExtractedCommand>();

        foreach (Match match in CodeBlockRegex.Matches(llmResponse))
        {
            var block = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(block)) continue;

            // Mehrzeilige Blöcke als einzelne Befehle behandeln
            foreach (var rawLine in block.Split('\n'))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith('#')) continue; // Kommentare überspringen

                var (isDangerous, reason) = CheckDanger(line);
                results.Add(new ExtractedCommand(line, isDangerous, reason));
            }
        }

        return results;
    }

    public static (bool IsDangerous, string? Reason) CheckDanger(string command)
    {
        foreach (var (pattern, reason) in DangerPatterns)
        {
            if (pattern.IsMatch(command))
                return (true, reason);
        }
        return (false, null);
    }
}
