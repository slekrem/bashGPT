# bashGPT

KI-gestützter Shell-Assistent für die Kommandozeile. bashGPT sammelt optional Kontext (Verzeichnis, Git-Status) und fragt ein LLM (Ollama oder Cerebras) an. Gefundene Shell-Befehle können bestätigt, trocken getestet oder automatisch ausgeführt werden.

## Features
- CLI mit `System.CommandLine`
- Provider: Ollama oder Cerebras (OpenAI-kompatibles Chat-API)
- Optionaler Shell-Kontext (Verzeichnis, OS, Shell, Git)
- Sicherheitsabfrage für gefährliche Befehle
- Streaming-Antworten

## Installation
```bash
# Repo klonen
# (lokal) dann bauen
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

# Cerebras-Key und Modell
 dotnet run --project src/bashGPT.Cli -- config set cerebras.apiKey <key>
 dotnet run --project src/bashGPT.Cli -- config set cerebras.model gpt-oss:120b-cloud
```

Alternativ per Environment:
- `BASHGPT_PROVIDER`
- `BASHGPT_CEREBRAS_KEY`, `BASHGPT_CEREBRAS_MODEL`
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

# Server mit Provider/Exec-Vorgaben
 dotnet run --project src/bashGPT.Server -- --provider cerebras --auto-exec --verbose

# Tool-Calls im Server explizit erzwingen (optional)
 dotnet run --project src/bashGPT.Server -- --provider cerebras --force-tools
```

Hinweis:
- Die UI bietet Chat-Verlauf, Exec-Mode pro Nachricht (`ask`, `dry-run`, `auto-exec`, `no-exec`) und Anzeige ausgeführter Befehle.
- Im Server-Modus wird `ask` intern als `dry-run` behandelt (kein interaktives Terminal-Prompt im Browser-Flow).
- `--force-tools` ist standardmäßig deaktiviert. Ohne Flag darf das Modell selbst entscheiden, ob ein Tool-Call nötig ist.

## Tests
```bash
 dotnet test
# mit Coverage
 dotnet test --collect:"XPlat Code Coverage"
# vollständiger HTML-Report (inkl. vorherigem Cleanup)
 ./scripts/coverage-report.sh
```

## Beispiele (Output-Format)
bashGPT erwartet, dass Shell-Befehle in Code-Blöcken stehen. Beispielantwort:
```bash
ls -la
```

## Lizenz
Noch nicht definiert.
