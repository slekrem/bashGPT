# bashGPT

KI-gestützter Shell-Assistent für die Kommandozeile. bashGPT sammelt optional Kontext (Verzeichnis, Git-Status) und fragt ein LLM über Ollama an. Gefundene Shell-Befehle können bestätigt, trocken getestet oder automatisch ausgeführt werden.

## Features
- CLI mit `System.CommandLine`
- Provider: Ollama
- Optionaler Shell-Kontext (Verzeichnis, OS, Shell, Git)
- Sicherheitsabfrage für gefährliche Befehle
- Streaming-Antworten
- **Browser-UI**: eingebetteter HTTP-Server mit Chat-Verlauf, Session-Verwaltung und Agent-Auswahl
- **Agenten**: spezialisierte Chat-Modi (Shell-Agent, Dev-Agent) mit dedizierten Tool-Sets
- **Tools-Ökosystem**: modulare LLM-Tools für Filesystem, Git, Build, Tests, Web-Fetch und Shell
- Lizenz: [MIT](LICENSE)

## Installation
Voraussetzungen für reproduzierbare Release-Checks: **.NET SDK 9.0.301** und **Node.js 20.19.0**.

Für den Frontend-Build sind zusätzlich kompatibel: **Node.js ≥ 20.19.0** oder **≥ 22.12.0** — benötigt von Vite 7.

Weitere lokale Voraussetzungen je nach Nutzung:
- `git` für Git-Tools und Git-Kontext (`git --version`)
- `ollama` für lokale Modellaufrufe im CLI- und Server-Modus (`ollama --version`, danach `ollama serve`)
- `npm` für den Web-Build sowie den `build_run`-/`test_run`-Runner `npm` (`npm --version`)
- `pytest` nur falls du den `test_run`-Runner `pytest` verwenden willst (`pytest --version`)
- Version prüfen: CLI über `dotnet run --project src/bashGPT.Cli -- --version`, Server über `GET /api/version`

```bash
# Repo klonen, dann bauen (npm install + npm run build laufen automatisch)
dotnet build
```

## Schnellstart
```bash
# einfache Anfrage
dotnet run --project src/bashGPT.Cli -- "zeige alle .cs Dateien"

# Provider wählen
dotnet run --project src/bashGPT.Cli -- --provider ollama "liste alle Tests"

# Modell überschreiben
dotnet run --project src/bashGPT.Cli -- --model llama3.2 "zeige geänderte Dateien"

# Tool-Calls explizit erzwingen (optional)
dotnet run --project src/bashGPT.Cli -- --force-tools "analysiere dieses Verzeichnis"
```

## Konfiguration
Standardmäßig liegt die Konfiguration unter `~/.config/bashgpt/config.json`.

```bash
# Konfiguration anzeigen
dotnet run --project src/bashGPT.Cli -- config list

# Provider setzen
dotnet run --project src/bashGPT.Cli -- config set defaultProvider ollama

# Ollama-URL und Modell
dotnet run --project src/bashGPT.Cli -- config set ollama.baseUrl http://localhost:11434
dotnet run --project src/bashGPT.Cli -- config set ollama.model llama3.2
```

Alternativ per Environment:
- `BASHGPT_PROVIDER`
- `BASHGPT_OLLAMA_URL`, `BASHGPT_OLLAMA_MODEL`

## Ausführungsmodi
```bash
# nur anzeigen, nicht ausführen
dotnet run --project src/bashGPT.Cli -- --dry-run "lösche alle tmp Dateien"

# keine Ausführung (reiner Chat)
dotnet run --project src/bashGPT.Cli -- --no-exec "wie finde ich große Dateien?"

# ohne Bestätigung ausführen
dotnet run --project src/bashGPT.Cli -- --auto-exec "git status"
```

## Kontext-Steuerung
```bash
# keinen Kontext mitschicken
dotnet run --project src/bashGPT.Cli -- --no-context "wie erstelle ich einen neuen branch?"

# Verzeichnisinhalt mitsenden
dotnet run --project src/bashGPT.Cli -- --include-dir "zeige wichtige Dateien"
```

## Server-Modus (Browser UI)
```bash
# startet lokalen Server auf http://127.0.0.1:5050 und öffnet den Browser
dotnet run --project src/bashGPT.Server

# eigener Port, ohne automatisches Browser-Öffnen
dotnet run --project src/bashGPT.Server -- --port 6060 --no-browser

# Server mit Modell-Vorgabe
dotnet run --project src/bashGPT.Server -- --provider ollama --model llama3.2 --verbose
```

Verfügbare Server-Flags: `--provider`, `--model`, `--port`, `--no-browser`, `--verbose`.

Hinweise:
- Die UI bietet Chat-Verlauf, Session-Verwaltung, Agent-Auswahl, manuell wählbare Tools und Anzeige ausgeführter Tool-Ergebnisse.
- Im Server-Modus wird der Exec-Mode vom Backend nicht ausgewertet — das Verhalten der Tools ist fest definiert (kein interaktives Terminal-Prompt möglich).
- Sessions werden unter `~/.config/bashgpt/sessions/` gespeichert (max. 20 Sessions).
- Verfügbare API-Endpunkte umfassen u. a. `/api/sessions/*`, `/api/agents`, `/api/agents/<id>/info-panel`, `/api/tools`, `/api/chat/stream` und `/api/chat/cancel`.
- Manuell auswählbare Tools in der Browser-UI sind standardmäßig auf eine Safe-Default-Liste begrenzt: `fetch`, `filesystem_read`, `filesystem_search`, `git_status`, `git_diff`, `git_log`, `git_branch`.
- Zusätzliche, riskantere Tools können explizit über `BASHGPT_SERVER_ALLOWED_TOOLS` freigegeben werden, z. B. `BASHGPT_SERVER_ALLOWED_TOOLS=shell_exec,filesystem_write`.
- Agenten mit fest definierten Tool-Sets (z. B. `shell`, `dev`) bleiben eine bewusste Vertrauensgrenze und können weiter mächtigere Tools verwenden.

## Agenten (Browser UI)
Agenten sind spezialisierte Chat-Modi mit eigenem System-Prompt und Tool-Set. Sie werden in der Browser-UI ausgewählt.

**Generic-Agent** (`generic`): Standard-Chat-Agent ohne agentenspezifischen System-Prompt. Keine Tools standardmäßig aktiv — Tools können manuell über den Tool-Picker in der UI ausgewählt werden.

**Shell-Agent** (`shell`): Interaktiver Shell-Assistent mit `shell_exec`-Tool. Führt Befehle direkt aus.

**Dev-Agent** (`dev`): Entwicklungsagent mit vollständigem Zugriff auf Filesystem, Git, Build, Tests und Shell. Lädt Quelldateien gezielt in den Kontext (`context_load_files`, `context_unload_files`, `context_clear_files`).

## Tests
```bash
dotnet test
# mit Coverage
dotnet test --collect:"XPlat Code Coverage"
# vollständiger HTML-Report (inkl. vorherigem Cleanup)
./scripts/coverage-report.sh
```

Für einen reproduzierbaren Launch-Check auf sauberem Checkout:
- `dotnet build -m:1 /nodeReuse:false`
- `dotnet test -m:1 /nodeReuse:false`
- optional zusätzlich in `src/bashGPT.Web`: `npm test`

Go für einen Public-Release ist erreicht, wenn:
- Build und Tests ohne manuelle Nacharbeit grün sind
- README und öffentliche Repo-Metadaten den tatsächlichen Stand widerspiegeln
- Community-Dateien (`LICENSE`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`) vorhanden sind

Empfohlener erster Public-Release:
- Tag `v0.1.0`
- Release Notes mit Kurzüberblick, Setup-Anforderungen und bekannten Einschränkungen
- vor Public-Schaltung GitHub-Beschreibung, Topics, Homepage-Entscheidung und Sichtbarkeit bewusst prüfen
- empfohlene GitHub-Beschreibung: `Local AI assistant for shell workflows with a .NET CLI, browser UI, agents, tools, and Ollama integration.`
- empfohlene Topics: `dotnet`, `cli`, `ollama`, `agent`, `tool-calling`
- den Visibility-Switch von `Private` auf `Public` erst als letzten Maintainer-Schritt nach erfolgreichem Launch-Check durchführen

## Beispiele (Output-Format)
bashGPT erwartet, dass Shell-Befehle in Code-Blöcken stehen. Beispielantwort:
```bash
ls -la
```

## Lizenz
MIT. Details in [LICENSE](LICENSE).

## Community
- Contribution Guide: [CONTRIBUTING.md](CONTRIBUTING.md)
- Code of Conduct: [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)
- Security Policy: [SECURITY.md](SECURITY.md)
