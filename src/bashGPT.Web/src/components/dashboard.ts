import { LitElement, html, css } from 'lit'
import { customElement, state } from 'lit/decorators.js'

interface UseCase {
  category: string
  title: string
  hint: string
  risk: 'safe' | 'medium'
  prompt: string
}

const USE_CASES: UseCase[] = [
  { category: 'System', title: 'Prozesse', hint: 'Alle laufenden Prozesse auflisten', risk: 'safe', prompt: 'Zeige alle laufenden Prozesse und ihre Ressourcennutzung.' },
  { category: 'System', title: 'Speicher & Disk', hint: 'Festplatten- und RAM-Nutzung anzeigen', risk: 'safe', prompt: 'Zeige die aktuelle Festplatten- und RAM-Nutzung.' },
  { category: 'System', title: 'Uptime & Load', hint: 'Systemlaufzeit und Last prüfen', risk: 'safe', prompt: 'Wie lange läuft das System schon? Zeige Uptime und Load Average.' },
  { category: 'System', title: 'Logs', hint: 'Aktuelle Systemlogs anzeigen', risk: 'safe', prompt: 'Zeige die neuesten Systemlogs (letzte 50 Zeilen).' },
  { category: 'Git', title: 'Status', hint: 'Aktuellen Branch und Änderungen anzeigen', risk: 'safe', prompt: 'Zeige den git-Status des aktuellen Verzeichnisses.' },
  { category: 'Git', title: 'Log', hint: 'Letzte 10 Commits anzeigen', risk: 'safe', prompt: 'Zeige die letzten 10 git-Commits mit Autor und Datum.' },
  { category: 'Dateien', title: 'Verzeichnis', hint: 'Dateien im aktuellen Ordner auflisten', risk: 'safe', prompt: 'Liste alle Dateien im aktuellen Verzeichnis mit Details auf.' },
  { category: 'Dateien', title: 'Datei suchen', hint: 'Datei nach Name suchen', risk: 'safe', prompt: 'Suche nach einer bestimmten Datei – welchen Namen soll ich suchen?' },
  { category: 'Netzwerk', title: 'IP-Adressen', hint: 'Netzwerk-Interfaces anzeigen', risk: 'safe', prompt: 'Zeige alle aktuellen Netzwerk-Interfaces und IP-Adressen.' },
  { category: 'Netzwerk', title: 'Offene Ports', hint: 'Aktive Dienste und Ports auflisten', risk: 'medium', prompt: 'Welche Ports sind aktuell offen und welche Dienste lauschen?' },
]

const RECENT_KEY = 'bashgpt_recent_prompts'
const MAX_RECENT = 3

@customElement('bashgpt-dashboard')
export class Dashboard extends LitElement {
  @state() private _search = ''
  @state() private _recent: string[] = []

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow-y: auto;
      padding: 28px 28px 40px;
      box-sizing: border-box;
    }

    .greeting {
      font-size: 22px;
      font-weight: 700;
      color: #f1f5f9;
      margin-bottom: 4px;
    }
    .subtitle {
      font-size: 14px;
      color: #64748b;
      margin-bottom: 20px;
    }

    .search {
      width: 100%;
      max-width: 480px;
      padding: 10px 14px;
      background: #111827;
      border: 1px solid #374151;
      border-radius: 10px;
      color: #e5e7eb;
      font-size: 14px;
      outline: none;
      box-sizing: border-box;
      margin-bottom: 24px;
      transition: border-color 0.15s;
    }
    .search:focus { border-color: #4b5563; }
    .search:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
    .search::placeholder { color: #4b5563; }

    .section-label {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.08em;
      color: #475569;
      text-transform: uppercase;
      margin-bottom: 10px;
    }

    .recent-row {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-bottom: 28px;
    }
    .recent-chip {
      padding: 6px 12px;
      background: #1e293b;
      border: 1px solid #334155;
      border-radius: 999px;
      font-size: 12px;
      color: #94a3b8;
      cursor: pointer;
      transition: background 0.12s, color 0.12s;
      max-width: 220px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      font-family: inherit;
    }
    .recent-chip:hover { background: #334155; color: #e2e8f0; }
    .recent-chip:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }

    .category-block { margin-bottom: 24px; }

    .category-header {
      font-size: 13px;
      font-weight: 600;
      color: #64748b;
      margin-bottom: 10px;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .category-header::after {
      content: '';
      flex: 1;
      height: 1px;
      background: #1e293b;
    }

    .cards-row {
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
    }

    .card {
      background: #0f172a;
      border: 1px solid #1e293b;
      border-radius: 10px;
      padding: 14px;
      width: 200px;
      display: flex;
      flex-direction: column;
      gap: 6px;
      transition: border-color 0.12s;
    }
    .card:hover { border-color: #334155; }

    .empty-search {
      padding: 32px 0;
      text-align: center;
      color: #475569;
      font-size: 14px;
    }

    .card-title {
      font-size: 14px;
      font-weight: 600;
      color: #f1f5f9;
    }
    .card-hint {
      font-size: 12px;
      color: #64748b;
      flex: 1;
    }

    .risk-badge {
      align-self: flex-start;
      font-size: 10px;
      font-weight: 600;
      padding: 2px 7px;
      border-radius: 999px;
    }
    .risk-safe     { background: #14532d; color: #86efac; }
    .risk-medium   { background: #78350f; color: #fcd34d; }

    .card-actions {
      display: flex;
      gap: 6px;
      margin-top: 4px;
    }
    .card-actions button {
      flex: 1;
      padding: 5px 8px;
      font-size: 12px;
      border-radius: 6px;
      cursor: pointer;
      border: 1px solid #374151;
      background: #1e293b;
      color: #e5e7eb;
      transition: background 0.12s;
    }
    .card-actions button:hover { background: #334155; }
    .card-actions button:focus-visible { outline: 2px solid #22c55e; outline-offset: 1px; }
    .card-actions button.run {
      background: #14532d;
      border-color: #16a34a;
      color: #dcfce7;
    }
    .card-actions button.run:hover { background: #166534; }

    @media (max-width: 768px) {
      :host { padding: 16px 16px 32px; }
      .card { width: 100%; }
    }
  `

  connectedCallback() {
    super.connectedCallback()
    this._loadRecent()
  }

  private _loadRecent() {
    try {
      const raw = localStorage.getItem(RECENT_KEY)
      this._recent = raw ? JSON.parse(raw) : []
    } catch {
      this._recent = []
    }
  }

  private _dispatch(prompt: string) {
    try {
      const updated = [prompt, ...this._recent.filter(r => r !== prompt)].slice(0, MAX_RECENT)
      localStorage.setItem(RECENT_KEY, JSON.stringify(updated))
      this._recent = updated
    } catch { /* ignore */ }
    this.dispatchEvent(new CustomEvent('prompt-selected', { detail: { prompt }, bubbles: true, composed: true }))
  }

  private get _filtered() {
    const q = this._search.toLowerCase().trim()
    if (!q) return USE_CASES
    return USE_CASES.filter(u =>
      u.title.toLowerCase().includes(q) ||
      u.hint.toLowerCase().includes(q) ||
      u.category.toLowerCase().includes(q)
    )
  }

  private get _categories() {
    return [...new Set(this._filtered.map(u => u.category))]
  }

  private _riskLabel(risk: UseCase['risk']) {
    return risk === 'safe' ? '✓ Sicher' : '⚠ Mittel'
  }

  render() {
    return html`
      <div class="greeting">Hallo! Ich bin bashGPT.</div>
      <div class="subtitle">Was möchtest du heute erledigen?</div>

      <input
        class="search"
        type="search"
        placeholder="Use-Cases durchsuchen…"
        aria-label="Use-Cases durchsuchen"
        .value=${this._search}
        @input=${(e: InputEvent) => { this._search = (e.target as HTMLInputElement).value }}
      />

      ${this._recent.length > 0 ? html`
        <div class="section-label">Zuletzt genutzt</div>
        <div class="recent-row">
          ${this._recent.map(p => html`
            <button class="recent-chip" title=${p} @click=${() => this._dispatch(p)}>${p}</button>
          `)}
        </div>
      ` : ''}

      ${this._filtered.length === 0 ? html`
        <div class="empty-search">Keine Use-Cases gefunden für „${this._search}".</div>
      ` : ''}

      ${this._categories.map(cat => html`
        <div class="category-block">
          <div class="category-header">${cat}</div>
          <div class="cards-row">
            ${this._filtered.filter(u => u.category === cat).map(u => html`
              <div class="card">
                <div class="card-title">${u.title}</div>
                <div class="card-hint">${u.hint}</div>
                <span class="risk-badge risk-${u.risk}">${this._riskLabel(u.risk)}</span>
                <div class="card-actions">
                  <button class="run" @click=${() => this._dispatch(u.prompt)}>Ausführen</button>
                  <button @click=${() => this.dispatchEvent(new CustomEvent('prompt-edit', { detail: { prompt: u.prompt }, bubbles: true, composed: true }))}>Anpassen</button>
                </div>
              </div>
            `)}
          </div>
        </div>
      `)}
    `
  }
}
