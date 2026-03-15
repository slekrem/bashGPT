# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build (Frontend wird automatisch mit gebaut)
dotnet build
dotnet build --configuration Release

# Tests ausführen
dotnet test
dotnet test --configuration Release
dotnet test --filter "FullyQualifiedName~CerebrasProvider"  # einzelne Klasse
dotnet test --filter "DisplayName~StreamAsync_StopsAtDone"  # einzelner Test

# CLI direkt ausführen
dotnet run --project src/bashGPT.Cli -- "zeige alle .cs Dateien"

# Server (Browser-UI) starten
dotnet run --project src/bashGPT.Server
dotnet run --project src/bashGPT.Server -- --port 6060 --no-browser

# Nur Frontend bauen
cd src/bashGPT.Web && npm run build
cd src/bashGPT.Web && npm run dev   # Dev-Server mit HMR (nur Frontend)
```

Das Frontend-Bundle (`dist/index.html` + `dist/bundle.js`) wird als Embedded Resource in die .NET-Assembly eingebettet. `dotnet build` ruft automatisch `npm run build` via MSBuild-Target auf.

## Architektur

bashGPT ist ein KI-gestützter Shell-Assistent (CLI + Browser-UI). Das Backend ist eine .NET 9-Solution mit mehreren Projekten.

### Projektstruktur

| Projekt | Zweck |
|---|---|
| `bashGPT.Core` | Shared Domain-Logik: Providers, Shell, Config, CLI-Runner, Session-Storage |
| `bashGPT.Cli` | CLI-Executable (`System.CommandLine`), nur Prompt-Anfragen und Config |
| `bashGPT.Server` | Server-Executable: eingebetteter HTTP-Listener, alle API-Handler |
| `bashGPT.Agents` | `AgentBase`, `AgentRegistry`, `GenericAgent` – Basis-Infrastruktur |
| `bashGPT.Agents.Shell` | Shell-Agent (`shell`) mit `shell_exec`-Tool |
| `bashGPT.Agents.Dev` | Dev-Agent (`dev`) mit Filesystem-, Git-, Build- und Test-Tools |
| `bashGPT.Tools` | Tool-Abstraktion: `ITool`, `ToolRegistry`, `ToolDefinition`, `ToolCall`, `ToolResult` |
| `bashGPT.Tools.Shell` | `shell_exec`-Tool |
| `bashGPT.Tools.Filesystem` | `filesystem_read`, `filesystem_write`, `filesystem_search` |
| `bashGPT.Tools.Git` | `git_status`, `git_diff`, `git_log`, `git_branch`, `git_add`, `git_commit`, `git_checkout` |
| `bashGPT.Tools.Build` | `build_run`-Tool |
| `bashGPT.Tools.Testing` | `test_run`-Tool |
| `bashGPT.Tools.Fetch` | `fetch`-Tool (HTTP GET mit HTML-Extraktion) |

### Datenfluss (Kernlogik)

```
User-Prompt
  → ShellContextCollector   (Git, OS, Shell, Env-Variablen)
  → System-Prompt + History → ILlmProvider.ChatAsync()
  → LLM antwortet mit Text und/oder Tool-Calls
  → ToolRegistry / CommandExecutor   (je nach ExecMode und aktivem Agenten)
  → Ergebnisse als Tool-Result-Messages zurück ans LLM
  → Follow-up-Loop
  → Ausgabe
```

Fallback: Falls keine Tool-Calls, extrahiert `BashCommandExtractor` Befehle aus ` ```bash ` Code-Blöcken.

### Wichtige Klassen

| Klasse | Pfad | Zweck |
|---|---|---|
| `CliChatRunner` | `Core/Cli/CliChatRunner.cs` | CLI-Ausführungslogik: Kontext → LLM → Ausführung → Follow-up |
| `ServerChatRunner` | `Core/Cli/ServerChatRunner.cs` | Server-Variante mit Session-, Agent- und Tool-Unterstützung |
| `ChatOrchestrator` | `Core/Cli/ChatOrchestrator.cs` | Gemeinsame Chat-Loop-Logik (Tool-Calls, Follow-up) |
| `ILlmProvider` | `Core/Providers/ILlmProvider.cs` | Abstrahiertes Provider-Interface (`ChatAsync`, `StreamAsync`, `CompleteAsync`) |
| `CerebrasProvider` | `Core/Providers/CerebrasProvider.cs` | OpenAI-kompatible Cloud-API, inkl. 429-Retry-Logik |
| `OllamaProvider` | `Core/Providers/OllamaProvider.cs` | Lokales LLM via ndjson-Stream |
| `ShellContextCollector` | `Core/Shell/ShellContextCollector.cs` | Sammelt Kontext + erstellt System-Prompt |
| `CommandExecutor` | `Core/Shell/CommandExecutor.cs` | Führt Shell-Befehle aus (30s Timeout, interaktive Befehle geblockt) |
| `BashCommandExtractor` | `Core/Shell/BashCommandExtractor.cs` | Extrahiert Befehle, prüft Danger-Patterns |
| `SessionStore` | `Core/Storage/SessionStore.cs` | Thread-sicherer Persistenz-Store für `~/.config/bashgpt/sessions/` |
| `ServerHost` | `Server/Server/ServerHost.cs` | Eingebetteter HTTP-Listener (kein ASP.NET), Routing zu API-Handlern |
| `AgentBase` | `Agents/AgentBase.cs` | Abstrakte Basisklasse für alle Chat-Agenten (code-first) |
| `AgentRegistry` | `Agents/AgentRegistry.cs` | In-Memory-Registry registrierter Agenten |
| `ToolRegistry` | `Tools/Builtins/ToolRegistry.cs` | Registry aller verfügbaren `ITool`-Implementierungen |

### Provider

Beide Provider implementieren `ILlmProvider`. Die relevante Methode für den Server-Modus ist `ChatAsync(LlmChatRequest)` — sie unterstützt Tool-Definitions, `OnToken`-Callback für Streaming und gibt `LlmChatResponse` mit `Content` + `ToolCalls` zurück.

`StreamAsync` / `CompleteAsync` sind einfachere Legacy-Methoden ohne Tool-Support.

### ExecutionMode

| Mode | Verhalten |
|---|---|
| `Ask` | Interaktive Bestätigung vor jedem Befehl |
| `AutoExec` | Sofortige Ausführung ohne Nachfrage (`-y`) |
| `DryRun` | Befehle anzeigen, nie ausführen |
| `NoExec` | Kein Anzeigen, kein Ausführen (reiner Chat) |

### Server-API (HTTP)

```
GET  /                        → index.html (embedded)
GET  /bundle.js               → JS-Bundle (embedded)
GET  /api/context             → Shell-Kontext (OS, Git, Verzeichnis)
GET  /api/settings            → Aktuelle Server-Einstellungen
POST /api/settings            → Einstellungen ändern
POST /api/chat                → { prompt, execMode } → { response, commands, usedToolCalls, logs }
POST /api/chat/stream         → Server-Sent Events (SSE), Token-Streaming
POST /api/chat/cancel         → Laufenden Chat-Request abbrechen
GET  /api/sessions            → Alle Sessions (Metadaten)
GET  /api/sessions/<id>       → Einzelne Session mit Messages
POST /api/sessions            → Neue Session anlegen
DELETE /api/sessions/<id>     → Session löschen
POST /api/sessions/clear      → Alle Sessions löschen
GET  /api/agents              → Alle registrierten Agenten
GET  /api/tools               → Alle verfügbaren Tools
GET  /api/history             → (veraltet) Legacy-History
POST /api/reset               → (veraltet) Legacy-History löschen
```

### Session-Persistenz

Sessions werden in einem Zwei-Schichten-Layout gespeichert:
- `~/.config/bashgpt/sessions/index.json` – Metadaten aller Sessions
- `~/.config/bashgpt/sessions/<id>/content.json` – Nachrichten einer Session
- `~/.config/bashgpt/sessions/<id>/requests/` – Rohe LLM-Requests/Responses (Debug)

`SessionStore` ist thread-safe via `SemaphoreSlim` und schreibt atomar via Temp-Datei. Maximum: 20 Sessions.

### Konfiguration

Gespeichert in `~/.config/bashgpt/config.json`. Env-Variablen überschreiben die Datei:
- `BASHGPT_PROVIDER`, `BASHGPT_CEREBRAS_KEY`, `BASHGPT_CEREBRAS_MODEL`
- `BASHGPT_OLLAMA_URL`, `BASHGPT_OLLAMA_MODEL`

### Frontend

TypeScript + Lit Web Components, gebaut mit Vite als Single Bundle. Komponenten: `chat-app` (Router), `chat-view`, `dashboard`, `sidebar`, `terminal-panel`, `settings-view`.

## Coding Style

- C# mit Nullable Reference Types und Implicit Usings
- 4 Spaces, file-scoped Namespaces
- `PascalCase` für Types/Members, `camelCase` für Locals
- Tests: xUnit, Naming `Method_Condition_Result` (z.B. `StreamAsync_StopsAtDone`)
- Commits: Conventional Messages (`feat:`, `fix:`, `test:` etc.), oft auf Deutsch
