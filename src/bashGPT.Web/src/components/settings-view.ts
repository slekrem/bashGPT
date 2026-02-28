import { LitElement, html, css } from 'lit'
import { customElement, state } from 'lit/decorators.js'
import { getSettings, saveSettings, testConnection } from '../api'
import type { Settings, ExecMode } from '../types'

@customElement('bashgpt-settings-view')
export class SettingsView extends LitElement {
  @state() private _settings: Settings | null = null
  @state() private _loading = false
  @state() private _testing = false
  @state() private _status = ''
  @state() private _statusOk = true

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow-y: auto;
      padding: 28px 28px 40px;
      box-sizing: border-box;
      max-width: 560px;
    }

    h2 {
      font-size: 20px;
      font-weight: 700;
      color: #f1f5f9;
      margin: 0 0 24px;
    }

    .section {
      margin-bottom: 28px;
    }
    .section-label {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.08em;
      color: #475569;
      text-transform: uppercase;
      margin-bottom: 12px;
      padding-bottom: 6px;
      border-bottom: 1px solid #1e293b;
    }

    .field {
      display: flex;
      flex-direction: column;
      gap: 5px;
      margin-bottom: 14px;
    }
    label {
      font-size: 13px;
      color: #94a3b8;
    }
    input, select {
      background: #111827;
      color: #e5e7eb;
      border: 1px solid #374151;
      border-radius: 8px;
      padding: 9px 12px;
      font-size: 14px;
      outline: none;
      transition: border-color 0.15s;
      font-family: inherit;
    }
    input:focus, select:focus { border-color: #4b5563; }
    input:focus-visible, select:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }

    .toggle-row {
      display: flex;
      align-items: center;
      gap: 10px;
    }
    input[type=checkbox] {
      width: 16px;
      height: 16px;
      accent-color: #22c55e;
      cursor: pointer;
      padding: 0;
    }

    .actions {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
      margin-top: 8px;
    }

    button {
      padding: 8px 16px;
      border-radius: 8px;
      font-size: 13px;
      cursor: pointer;
      border: 1px solid #374151;
      background: #1e293b;
      color: #e5e7eb;
      transition: background 0.12s;
    }
    button:hover:not(:disabled) { background: #334155; }
    button:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
    button:disabled { opacity: 0.4; cursor: not-allowed; }

    button.primary {
      background: #14532d;
      border-color: #16a34a;
      color: #dcfce7;
      font-weight: 600;
    }
    button.primary:hover:not(:disabled) { background: #166534; }

    button.danger {
      background: #7f1d1d;
      border-color: #991b1b;
      color: #fca5a5;
    }
    button.danger:hover:not(:disabled) { background: #991b1b; }

    .status-msg {
      margin-top: 14px;
      font-size: 13px;
      padding: 8px 12px;
      border-radius: 8px;
    }
    .status-msg.ok    { background: #14532d; color: #86efac; }
    .status-msg.error { background: #7f1d1d; color: #fca5a5; }

    .divider {
      height: 1px;
      background: #1e293b;
      margin: 4px 0 20px;
    }

    .hint {
      font-size: 12px;
      color: #475569;
      margin-top: 2px;
    }

    .loading-placeholder {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 40px 0;
      color: #475569;
      font-size: 14px;
    }
    .loading-placeholder .spinner {
      width: 16px; height: 16px;
      border: 2px solid #1e293b;
      border-top-color: #22c55e;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
      flex-shrink: 0;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    @media (max-width: 768px) {
      :host { padding: 16px 16px 32px; }
    }
  `

  async connectedCallback() {
    super.connectedCallback()
    this._loading = true
    this._settings = await getSettings()
    this._loading = false
    if (!this._settings) {
      this._status = 'Einstellungen konnten nicht geladen werden. Bitte stelle sicher, dass der Server läuft.'
      this._statusOk = false
    }
  }

  private _set<K extends keyof Settings>(key: K, value: Settings[K]) {
    if (!this._settings) return
    this._settings = { ...this._settings, [key]: value }
    this._status = ''
  }

  private async _save() {
    if (!this._settings) return
    this._loading = true
    try {
      await saveSettings(this._settings)
      this._status = 'Einstellungen gespeichert.'
      this._statusOk = true
    } catch (e) {
      this._status = `Fehler: ${e instanceof Error ? e.message : String(e)}`
      this._statusOk = false
    } finally {
      this._loading = false
    }
  }

  private async _test() {
    this._testing = true
    this._status = ''
    const result = await testConnection()
    this._testing = false
    if (result.ok) {
      this._status = `Verbindung OK${result.latencyMs != null ? ` (${result.latencyMs} ms)` : ''}`
      this._statusOk = true
    } else {
      this._status = `Verbindung fehlgeschlagen: ${result.error ?? 'Unbekannt'}`
      this._statusOk = false
    }
  }

  private _switchToV1() {
    localStorage.removeItem('bashgpt_ui_v2')
    location.reload()
  }

  render() {
    const s = this._settings

    if (this._loading && !s) {
      return html`
        <h2>Einstellungen</h2>
        <div class="loading-placeholder">
          <div class="spinner"></div> Einstellungen werden geladen…
        </div>
      `
    }

    return html`
      <h2>Einstellungen</h2>

      <div class="section">
        <div class="section-label">Verbindung</div>

        <div class="field">
          <label>Provider</label>
          <select
            .value=${s?.provider ?? 'cerebras'}
            @change=${(e: Event) => this._set('provider', (e.target as HTMLSelectElement).value)}
            ?disabled=${!s || this._loading}
          >
            <option value="cerebras">cerebras</option>
            <option value="ollama">ollama</option>
          </select>
        </div>

        <div class="field">
          <label>Modell</label>
          <input
            type="text"
            .value=${s?.model ?? ''}
            @input=${(e: InputEvent) => this._set('model', (e.target as HTMLInputElement).value)}
            ?disabled=${!s || this._loading}
            placeholder="z.B. gpt-oss-120b"
          />
        </div>

        <div class="field">
          <label>API-Key</label>
          <input
            type="password"
            .value=${s?.apiKey ?? ''}
            @input=${(e: InputEvent) => this._set('apiKey', (e.target as HTMLInputElement).value)}
            ?disabled=${!s || this._loading}
            placeholder="Leer lassen um bestehenden Key zu behalten"
            autocomplete="off"
          />
          <div class="hint">Nur für Cerebras. Leer lassen = bestehender Key bleibt.</div>
        </div>

        <div class="field">
          <label>Ollama Host</label>
          <input
            type="text"
            .value=${s?.ollamaHost ?? 'http://localhost:11434'}
            @input=${(e: InputEvent) => this._set('ollamaHost', (e.target as HTMLInputElement).value)}
            ?disabled=${!s || this._loading}
          />
        </div>

        <div class="actions">
          <button @click=${this._test} ?disabled=${!s || this._testing || this._loading}>
            ${this._testing ? 'Teste…' : 'Verbindung testen'}
          </button>
        </div>
      </div>

      <div class="section">
        <div class="section-label">Ausführung</div>

        <div class="field">
          <label>Standard Exec-Mode</label>
          <select
            .value=${s?.execMode ?? 'ask'}
            @change=${(e: Event) => this._set('execMode', (e.target as HTMLSelectElement).value as ExecMode)}
            ?disabled=${!s || this._loading}
          >
            <option value="ask">ask</option>
            <option value="dry-run">dry-run</option>
            <option value="auto-exec">auto-exec</option>
            <option value="no-exec">no-exec</option>
          </select>
        </div>

        <div class="field">
          <div class="toggle-row">
            <input
              type="checkbox"
              id="force-tools"
              .checked=${s?.forceTools ?? false}
              @change=${(e: Event) => this._set('forceTools', (e.target as HTMLInputElement).checked)}
              ?disabled=${!s || this._loading}
            />
            <label for="force-tools">Force Tools (Tool-Calling erzwingen)</label>
          </div>
        </div>
      </div>

      <div class="divider"></div>

      <div class="actions">
        <button class="primary" @click=${this._save} ?disabled=${!s || this._loading}>
          ${this._loading ? 'Speichert…' : 'Speichern'}
        </button>
        <button class="danger" @click=${this._switchToV1}>
          Zur alten UI wechseln
        </button>
      </div>

      <div aria-live="polite" aria-atomic="true">
        ${this._status ? html`
          <div class="status-msg ${this._statusOk ? 'ok' : 'error'}" role=${this._statusOk ? 'status' : 'alert'}>
            ${this._status}
          </div>
        ` : ''}
      </div>
    `
  }
}
