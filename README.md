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
 dotnet run --project src/bashGPT -- "zeige alle .cs Dateien"

# Provider wählen
 dotnet run --project src/bashGPT -- --provider ollama "liste alle Tests"

# Modell überschreiben
 dotnet run --project src/bashGPT -- --model llama3.2 "zeige geänderte Dateien"
```

## Konfiguration
Standardmäßig liegt die Konfiguration unter `~/.config/bashgpt/config.json`.

```bash
# Konfiguration anzeigen
 dotnet run --project src/bashGPT -- config list

# Provider setzen
 dotnet run --project src/bashGPT -- config set defaultProvider ollama

# Ollama-URL und Modell
 dotnet run --project src/bashGPT -- config set ollama.baseUrl http://localhost:11434
 dotnet run --project src/bashGPT -- config set ollama.model llama3.2

# Cerebras-Key und Modell
 dotnet run --project src/bashGPT -- config set cerebras.apiKey <key>
 dotnet run --project src/bashGPT -- config set cerebras.model gpt-oss:120b-cloud
```

Alternativ per Environment:
- `BASHGPT_PROVIDER`
- `BASHGPT_CEREBRAS_KEY`, `BASHGPT_CEREBRAS_MODEL`
- `BASHGPT_OLLAMA_URL`, `BASHGPT_OLLAMA_MODEL`

## Ausführungsmodi
```bash
# nur anzeigen, nicht ausführen
 dotnet run --project src/bashGPT -- --dry-run "lösche alle tmp Dateien"

# keine Ausführung (reiner Chat)
 dotnet run --project src/bashGPT -- --no-exec "wie finde ich große Dateien?"

# ohne Bestätigung ausführen
 dotnet run --project src/bashGPT -- --auto-exec "git status"
```

## Kontext-Steuerung
```bash
# keinen Kontext mitschicken
 dotnet run --project src/bashGPT -- --no-context "wie erstelle ich einen neuen branch?"

# Verzeichnisinhalt mitsenden
 dotnet run --project src/bashGPT -- --include-dir "zeige wichtige Dateien"
```

## Tests
```bash
 dotnet test
# mit Coverage
 dotnet test --collect:"XPlat Code Coverage"
```

## Beispiele (Output-Format)
bashGPT erwartet, dass Shell-Befehle in Code-Blöcken stehen. Beispielantwort:
```bash
ls -la
```

## Lizenz
Noch nicht definiert.
