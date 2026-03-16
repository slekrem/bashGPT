# bashGPT Server – API- und State-Contract v2

> Contract zwischen Server-Backend (`ServerHost.cs`) und Browser-Frontend (Lit/TypeScript).
> Alle bestehenden Endpunkte bleiben abwärtskompatibel. Neue Endpunkte werden mit `[NEU]` markiert.

---

## 1. Allgemeines

### Basis-URL
```
http://127.0.0.1:{port}/api/
```
Standard-Port: `5050`. Konfigurierbar via `--port`.

### Datenformat
- Alle Request- und Response-Bodies: **JSON** (`Content-Type: application/json; charset=utf-8`)
- Encoding: **UTF-8**
- Datumsformat: **ISO 8601** (`2026-02-27T10:00:00Z`)

### Fehlerformat (einheitlich für alle Endpunkte)
```json
{
  "error": "Beschreibung des Fehlers",
  "code":  "ERROR_CODE"
}
```

| HTTP-Status | Bedeutung |
|-------------|-----------|
| `200` | Erfolg |
| `400` | Ungültige Anfrage (fehlende/falsche Felder) |
| `404` | Ressource nicht gefunden |
| `500` | Interner Serverfehler |
| `503` | Provider nicht erreichbar (Timeout, kein API-Key) |

### Fehler-Codes

| Code | Beschreibung |
|------|-------------|
| `PROMPT_MISSING` | Pflichtfeld `prompt` fehlt oder leer |
| `SESSION_NOT_FOUND` | Session-ID existiert nicht |
| `PROVIDER_UNAVAILABLE` | LLM-Provider nicht erreichbar |
| `PROVIDER_AUTH_FAILED` | API-Key ungültig oder abgelaufen |
| `SETTINGS_INVALID` | Ungültige Einstellungs-Werte |
| `UNKNOWN` | Nicht klassifizierter Fehler |

---

## 2. Datenmodelle

### 2.1 `Message`

```typescript
interface Message {
  role:    "user" | "assistant";   // Pflicht
  content: string;                  // Pflicht, kann Markdown enthalten
}
```

### 2.2 `CommandResult`

```typescript
interface CommandResult {
  command:     string;   // Pflicht – ausgeführter Bash-Befehl
  exitCode:    number;   // Pflicht – 0 = Erfolg, sonst Fehler; -1 = nicht ausgeführt
  output:      string;   // Pflicht – stdout/stderr (leer wenn nicht ausgeführt)
  wasExecuted: boolean;  // Pflicht – false bei skip/dry-run/ask-abgelehnt
}
```

**Mapping ExecMode → wasExecuted:**

| ExecMode | Befehl genehmigt | wasExecuted | exitCode |
|----------|-----------------|-------------|----------|
| `auto-exec` | immer | `true` | tatsächlicher Exit-Code |
| `ask` | ja (Nutzer bestätigt) | `true` | tatsächlicher Exit-Code |
| `ask` | nein (Nutzer lehnt ab) | `false` | `-1` |
| `dry-run` | nie | `false` | `-1` |
| `no-exec` | nie | `false` | `-1` |

### 2.3 `ChatResponse`

```typescript
interface ChatResponse {
  response:      string;          // Pflicht – Markdown-Text der Antwort
  commands:      CommandResult[]; // Pflicht – leer wenn keine Befehle
  usedToolCalls: boolean;         // Pflicht – true wenn LLM Tool-Calling nutzte
  logs:          string[];        // Pflicht – interne Debug-Logs (leer bei Verbose=false)
}
```

### 2.4 `Session` `[NEU]`

```typescript
interface Session {
  id:        string;   // Pflicht – UUID v4
  title:     string;   // Pflicht – erster Prompt (max. 60 Zeichen, abgeschnitten mit "…")
  createdAt: string;   // Pflicht – ISO 8601
  updatedAt: string;   // Pflicht – ISO 8601, letztes Chat-Ereignis
}
```

### 2.5 `Settings` `[NEU]`

```typescript
interface Settings {
  provider:              "ollama";                              // Pflicht
  model:                 string;                                // Pflicht
  contextWindowTokens?:  number;                                // Optional – aktuell null
  ollamaHost?:           string;                                // Optional – Default: "http://localhost:11434"
  execMode:              "ask" | "dry-run" | "auto-exec" | "no-exec";
  forceTools:            boolean;
  commandTimeoutSeconds: number;
  loopDetectionEnabled:  boolean;
  maxToolCallRounds:     number;
  rateLimiting: {
    enabled: boolean;
    maxRequestsPerMinute: number;
    agentRequestDelayMs: number;
  };
  ollama: {
    model: string;
    host: string;
  };
}
```

### 2.6 `ExecMode`

```typescript
type ExecMode = "ask" | "dry-run" | "auto-exec" | "no-exec";
```

---

## 3. Endpunkte

### 3.1 Statische Assets

#### `GET /`
Liefert `index.html` (Lit-App Shell).

#### `GET /bundle.js`
Liefert das kompilierte TypeScript-Bundle.

---

### 3.2 Chat

#### `POST /api/chat`

**Bestehendes Verhalten bleibt erhalten.** Neu: optionales `sessionId`-Feld.

**Request:**
```json
{
  "prompt":    "Zeige alle laufenden Prozesse",
  "execMode":  "ask",
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "verbose":   false
}
```

| Feld | Typ | Pflicht | Beschreibung |
|------|-----|---------|-------------|
| `prompt` | `string` | ✅ | Nutzer-Eingabe |
| `execMode` | `ExecMode` | ❌ | Überschreibt Server-Default |
| `sessionId` | `string` | ❌ | `[NEU]` Session zuordnen; ohne = aktuelle globale Session |
| `verbose` | `boolean` | ❌ | Debug-Logs zurückgeben |

**Response `200`:**
```json
{
  "response": "Hier sind alle laufenden Prozesse:\n\n```\nPID   COMMAND\n1234  bash\n```",
  "commands": [
    {
      "command":     "ps aux",
      "exitCode":    0,
      "output":      "USER  PID  ...\nroot    1  ...",
      "wasExecuted": true
    }
  ],
  "usedToolCalls": true,
  "logs": []
}
```

**Response `400`:**
```json
{ "error": "Prompt fehlt.", "code": "PROMPT_MISSING" }
```

**Response `503`:**
```json
{ "error": "Provider nicht erreichbar: Connection refused", "code": "PROVIDER_UNAVAILABLE" }
```

---

### 3.3 History (bestehend)

#### `GET /api/history`

Gibt die Nachrichten der aktuellen (globalen) Session zurück.

**Response `200`:**
```json
{
  "history": [
    { "role": "user",      "content": "Zeige alle Prozesse" },
    { "role": "assistant", "content": "Hier sind alle Prozesse:\n\n..." }
  ]
}
```

#### `POST /api/reset`

Löscht die aktuelle (globale) Session-History.

**Response `200`:**
```json
{ "ok": true }
```

---

### 3.4 Sessions `[NEU]`

#### `GET /api/sessions`

Gibt alle gespeicherten Sessions zurück, neueste zuerst.

**Response `200`:**
```json
{
  "sessions": [
    {
      "id":        "550e8400-e29b-41d4-a716-446655440000",
      "title":     "Zeige alle laufenden Prozesse",
      "createdAt": "2026-02-27T10:00:00Z",
      "updatedAt": "2026-02-27T10:05:00Z"
    }
  ]
}
```

---

#### `POST /api/sessions`

Erstellt eine neue leere Session.

**Response `200`:**
```json
{
  "id":        "661f9511-f30c-52e5-b827-557766551111",
  "title":     "Neue Session",
  "createdAt": "2026-02-27T11:00:00Z",
  "updatedAt": "2026-02-27T11:00:00Z"
}
```

---

#### `GET /api/sessions/:id/history`

Gibt den Chatverlauf einer bestimmten Session zurück.

**Response `200`:**
```json
{
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "history": [
    { "role": "user",      "content": "Zeige alle Prozesse" },
    { "role": "assistant", "content": "..." }
  ]
}
```

**Response `404`:**
```json
{ "error": "Session nicht gefunden.", "code": "SESSION_NOT_FOUND" }
```

---

#### `DELETE /api/sessions/:id`

Löscht eine Session inkl. ihres Verlaufs.

**Response `200`:**
```json
{ "ok": true }
```

**Response `404`:**
```json
{ "error": "Session nicht gefunden.", "code": "SESSION_NOT_FOUND" }
```

---

### 3.5 Settings `[NEU]`

#### `GET /api/settings`

Liest die aktuellen Einstellungen.

**Response `200`:**
```json
{
  "provider": "ollama",
  "model": "gpt-oss:20b",
  "contextWindowTokens": null,
  "ollamaHost": "http://localhost:11434",
  "execMode": "ask",
  "forceTools": false,
  "commandTimeoutSeconds": 300,
  "loopDetectionEnabled": true,
  "maxToolCallRounds": 8,
  "rateLimiting": {
    "enabled": true,
    "maxRequestsPerMinute": 30,
    "agentRequestDelayMs": 500
  },
  "ollama": {
    "model": "gpt-oss:20b",
    "host": "http://localhost:11434"
  }
}
```

---

#### `PUT /api/settings`

Speichert Einstellungen in `~/.config/bashgpt/config.json`.

**Request:**
```json
{
  "provider":   "ollama",
  "model":      "gpt-oss:20b",
  "ollamaHost": "http://localhost:11434",
  "execMode":   "ask",
  "forceTools": false,
  "commandTimeoutSeconds": 300,
  "loopDetectionEnabled": true,
  "maxToolCallRounds": 8,
  "rateLimiting": {
    "enabled": true,
    "maxRequestsPerMinute": 30,
    "agentRequestDelayMs": 500
  },
  "ollama": {
    "model": "gpt-oss:20b",
    "host": "http://localhost:11434"
  }
}
```

**Response `200`:**
```json
{ "ok": true }
```

**Response `400`:**
```json
{ "error": "Ungültiger execMode.", "code": "SETTINGS_INVALID" }
```

---

#### `POST /api/settings/test`

Testet die Verbindung zum konfigurierten Provider mit einem minimalen API-Call.

**Response `200`:**
```json
{ "ok": true, "latencyMs": 312 }
```

**Response `503`:**
```json
{
  "ok":    false,
  "error": "Verbindung fehlgeschlagen: 401 Unauthorized",
  "code":  "PROVIDER_AUTH_FAILED"
}
```

---

## 4. Streaming `[Spezifikation – noch nicht implementiert]`

> Für Echtzeit-Streaming der LLM-Antwort. Implementierung in einem späteren Issue.

**Endpunkt:** `GET /api/chat/stream` (SSE)

**Event-Typen:**

```
event: token
data: {"token": "Hier "}

event: token
data: {"token": "sind "}

event: tool_call
data: {"command": "ps aux", "status": "running"}

event: tool_result
data: {"command": "ps aux", "exitCode": 0, "output": "USER PID ...", "wasExecuted": true}

event: done
data: {"usedToolCalls": true, "logs": []}

event: error
data: {"error": "Provider nicht erreichbar", "code": "PROVIDER_UNAVAILABLE"}
```

**Verhalten:**
- `token`: Einzelnes Text-Fragment, Frontend hängt es an die aktuelle Nachricht an
- `tool_call`: Befehl wird gerade ausgeführt → Terminal-Eintrag anlegen
- `tool_result`: Befehl fertig → Terminal-Eintrag mit Output aktualisieren
- `done`: Stream abgeschlossen → Eingabe wieder freigeben
- `error`: Stream abgebrochen → Fehlermeldung anzeigen, Eingabe freigeben

---

## 5. Frontend-State-Modell

### 5.1 App-State (global)

```typescript
interface AppState {
  view:            "dashboard" | "chat" | "settings";
  sessions:        Session[];
  activeSessionId: string | null;
  settings:        Settings | null;
}
```

### 5.2 Chat-State (pro Session)

```typescript
interface ChatState {
  messages:    UiMessage[];
  status:      "idle" | "loading" | "tool_running" | "error";
  statusText:  string;           // z.B. "Denke…" / "Führt Befehl aus…"
  execMode:    ExecMode;
  terminal:    TerminalEntry[];
}

interface UiMessage {
  id:            number;         // lokaler Zähler
  role:          "user" | "assistant";
  content:       string;
  commands:      CommandResult[];
  usedToolCalls: boolean;
}

interface TerminalEntry {
  command:     string;
  output:      string;
  exitCode:    number;
  wasExecuted: boolean;
  status:      "running" | "success" | "error" | "skipped";
}
```

### 5.3 State-Übergänge

```
idle
 ├─→ [Senden geklickt] → loading
 │
loading
 ├─→ [Tool-Call empfangen]  → tool_running
 ├─→ [Antwort erhalten]     → idle
 └─→ [Fehler]               → error

tool_running
 ├─→ [Tool fertig, mehr Tool-Calls] → tool_running
 ├─→ [Tool fertig, Antwort fertig]  → idle
 └─→ [Fehler]                       → error

error
 └─→ [Neue Eingabe]  → loading
```

---

## 6. Persistenz-Verhalten

### Bestehend (unverändert)
- Globale In-Memory-History in `ServerHost._history`
- Max. 40 Nachrichten, FIFO-Rotation
- `POST /api/reset` löscht alles

### Neu (Issue #17-Implementierung)
- Sessions werden serverseitig in `~/.config/bashgpt/sessions/` als JSON-Dateien gespeichert
- Dateiname: `{iso-date}-{uuid}.json`
- Jede Datei enthält: Session-Metadaten + vollständige Message-History
- Kein automatisches Ablaufen; `DELETE /api/sessions/:id` löscht explizit

### Kompatibilitätsregeln
- `GET /api/history` und `POST /api/reset` bleiben unverändert (globale Session)
- Neue Session-Endpunkte sind additiv – bestehende Clients funktionieren weiterhin
- `sessionId` in `POST /api/chat` ist optional – fehlt es, wird die globale In-Memory-History verwendet

---

## 7. TypeScript-Typen (Frontend-Referenz)

```typescript
// src/types.ts

export type ExecMode = "ask" | "dry-run" | "auto-exec" | "no-exec";

export interface CommandResult {
  command:     string;
  exitCode:    number;
  output:      string;
  wasExecuted: boolean;
}

export interface ChatResponse {
  response:      string;
  commands:      CommandResult[];
  usedToolCalls: boolean;
  logs:          string[];
}

export interface HistoryMessage {
  role:    "user" | "assistant";
  content: string;
}

export interface Session {
  id:        string;
  title:     string;
  createdAt: string;
  updatedAt: string;
}

export interface Settings {
  provider:     string;
  model:        string;
  apiKey?:      string;
  ollamaHost?:  string;
  execMode:     ExecMode;
  forceTools:   boolean;
}

export type AppView = "dashboard" | "chat" | "settings";

export type ChatStatus = "idle" | "loading" | "tool_running" | "error";

export interface TerminalEntry {
  command:     string;
  output:      string;
  exitCode:    number;
  wasExecuted: boolean;
  status:      "running" | "success" | "error" | "skipped";
}
```

---

## 8. Offene Punkte

- [ ] Streaming-Implementierung (SSE) — separates Issue nach #17
- [ ] Session-Persistenz: Datei-basiert (beschrieben) oder SQLite?
- [ ] API-Key im Response: maskieren reicht, oder komplett weglassen?
- [ ] `DELETE /api/sessions/:id` — aktive Session: Fallback auf Dashboard oder Fehler?

---

*Erstellt für Issue [#20](https://github.com/slekrem/bashGPT/issues/20) · Eltern-Issue [#15](https://github.com/slekrem/bashGPT/issues/15)*
