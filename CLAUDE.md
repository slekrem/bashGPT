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
dotnet test --filter "FullyQualifiedName~AgentRunner"       # Agent-Tests
dotnet test --filter "FullyQualifiedName~AgentStore"        # Store-Tests

# CLI direkt ausführen
dotnet run --project src/bashGPT.Cli -- "zeige alle .cs Dateien"
dotnet run --project src/bashGPT.Cli -- server  # Web-UI auf Port 5050

# Agent-Modus
dotnet run --project src/bashGPT.Cli -- agent add git --name my-repo --path . --every 5
dotnet run --project src/bashGPT.Cli -- agent add http --name my-api --url https://example.com --every 60
dotnet run --project src/bashGPT.Cli -- agent list
dotnet run --project src/bashGPT.Cli -- agent status <name-or-id>
dotnet run --project src/bashGPT.Cli -- agent pause <name-or-id>
dotnet run --project src/bashGPT.Cli -- agent resume <name-or-id>
dotnet run --project src/bashGPT.Cli -- agent remove <name-or-id>
dotnet run --project src/bashGPT.Cli -- agent run   # Ctrl+C zum Beenden

# Nur Frontend bauen
cd src/bashGPT.Web && npm run build
cd src/bashGPT.Web && npm run dev   # Dev-Server mit HMR (nur Frontend)
```

Das Frontend-Bundle (`dist/index.html` + `dist/bundle.js`) wird als Embedded Resource in die .NET-Assembly eingebettet. `dotnet build` ruft automatisch `npm run build` via MSBuild-Target auf.

## Architektur

bashGPT ist ein KI-gestützter Shell-Assistent (CLI + Browser-UI). Das Backend ist eine einzelne .NET 9-Anwendung, die bei Bedarf einen eingebetteten HTTP-Server hochfährt.

### Datenfluss (Kernlogik)

```
User-Prompt
  → ShellContextCollector   (Git, OS, Shell, Env-Variablen)
  → System-Prompt + History → ILlmProvider.ChatAsync()
  → LLM antwortet mit Text und/oder Tool-Calls (bash-Tool)
  → CommandExecutor.ProcessAsync()   (je nach ExecMode)
  → Ergebnisse als Tool-Result-Messages zurück ans LLM
  → Follow-up-Loop (max 3 Runden, Loop-Guard)
  → Ausgabe
```

Fallback: Falls keine Tool-Calls, extrahiert `BashCommandExtractor` Befehle aus ` ```bash ` Code-Blöcken.

### Wichtige Klassen

| Klasse | Pfad | Zweck |
|---|---|---|
| `PromptHandler` | `Cli/PromptHandler.cs` | Hauptlogik: Kontext → LLM → Ausführung → Follow-up |
| `ILlmProvider` | `Providers/ILlmProvider.cs` | Abstrahiertes Provider-Interface (`ChatAsync`, `StreamAsync`, `CompleteAsync`) |
| `CerebrasProvider` | `Providers/CerebrasProvider.cs` | OpenAI-kompatible Cloud-API, inkl. 429-Retry-Logik (max 3 Versuche, `Retry-After`-Header) |
| `OllamaProvider` | `Providers/OllamaProvider.cs` | Lokales LLM via ndjson-Stream |
| `ShellContextCollector` | `Shell/ShellContextCollector.cs` | Sammelt Kontext + erstellt System-Prompt |
| `CommandExecutor` | `Shell/CommandExecutor.cs` | Führt Shell-Befehle aus (30s Timeout, interaktive Befehle geblockt) |
| `BashCommandExtractor` | `Shell/BashCommandExtractor.cs` | Extrahiert Befehle, prüft Danger-Patterns |
| `ServerHost` | `Server/ServerHost.cs` | Eingebetteter HTTP-Listener (kein ASP.NET) |
| `AgentRunner` | `Agents/AgentRunner.cs` | 1s-Polling-Loop, führt aktive Agenten zyklisch aus, meldet bei Änderungen |
| `AgentStore` | `Agents/AgentStore.cs` | Thread-sicherer Persistenz-Store für `~/.config/bashgpt/agents.json` |
| `GitStatusCheck` | `Agents/GitStatusCheck.cs` | Prüft `git status --porcelain`, SHA256-Hash als Fingerprint |
| `HttpStatusCheck` | `Agents/HttpStatusCheck.cs` | HTTP GET, Statuscode-Wechsel als Änderungssignal |

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
GET  /             → index.html (embedded)
GET  /bundle.js    → JS-Bundle (embedded)
POST /api/chat     → { prompt, execMode } → { response, commands, usedToolCalls, logs }
GET  /api/history  → { history: ChatMessage[] }
POST /api/reset    → History löschen
```

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
