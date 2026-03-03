# Testrunbook: Server-UI v2 (Issue #21)

## Überblick

Die Server-UI-Tests sind als **Integration-Tests** im Projekt `tests/bashGPT.Tests`
unter `Server/` implementiert. Sie starten den echten `ServerHost`-HTTP-Listener auf
einem zufälligen Port und testen alle API-Endpunkte ohne echte LLM-Verbindung, indem
ein `FakePromptHandler`-Stub injiziert wird.

---

## Lokaler Testlauf

### Voraussetzungen

- .NET SDK 9.0
- Node.js 20+ (für Frontend-Build, der dem .NET-Build vorgelagert ist)

### Alle Tests ausführen

```bash
# im Root-Verzeichnis des Repos
dotnet test
```

### Nur Server-UI-Tests ausführen

```bash
dotnet test --filter "FullyQualifiedName~ServerHost"
```

### Mit ausführlicher Ausgabe

```bash
dotnet test --filter "FullyQualifiedName~ServerHost" --logger "console;verbosity=normal"
```

---

## Testfälle

### GET /

| Test | Erwartetes Ergebnis |
|------|---------------------|
| `Get_Root_Returns200WithHtml` | Status 200, Content-Type `text/html` |

### GET /bundle.js

| Test | Erwartetes Ergebnis |
|------|---------------------|
| `Get_BundleJs_Returns200WithJavaScript` | Status 200, Content-Type `application/javascript` |

### GET /api/history

| Test | Erwartetes Ergebnis |
|------|---------------------|
| `Get_History_InitiallyEmpty` | Status 200, leeres `history`-Array |
| `Get_History_AfterChat_ContainsMessages` | Status 200, 2 Einträge (user + assistant) |

### POST /api/reset

| Test | Erwartetes Ergebnis |
|------|---------------------|
| `Post_Reset_ClearsHistory` | Status 200, History danach leer |

### POST /api/chat

| Test | Erwartetes Ergebnis |
|------|---------------------|
| `Post_Chat_ValidPrompt_Returns200WithResponse` | Status 200, Antwort im JSON |
| `Post_Chat_EmptyPrompt_Returns400` | Status 400, `error`-Feld im JSON |
| `Post_Chat_MissingPrompt_Returns400` | Status 400 |
| `Post_Chat_PassesExecModeToHandler` | `auto-exec` wird korrekt weitergereicht |
| `Post_Chat_UnknownExecMode_FallsBackToServerDefault` | Unbekannter Mode → Server-Default |
| `Post_Chat_AllExecModes_AreParsedCorrectly` | ask / dry-run / auto-exec / no-exec korrekt geparst |
| `Post_Chat_WithCommands_ReturnsCommandsInResponse` | Commands + `usedToolCalls` im Response |
| `Post_Chat_HandlerThrows_Returns500` | Status 500 mit `error`-Feld (Fehler-Szenario) |

### Fehler-Route

| Test | Erwartetes Ergebnis |
|------|---------------------|
| `Get_UnknownRoute_Returns404` | Status 404 |

---

## CI-Integration

Der Workflow `.github/workflows/ci.yml` läuft bei jedem Push auf `main` und bei
jedem Pull Request gegen `main`. Er führt folgende Schritte aus:

1. Frontend bauen (`npm ci && npm run build`)
2. .NET Solution bauen (`dotnet build --configuration Release`)
3. Alle Tests ausführen (`dotnet test`)
4. TRX-Testergebnis-Artefakt hochladen

**Neue UI-Merges nach `main` sind automatisch an grünen Regressionschecks gekoppelt.**

---

## Architektur der Tests

```
tests/bashGPT.Tests/
└── Server/
    ├── FakePromptHandler.cs   # Stub: konfigurierbare Dummy-Antworten ohne LLM
    └── ServerHostTests.cs     # Integration-Tests: echter HTTP-Listener, zufälliger Port
```

### FakePromptHandler

- Implementiert `IPromptHandler`
- Felder `NextResult`, `NextException` sind pro Test konfigurierbar
- `LastOptions` und `CallCount` erlauben Assertions auf den übergebenen Optionen

### Testlebenszyklus (IAsyncLifetime)

1. **InitializeAsync**: Freien Port ermitteln → `ServerHost.RunAsync` starten →
   Warten bis Server bereit
2. **Test**: HTTP-Anfragen über `HttpClient` an `http://127.0.0.1:{port}`
3. **DisposeAsync**: CancellationToken canceln → Probe-Anfrage senden (entsperrt
   `GetContextAsync`) → auf Server-Aufgabe warten (max. 5 s)

---

## Manuelle Regressionsprüfung

Bei größeren Änderungen an der UI oder dem Server-Code kann zusätzlich eine manuelle
Prüfung sinnvoll sein:

```bash
# Server starten
dotnet run --project src/bashGPT.Server -- --no-browser

# Im Browser öffnen
open http://localhost:5050

# Manuell prüfen:
# 1. v2-UI aktivieren (localStorage: bashgpt_ui_v2=true, Seite neu laden)
# 2. Dashboard öffnen → Use-Case per Klick ausführen
# 3. Chat öffnen → Nachricht senden (no-exec), Antwort erscheint
# 4. Exec-Mode wechseln (ask/dry-run/auto-exec/no-exec), Nachricht senden
# 5. Terminal-Panel ein-/ausblenden
# 6. Session neu starten (Neuer Chat)
# 7. Einstellungen öffnen → Provider/Modell ändern
# 8. Zur alten UI wechseln (v1), zurück zu v2
```
