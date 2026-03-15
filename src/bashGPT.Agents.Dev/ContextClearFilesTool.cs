using BashGPT.Tools.Abstractions;

namespace BashGPT.Agents.Dev;

/// <summary>
/// Agenten-spezifisches Tool: Löscht den gesamten session-spezifischen Kontext-Cache.
/// </summary>
public sealed class ContextClearFilesTool : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "context_clear_files",
        Description: "Removes all loaded files from the context at once.",
        Parameters: []);

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        var count = ContextFileCache.ReadFiles(call.SessionPath).Count;
        ContextFileCache.Clear(call.SessionPath);

        return Task.FromResult(new ToolResult(
            Success: true,
            Content: count > 0
                ? $"Kontext geleert – {count} Datei(en) entfernt."
                : "Kontext war bereits leer."));
    }
}
