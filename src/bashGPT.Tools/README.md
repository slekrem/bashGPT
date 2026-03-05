# bashGPT.Tools

Zentrales Projekt für LLM-Tooling in bashGPT.

## Struktur

- `Abstractions/`: Tool-Contracts und gemeinsame Modelle (`ITool`, `ToolDefinition`, `ToolCall`, `ToolResult`)
- `Execution/`: Laufzeitnahe Infrastruktur (z. B. `ToolRegistry`)
- `Builtins/`: Eingebaute Tool-Implementierungen

## Ziel

Dieses Projekt ist das Fundament für zukünftige Tool-Implementierungen und Migrationen aus anderen Modulen.
