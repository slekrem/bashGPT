# bashGPT Server UI – Konzept v2

> Inspiriert durch [PCPal](https://github.com/slekrem/pcpal) – adaptiert für Web (Lit + TypeScript) und Bash statt PowerShell.

---

## 1. Informationsarchitektur

### 1.1 Drei Hauptbereiche

```
┌───────────────────────────────────────────────────────────────┐
│  HEADER  ● bashGPT                          [+] Neuer Chat    │
├──────────────┬────────────────────────────────────────────────┤
│              │                                                  │
│   SIDEBAR    │              MAIN PANEL                         │
│   (220px)    │   Dashboard  /  Chat  /  Settings              │
│              │                                                  │
│  [Sessions]  │                                                  │
│  ──────────  │                                                  │
│  Dashboard   │                                                  │
│  Settings    │                                                  │
│              │                                                  │
└──────────────┴────────────────────────────────────────────────┘
```

| Bereich | Zweck | Trigger |
|---------|-------|---------|
| **Dashboard** | Einstieg, Use-Cases, zuletzt genutzte Prompts | Start, Logo-Klick |
| **Chat** | Interaktion mit bashGPT, Tool-Ausgaben | Session wählen, Neuer Chat |
| **Settings** | Provider-Hinweis, Ollama-Verbindung, Verlauf | Settings-Button |

---

### 1.2 Gesamtlayout (Desktop)

```
┌─────────────────────────────────────────────────────────────────────┐
│  ● bashGPT                                           [+] Neuer Chat │
│  ─────────────────────────────────────────────────────────────────  │
├──────────────┬──────────────────────────────────────────────────────┤
│  VERLAUF     │  ┌──────────────────┬──────────────────────────────┐ │
│              │  │  TERMINAL        │  CHAT                        │ │
│  Session A   │  │  (280px)   [──]  │                              │ │
│  Session B   │  │  $ ls -la        │  [Du]  Zeige alle Prozesse   │ │
│  Session C   │  │  total 32        │                              │ │
│  ──────────  │  │  drwxr-xr-x ...  │  [bashGPT]  Hier sind alle  │ │
│  Dashboard   │  │  ──────────────  │  laufenden Prozesse:         │ │
│  Einst.      │  │  $ ps aux        │  `ps aux` liefert ...        │ │
│              │  │  USER  PID  ...  │                              │ │
│              │  │                  │  [Du]  Beende PID 1234       │ │
│              │  │                  ├──────────────────────────────┤ │
│              │  │                  │  ask ▾  Nachricht...  Senden │ │
│              │  └──────────────────┴──────────────────────────────┘ │
└──────────────┴──────────────────────────────────────────────────────┘
```

### 1.3 Mobile Layout (< 768px)

```
┌──────────────────────────┐
│  ● bashGPT       ☰  [+] │
├──────────────────────────┤
│  [Chat]  [Terminal]      │  ← Tab-Umschalter
├──────────────────────────┤
│                          │
│  CHAT VIEW               │
│  (oder Terminal View)    │
│                          │
├──────────────────────────┤
│  ask ▾  Nachricht...  →  │
└──────────────────────────┘
```

- Sidebar: Drawer (von links einblenden via Hamburger-Menü)
- Chat + Terminal: Tabs statt nebeneinander
- Breakpoint: `768px`

---

## 2. Komponenten

### 2.1 Header

```
┌─────────────────────────────────────────────────────────┐
│  ● bashGPT                              [+] Neuer Chat  │
└─────────────────────────────────────────────────────────┘
```

| Element | Beschreibung |
|---------|-------------|
| Logo `● bashGPT` | Klick → Dashboard |
| `[+] Neuer Chat` | Neue Session starten, zu Chat wechseln |

---

### 2.2 Sidebar

```
┌──────────────────────┐
│  VERLAUF             │
│  ┌────────────────┐  │
│  │ Session A      │  │  ← aktiv (hervorgehoben)
│  │ 27. Feb. 2026  │  │
│  └────────────────┘  │
│  ┌────────────────┐  │
│  │ Session B      │  │
│  │ 26. Feb. 2026  │  │
│  └────────────────┘  │
│  ────────────────    │
│  [ Dashboard ]       │
│  [ Einstellungen ]   │
└──────────────────────┘
```

| Element | Aktion |
|---------|--------|
| Session-Karte | Klick → Session laden + Chat-View |
| `Dashboard` | Klick → Dashboard-View |
| `Einstellungen` | Klick → Settings-View |

---

### 2.3 Dashboard-View

```
┌──────────────────────────────────────────────────────────┐
│  Hallo! Ich bin bashGPT.                                  │
│  Was möchtest du heute erledigen?                         │
│                                                           │
│  [ 🔍 Suche Use-Cases...                              ]  │
│                                                           │
│  Zuletzt genutzt                                          │
│  [Prozesse anzeigen] [Git-Status] [Speicher prüfen]      │
│                                                           │
│  ── System ───────────────────────────────────────────   │
│  ┌─────────────────┐  ┌─────────────────┐                │
│  │ Prozesse        │  │ Speicher         │                │
│  │ Alle laufenden  │  │ Disk-Nutzung     │                │
│  │ Prozesse listen │  │ anzeigen         │                │
│  │ ⚠ Mittel        │  │ ✓ Sicher         │                │
│  │ [Ausführen]     │  │ [Ausführen]      │                │
│  │ [Anpassen]      │  │ [Anpassen]       │                │
│  └─────────────────┘  └─────────────────┘                │
│                                                           │
│  ── Git ──────────────────────────────────────────────   │
│  ┌─────────────────┐  ┌─────────────────┐                │
│  │ Status          │  │ Log              │                │
│  │ Aktuellen       │  │ Letzte 10        │                │
│  │ Branch anzeigen │  │ Commits anzeigen │                │
│  │ ✓ Sicher         │  │ ✓ Sicher         │                │
│  │ [Ausführen]     │  │ [Ausführen]      │                │
│  │ [Anpassen]      │  │ [Anpassen]       │                │
│  └─────────────────┘  └─────────────────┘                │
└──────────────────────────────────────────────────────────┘
```

**Risk-Labels:**
- `✓ Sicher` — kein destructiver Befehl erwartet
- `⚠ Mittel` — modifiziert ggf. Dateien/Prozesse
- `🔴 Kritisch` — destruktiv (löscht, beendet, formatiert)

**Vordefinierte Use-Case-Kategorien:**

| Kategorie | Beispiele |
|-----------|----------|
| System | Prozesse, Speicher, CPU, Uptime |
| Git | Status, Log, Diff, Branches |
| Dateien | Verzeichnis listen, Datei finden, Permissions |
| Netzwerk | IP anzeigen, Ports, Ping |
| Wartung | Logs, Temp-Dateien, Updates |

---

### 2.4 Chat-View

```
┌──────────────────────┬─────────────────────────────────────┐
│  TERMINAL            │  CHAT                               │
│                      │                                     │
│  $ ls -la            │                                     │
│  total 32            │  ┌──────────────────────────────┐  │
│  drwxr-xr-x  5 ...   │  │ Du                           │  │
│  -rw-r--r--  1 ...   │  │ Zeige alle Dateien in /tmp   │  │
│  ──────────────────  │  └──────────────────────────────┘  │
│  $ find /tmp -type f │                                     │
│  /tmp/abc.log        │  ┌──────────────────────────────┐  │
│  /tmp/xyz.tmp        │  │ bashGPT  ⚡ Tool-Call         │  │
│                      │  │                              │  │
│                      │  │ In `/tmp` befinden sich      │  │
│                      │  │ folgende Dateien:            │  │
│                      │  │ - `abc.log` (12KB)           │  │
│                      │  │ - `xyz.tmp` (4KB)            │  │
│                      │  └──────────────────────────────┘  │
│                      │                                     │
│                      ├─────────────────────────────────────┤
│                      │  [ask ▾]  Nachricht...    [Senden]  │
└──────────────────────┴─────────────────────────────────────┘
```

**Nachrichten-Typen:**

| Typ | Darstellung |
|-----|------------|
| User | Rechts ausgerichtet, Akzentfarbe |
| Assistant | Links, dunkler Hintergrund, Markdown-Rendering |
| Tool-Call-Badge | `⚡ Tool-Call` Label an Assistant-Bubble |

**Terminal-Panel:**
- Monospace-Font, dunkler Hintergrund (`#020617`)
- Prompt: `$` in Akzentfarbe (`#22c55e`)
- Befehl: weiß
- Output: gedimmt (`#94a3b8`)
- Fehler: rot (`#ef4444`)
- Trennlinie zwischen Befehlen
- Resizable (Splitter, 150–500px, Default 280px)

**Eingabe-Zeile:**
- Exec-Mode-Dropdown: `ask` / `dry-run` / `auto-exec` / `no-exec`
- Textarea (wächst bis max. 5 Zeilen)
- `Senden`-Button (Cmd+Enter)
- Lade-Indikator während Verarbeitung

---

### 2.5 Settings-View

```
┌──────────────────────────────────────────────────────────┐
│  Einstellungen                                            │
│                                                           │
│  ── Verbindung ──────────────────────────────────────    │
│  Provider       [ollama                 ]                 │
│  Modell         [gpt-oss:20b             ]                │
│  Ollama Host    [http://localhost:11434  ]                │
│  [Verbindung testen]                                      │
│                                                           │
│  Hinweis: Open-Source-Builds nutzen ausschließlich       │
│  Ollama; weitere Runtime-Optionen werden nicht           │
│  über diese Seite konfiguriert.                          │
│                                                           │
│  ── Daten ───────────────────────────────────────────    │
│  [Verlauf löschen]                                        │
│                                                           │
│                                          [Speichern]      │
└──────────────────────────────────────────────────────────┘
```

**Gespeichert in:** `~/.config/bashgpt/config.json` (Provider-/Ollama-/UI-relevante Felder)

---

## 3. Design-System

### 3.1 Farbpalette (Dark Mode – Standard)

```
Hintergrund:         #020617   (tiefes Blau-Schwarz)
Panel:               #0f172a   (dunkelblau)
Sidebar:             #0b1120   (noch dunkler)
Border:              #1e293b   (subtile Linie)
Border aktiv:        #374151

Text primär:         #f1f5f9
Text sekundär:       #94a3b8
Text gedimmt:        #475569

Akzent (grün):       #22c55e
Akzent Hover:        #16a34a
Akzent BG:           #14532d

User-Bubble:         #1f2937
Assistant-Bubble:    #0b1220
Terminal-BG:         #020617

Fehler:              #ef4444
Fehler BG:           #7f1d1d
Warnung:             #f59e0b
Warnung BG:          #78350f
Erfolg:              #22c55e
Erfolg BG:           #14532d
```

### 3.2 Farbpalette (Light Mode)

```
Hintergrund:         #f8fafc
Panel:               #ffffff
Sidebar:             #f1f5f9
Border:              #e2e8f0

Text primär:         #0f172a
Text sekundär:       #64748b
Text gedimmt:        #94a3b8

Akzent:              #16a34a
Terminal-BG:         #1e293b  (bleibt dunkel!)
```

### 3.3 Typografie

| Rolle | Font | Größe | Gewicht |
|-------|------|-------|---------|
| UI allgemein | `ui-sans-serif, system-ui` | 14px | 400 |
| Terminal | `ui-monospace, monospace` | 13px | 400 |
| Code inline | `ui-monospace, monospace` | 0.88em | 400 |
| Labels/Badges | — | 11px | 600 |
| Session-Titel | — | 13px | 600 |

### 3.4 Komponenten-Tokens

| Token | Wert |
|-------|------|
| `--radius-sm` | 6px |
| `--radius-md` | 10px |
| `--radius-lg` | 12px |
| `--radius-bubble` | 18px |
| `--transition` | 0.15s ease |
| `--sidebar-width` | 220px |
| `--terminal-width` | 280px |

---

## 4. User-Flows

### Flow 1: Neuer Chat aus Dashboard

```
[User klickt "Ausführen" auf Use-Case-Karte]
        │
        ▼
Prompt wird ins Eingabefeld übernommen
Session wird neu erstellt
View wechselt zu Chat
        │
        ▼
[User drückt Senden / Cmd+Enter]
        │
        ▼
User-Message erscheint im Chat
Loading-Indikator aktiv
Exec-Mode = aktueller Default
        │
        ▼
[API-Call POST /api/chat]
        │
        ├── Tool-Call → Bash-Befehl erscheint im Terminal
        │               Output erscheint im Terminal
        │               Loop weiter
        │
        └── Antwort → Assistant-Message erscheint (Markdown)
                       ⚡ Tool-Call Badge falls Tool verwendet
                       Loading-Indikator weg
```

### Flow 2: Session aus Verlauf laden

```
[User klickt Session in Sidebar]
        │
        ▼
GET /api/history → History laden
View wechselt zu Chat
Nachrichten werden aufgebaut
Terminal zeigt bisherige Befehle dieser Session
        │
        ▼
[User kann weiter chatten]
```

### Flow 3: Einstellungen speichern

```
[User klickt "Einstellungen" in Sidebar]
        │
        ▼
Settings-View öffnet
Felder sind mit aktuellen Werten befüllt
        │
        ▼
[User ändert Provider / Modell / API-Key]
        │
        ▼
[User klickt "Verbindung testen"]
        │
        ├── Erfolg → Grüner Toast "Verbindung OK"
        └── Fehler → Roter Toast mit Fehlermeldung
        │
        ▼
[User klickt "Speichern"]
        │
        ▼
PUT /api/settings → config.json aktualisiert
Toast: "Einstellungen gespeichert"
```

### Flow 4: Ollama-Host ändern

```
[User ändert den Ollama Host]
        │
        ▼
PUT /api/settings → ollamaHost + ollama.host werden aktualisiert
        │
        ▼
[User klickt "Verbindung testen"]
        │
        ▼
POST /api/settings/test
        │
        ▼
Toast: "Verbindung OK" oder "Verbindung fehlgeschlagen"
```

---

## 5. Zustandsmodell

| State | UI-Zustand |
|-------|-----------|
| `idle` | Eingabe aktiv, kein Indikator |
| `loading` | Eingabe gesperrt, Spinner + "Denke…" |
| `tool_running` | Spinner + "Führt Befehl aus…", Terminal aktualisiert sich |
| `error` | Toast-Fehler, letzte Nachricht zeigt ⚠️ |
| `empty` (kein Verlauf) | Dashboard wird angezeigt |
| `empty_session` | Empty-State im Chat: "Stell mir eine Frage…" |

**States pro Terminal-Eintrag:**

| State | Darstellung |
|-------|------------|
| `skipped` | Grau, Badge "übersprungen" |
| `success` (exit 0) | Normal, Badge "exit 0" grün |
| `error` (exit ≠ 0) | Rot, Badge "exit N" rot |

---

## 6. Komponentenliste

| Komponente | Web-Element | Verantwortung |
|-----------|-------------|---------------|
| `<bashgpt-app>` | Root | Layout, Routing, State |
| `<bashgpt-sidebar>` | Sidebar | Session-Liste, Navigation |
| `<bashgpt-dashboard>` | Main | Use-Cases, Suche, Zuletzt genutzt |
| `<bashgpt-chat>` | Main | Chat + Terminal nebeneinander |
| `<bashgpt-terminal>` | Panel | Bash-Befehle + Output darstellen |
| `<bashgpt-message>` | Item | User/Assistant Bubble + Markdown |
| `<bashgpt-settings>` | Main | Provider/Exec/Theme-Konfiguration |
| `<bashgpt-input>` | Footer | Textarea + Exec-Mode + Send |
| `<bashgpt-toast>` | Overlay | Fehler-/Erfolgs-Meldungen |
| `<bashgpt-spinner>` | Inline | Lade-Indikator |

---

## 7. API-Anforderungen (Vorschau für #20)

Neue/erweiterte Endpunkte die für UI v2 benötigt werden:

| Methode | Pfad | Beschreibung |
|---------|------|-------------|
| `GET` | `/api/sessions` | Alle Sessions (ID, Titel, Datum) |
| `POST` | `/api/sessions` | Neue Session erstellen |
| `GET` | `/api/sessions/:id/history` | History einer Session |
| `DELETE` | `/api/sessions/:id` | Session löschen |
| `GET` | `/api/settings` | Aktuelle Einstellungen lesen |
| `PUT` | `/api/settings` | Einstellungen speichern |
| `POST` | `/api/settings/test` | Provider-Verbindung testen |
| `POST` | `/api/chat` | Bestehend (+ `sessionId` Parameter) |
| `POST` | `/api/reset` | Bestehend |

---

## 8. Umsetzungsplan (Iterationen)

### MVP (Issue #17)
- [ ] Sidebar mit Navigation (Dashboard / Chat / Settings)
- [ ] Dashboard mit Use-Case-Karten (statisch, 5–8 Cases)
- [ ] Chat-View mit Terminal-Panel (Splitter, resizable)
- [ ] Settings-View (Provider, Exec-Default, Theme)
- [ ] Dark/Light Mode Toggle

### Ausbau (Issue #18)
- [ ] Session-Persistenz (mehrere Sessions in Sidebar)
- [ ] Terminal-Ausgaben klar strukturiert (Prompt / Output / Error)
- [ ] Exec-Mode pro Nachricht mit Badge-Anzeige
- [ ] Tool-Call-Transparenz (Runden, Badge, Anzahl)

### Polish (Issue #19)
- [ ] Mobile Layout (Drawer, Tabs)
- [ ] Toast-Notifications
- [ ] Keyboard-Shortcuts (Cmd+K für neuen Chat, etc.)
- [ ] Accessibility (ARIA, Fokus-Management)
- [ ] Animationen (Sidebar, View-Transitions)
- [ ] Suche in Dashboard Use-Cases

---

## 9. Offene Fragen

- [ ] Session-Persistenz: clientseitig (localStorage) oder serverseitig (JSON-Dateien)?
- [ ] Feature-Flag `ui_v2`: Soll per CLI-Flag oder config.json steuerbar sein?
- [ ] Streaming-Responses: SSE für Echtzeit-Output oder weiterhin Polling?
- [ ] Terminal-Panel: Immer sichtbar oder nur bei Tool-Calls einblenden?
- [ ] Settings-API: Direkt in `config.json` schreiben oder neuer Endpunkt?

---

*Erstellt für Issue [#16](https://github.com/slekrem/bashGPT/issues/16) · Eltern-Issue [#15](https://github.com/slekrem/bashGPT/issues/15)*
