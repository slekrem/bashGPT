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
dotnet test --filter "FullyQualifiedName~DevAgent"          # Dev-Agent-Tests
dotnet test --filter "FullyQualifiedName~ShellAgent"        # Shell-Agent-Tests
dotnet test --filter "FullyQualifiedName~ShellExecTool"     # Tool-Tests
dotnet test --project tests/bashGPT.Tools.Git.Tests         # einzelnes Test-Projekt

# CLI direkt ausführen
dotnet run --project src/bashGPT.Cli -- "zeige alle .cs Dateien"

# Server (Browser-UI) starten
dotnet run --project src/bashGPT.Server
dotnet run --project src/bashGPT.Server -- --port 6060 --no-browser

# Nur Frontend bauen
cd src/bashGPT.Web && npm run build
cd src/bashGPT.Web && npm run dev   # Dev-Server mit HMR (nur Frontend)

# Coverage-Report (HTML) generieren
./scripts/coverage-report.sh        # Output: coverage/report/index.html
```

Das Frontend-Bundle (`dist/index.html` + `dist/bundle.js`) wird als Embedded Resource in `bashGPT.Server.csproj` eingebettet. `dotnet build` ruft automatisch `npm install` + `npm run build` via MSBuild-Target (`BeforeTargets="BeforeBuild"`) auf. Voraussetzung: **Node.js ≥ 20.19.0 oder ≥ 22.12.0** (Vite 7). CI/CD: `.github/workflows/ci.yml` — baut auf `ubuntu-latest` bei Push/PR auf `main` (dotnet 9 + node 20, Release-Build, TRX-Test-Results als Artifact).

## Architektur

bashGPT ist ein KI-gestützter Shell-Assistent (CLI + Browser-UI). Das Backend ist eine .NET 9-Solution mit mehreren Projekten.

### Projektstruktur

| Projekt | Zweck |
|---|---|
| `bashGPT.Core` | Shared Domain-Logik: Providers, Shell, Config, CLI-Runner, Session-Storage |
| `bashGPT.Cli` | CLI-Executable (`System.CommandLine`), nur Prompt-Anfragen und Config |
| `bashGPT.Server` | Server-Executable: eingebetteter HTTP-Listener, alle API-Handler |
| `bashGPT.Agents` | `AgentBase`, `AgentRegistry`, `GenericAgent` – Basis-Infrastruktur |
| `bashGPT.Agents.Shell` | Shell-Agent (`shell`): nur `shell_exec`, Temperature=0.1 |
| `bashGPT.Agents.Dev` | Dev-Agent (`dev`): 17 Tools aktiv (fetch, filesystem_*, git_*, test_run, build_run, shell_exec, context_*), Temperature=0.1, NumCtx=64K; enthält auch `context_load_files`/`context_unload_files`/`context_clear_files`-Tools und `ContextFileCache` |
| `bashGPT.Tools` | Tool-Abstraktion: `ITool`, `ToolDefinition`, `ToolCall`, `ToolResult`; `ToolRegistry` in `Execution/`-Unterordner (Namespace `BashGPT.Tools.Execution`) |
| `bashGPT.Tools.Shell` | `shell_exec`-Tool |
| `bashGPT.Tools.Filesystem` | `filesystem_read`, `filesystem_write`, `filesystem_search` |
| `bashGPT.Tools.Git` | `git_status`, `git_diff`, `git_log`, `git_branch`, `git_add`, `git_commit`, `git_checkout` |
| `bashGPT.Tools.Build` | `build_run`-Tool |
| `bashGPT.Tools.Testing` | `test_run`-Tool |
| `bashGPT.Tools.Fetch` | `fetch`-Tool (HTTP GET mit HTML-Extraktion) |

### Datenfluss

**CLI-Modus (`CliChatRunner`):**
```
User-Prompt
  → ShellContextCollector   (Git, OS, Shell, gefilterte Env-Variablen)
  → System-Prompt + History → ILlmProvider.ChatAsync()
  → LLM antwortet mit Text und/oder Tool-Calls (bash-Tool)
  → CommandExecutor   (ExecMode: Ask / AutoExec / DryRun / NoExec)
  → Ergebnisse als Tool-Result-Messages zurück ans LLM
  → Follow-up-Loop (max. 8 Runden, Loop-Guard bei identischen Tool-Calls)
  → Ausgabe auf Console
```
Fallback: Falls keine Tool-Calls, extrahiert `BashCommandExtractor` Befehle aus ` ```bash ` Code-Blöcken.

**Server-Modus (`ServerChatRunner`):**
```
POST /api/chat   (prompt, sessionId?, agentId?, enabledTools?)
  → Agent-Lookup via AgentRegistry → SystemPrompt + LlmConfig
  → Session laden via SessionStore
  → ILlmProvider.ChatAsync()  (mit LlmRateLimiter)
  → LLM antwortet mit Tool-Calls
  → ToolRegistry.TryGet(name) → ITool.ExecuteAsync()
  → Ergebnisse als Tool-Result-Messages zurück ans LLM
  → Follow-up-Loop (max. 8 Runden)
  → Session persistieren, ServerChatResult zurückgeben
```
Hinweis: ExecMode wird im Server-Modus nicht ausgewertet — das Verhalten steuern die Tools selbst.

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
| `CommandExecutor` | `Core/Shell/CommandExecutor.cs` | Führt Shell-Befehle aus (300s Timeout); blockiert interaktive Befehle: `htop`, `btop`, `watch`, `less`, `more`, `man`, `vim`, `vi`, `nano`, `emacs`, `top` (ohne `-l`/`-n`), `tail -f`, `ping` (ohne `-c`/`-n`) |
| `BashCommandExtractor` | `Core/Shell/BashCommandExtractor.cs` | Fallback-Extraktion aus ` ```bash ` Blöcken; prüft 13 Danger-Patterns (u.a. `rm -rf`, `sudo`, `dd`, `mkfs`, `curl \| sh`, Fork-Bomb) |
| `LlmRateLimiter` | `Core/Providers/LlmRateLimiter.cs` | Sliding-Window Rate-Limiter (nur Server); Mindestabstand + Max-Anfragen/Minute |
| `SessionStore` | `Core/Storage/SessionStore.cs` | Thread-sicherer Persistenz-Store für `~/.config/bashgpt/sessions/` |
| `ServerHost` | `Server/Server/ServerHost.cs` | Eingebetteter HTTP-Listener (kein ASP.NET), Routing zu API-Handlern |
| `AgentBase` | `Agents/AgentBase.cs` | Abstrakte Basisklasse für alle Chat-Agenten (code-first) |
| `AgentRegistry` | `Agents/AgentRegistry.cs` | In-Memory-Registry registrierter Agenten |
| `ToolRegistry` | `Tools/Builtins/ToolRegistry.cs` | Registry aller verfügbaren `ITool`-Implementierungen |
| `ContextFileCache` | `Agents.Dev/ContextFileCache.cs` | Persistiert geladene Dateipfade pro Session (AsyncLocal für Thread-Isolation); genutzt vom Dev-Agent |
| `RunningChatRegistry` | `Server/Server/RunningChatRegistry.cs` | ConcurrentDictionary von RequestId → CancellationTokenSource; ermöglicht `POST /api/chat/cancel` |

### Provider

Beide Provider implementieren `ILlmProvider`. Die relevante Methode für den Server-Modus ist `ChatAsync(LlmChatRequest)` — sie unterstützt Tool-Definitions, `OnToken`/`OnReasoningToken`-Callbacks für Streaming und gibt `LlmChatResponse` mit `Content` + `ToolCalls` + `Usage` zurück.

`StreamAsync` / `CompleteAsync` sind einfachere Legacy-Methoden ohne Tool-Support.

**Besonderheiten:**
- `ILlmProvider` hat `Name` und `Model` Properties sowie drei Methoden: `ChatAsync` (Tool-Support), `CompleteAsync` (Legacy, kein Tool-Support), `StreamAsync` (Legacy, kein Tool-Support).
- `RateLimitedLlmProvider` ist ein Dekorator, der einen echten Provider umhüllt und `LlmRateLimiter.WaitAsync()` vor jedem `ChatAsync`-Aufruf einfügt. Wird vom `ServerChatRunner` bei aktivem Rate-Limiting automatisch verwendet.
- `OllamaProvider`: Retry bei unvollständigem Stream (max. 3 Versuche). Bei HTTP 500 von Reasoning-Modellen (Denktext vor Tool-Call-JSON) versucht `TryRecoverToolCall()` den Tool-Call aus der Fehlermeldung zu rekonstruieren.
- `CerebrasProvider`: 429-Retry mit `Retry-After`-Header (exponentiell: 2s, 4s, 8s, max 10s). Bei HTTP 422 mit "wrong_api_format" wird einmalig ohne `tool_choice` wiederholt.

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
GET    /api/settings          → Aktuelle Server-Einstellungen
PUT    /api/settings          → Einstellungen ändern
POST   /api/settings/test     → Provider-Verbindung testen
POST /api/chat                → { prompt, sessionId?, agentId?, enabledTools?, verbose? } → { response, commands, usedToolCalls, finalStatus, logs, shellContext, usage }
POST /api/chat/stream         → Server-Sent Events (SSE), Token-Streaming
POST /api/chat/cancel         → Laufenden Chat-Request abbrechen
GET  /api/sessions            → Alle Sessions (Metadaten)
GET  /api/sessions/<id>       → Einzelne Session mit Messages
POST /api/sessions            → Neue Session anlegen
DELETE /api/sessions/<id>     → Session löschen
POST /api/sessions/clear      → Alle Sessions löschen
GET  /api/agents              → [{ id, name }] — alle registrierten Agenten
GET  /api/agents/<id>/info-panel → { markdown } — Agent-Beschreibung für UI-Panel
PUT  /api/sessions/<id>       → Session aktualisieren (Title, Messages, etc.)
GET  /api/tools               → [{ name, description, parameters[] }] — alle verfügbaren Tools
GET  /api/history             → (veraltet) Legacy-History
POST /api/reset               → (veraltet) Legacy-History löschen
```

**Tool-Auflösung bei `POST /api/chat`:** `agent.EnabledTools` > `session.EnabledTools` > `body.enabledTools` (erste nicht-leere Liste gewinnt). `ToolHelper.Resolve()` übersetzt Tool-Namen in `ToolDefinition`-Objekte für das LLM.

**`AgentBase.SystemPrompt`** ist `IReadOnlyList<string>` — mehrere System-Prompts möglich, jeder wird als separate System-Nachricht gesendet. Beim Dev-Agent werden die letzten zwei Einträge dynamisch generiert (Projektkontext + geladene Dateien).

### Session-Persistenz

Sessions werden in einem Zwei-Schichten-Layout gespeichert:
- `~/.config/bashgpt/sessions/index.json` – Metadaten aller Sessions
- `~/.config/bashgpt/sessions/<id>/content.json` – Nachrichten einer Session
- `~/.config/bashgpt/sessions/<id>/requests/` – Rohe LLM-Requests/Responses (Debug)

`SessionStore` ist thread-safe via `SemaphoreSlim` und schreibt atomar via Temp-Datei. Maximum: 20 Sessions. Session-Titel wird automatisch aus der ersten User-Message gesetzt (max. 40 Zeichen). Migration alter `history.json` / `sessions.json` erfolgt einmalig beim ersten Start.

### Konfiguration

Gespeichert in `~/.config/bashgpt/config.json`. Relevante `config set`-Schlüssel:

| Schlüssel | Typ | Default | Beschreibung |
|---|---|---|---|
| `defaultProvider` | string | `ollama` | Aktiver Provider |
| `defaultExecMode` | string | `ask` | Standard-ExecutionMode |
| `defaultForceTools` | bool | `false` | Tool-Calls immer erzwingen |
| `commandTimeoutSeconds` | int | `300` | Shell-Befehl-Timeout |
| `maxToolCallRounds` | int | `8` | Max. Tool-Call-Runden pro Anfrage |
| `loopDetectionEnabled` | bool | `true` | Schleifenerkennung bei identischen Tool-Calls |
| `ollama.baseUrl` | string | `http://localhost:11434` | Ollama-URL |
| `ollama.model` | string | `gpt-oss:20b` | Ollama-Modell |
| `cerebras.apiKey` | string | — | Cerebras API-Key |
| `cerebras.model` | string | `gpt-oss:120b-cloud` | Cerebras-Modell |
| `cerebras.baseUrl` | string | `https://api.cerebras.ai/v1` | Cerebras API-URL |

`rateLimiting.*`-Felder (`enabled`, `maxRequestsPerMinute` (30), `agentRequestDelayMs` (500)) werden von `config set` **nicht** unterstützt — erreichbar über `PUT /api/settings` im Browser oder direkt in `~/.config/bashgpt/config.json`.

Env-Variablen überschreiben die Datei:

| Variable | Entspricht |
|---|---|
| `BASHGPT_PROVIDER` | `defaultProvider` |
| `BASHGPT_CEREBRAS_KEY` | `cerebras.apiKey` |
| `BASHGPT_CEREBRAS_MODEL` | `cerebras.model` |
| `BASHGPT_OLLAMA_URL` | `ollama.baseUrl` |
| `BASHGPT_OLLAMA_MODEL` | `ollama.model` |
| `BASHGPT_EXEC_MODE` | `defaultExecMode` |
| `BASHGPT_FORCE_TOOLS` | `defaultForceTools` |
| `BASHGPT_COMMAND_TIMEOUT` | `commandTimeoutSeconds` |
| `BASHGPT_LOOP_DETECTION` | `loopDetectionEnabled` |
| `BASHGPT_MAX_TOOL_CALL_ROUNDS` | `maxToolCallRounds` |

### Frontend

TypeScript + Lit Web Components, gebaut mit Vite als Single Bundle. Komponenten: `chat-app` (Router), `chat-view`, `dashboard`, `sidebar`, `settings-view`, `agents-view`, `tools-view`, `message-bubble`, `tool-calls-panel`, `chat-info-panel`.

## Coding Style

- C# mit Nullable Reference Types und Implicit Usings
- 4 Spaces, file-scoped Namespaces
- `PascalCase` für Types/Members, `camelCase` für Locals
- Tests: xUnit, Naming `Method_Condition_Result` (z.B. `StreamAsync_StopsAtDone`)
- Commits: Conventional Messages (`feat:`, `fix:`, `test:` etc.), oft auf Deutsch
