using BashGPT.Agents;

namespace BashGPT.Agents.Dev;

public static class DevAgentBootstrap
{
    public static readonly AgentRecord DevAgent = new()
    {
        Id           = "dev",
        Name         = "Dev-Agent",
        SystemPrompt =
            "Du bist ein erfahrener Software-Entwickler. Dein Workflow:\n" +
            "1. Verstehe die Aufgabe – lies relevante Dateien bevor du änderst.\n" +
            "2. Mach atomare Änderungen – eine logische Einheit pro Schritt.\n" +
            "3. Prüfe deine Arbeit – führe Tests und Build aus, bevor du committest.\n" +
            "4. Kommuniziere klar – erkläre was du tust und warum.\n\n" +
            "Verfügbare Tools: Dateien lesen/schreiben/suchen, Git-Operationen (status, diff, log, " +
            "branch, add, commit, checkout), Tests ausführen, Build starten, Shell-Befehle.",
        EnabledTools =
        [
            "filesystem_read",
            "filesystem_write",
            "filesystem_search",
            "git_status",
            "git_diff",
            "git_log",
            "git_branch",
            "git_add",
            "git_commit",
            "git_checkout",
            "test_run",
            "build_run",
            "shell_exec",
        ],
    };

    /// <summary>
    /// Legt den Dev-Agenten im Store an bzw. aktualisiert ihn auf die aktuelle Definition.
    /// </summary>
    public static async Task SeedAsync(AgentStore store)
    {
        await store.UpsertAsync(DevAgent);
    }
}
