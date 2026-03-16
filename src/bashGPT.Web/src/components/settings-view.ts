import { LitElement, html, css } from 'lit'
import { customElement, state } from 'lit/decorators.js'
import { getSettings, saveSettings, testConnection } from '../api'
import type { Settings, ExecMode, ProviderName } from '../types'

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
      width: 100%;
    }

    h2 {
      font-size: 20px;
      font-weight: 700;
      color: #f1f5f9;
      margin: 0 0 24px;
    }
    .layout {
      display: grid;
      grid-template-columns: minmax(0, 1fr) 360px;
      gap: 24px;
      align-items: start;
    }
    .settings-main {
      min-width: 0;
    }
    .provider-doc {
      border: 1px solid #1e293b;
      background: #0b1220;
      border-radius: 12px;
      padding: 16px;
      position: sticky;
      top: 12px;
    }
    .provider-doc h3 {
      margin: 0 0 8px;
      font-size: 16px;
      color: #e2e8f0;
    }
    .provider-doc p {
      margin: 0 0 12px;
      color: #94a3b8;
      font-size: 13px;
      line-height: 1.45;
    }
    .doc-group {
      margin-bottom: 14px;
    }
    .doc-label {
      display: block;
      font-size: 11px;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: #475569;
      margin-bottom: 6px;
    }
    .doc-list {
      margin: 0;
      padding-left: 16px;
      color: #cbd5e1;
      font-size: 13px;
      line-height: 1.45;
    }
    .doc-list li + li {
      margin-top: 4px;
    }
    .doc-links {
      display: flex;
      flex-direction: column;
      gap: 6px;
      margin-top: 8px;
    }
    .doc-links a {
      color: #86efac;
      text-decoration: none;
      font-size: 12px;
    }
    .doc-links a:hover {
      text-decoration: underline;
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
    .input-with-action {
      display: flex;
      align-items: stretch;
      gap: 8px;
    }
    .input-with-action input {
      flex: 1;
      min-width: 0;
    }
    .icon-btn {
      width: 40px;
      padding: 0;
      border-radius: 8px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      font-size: 16px;
      line-height: 1;
    }

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
      .layout {
        grid-template-columns: 1fr;
        gap: 16px;
      }
      .provider-doc {
        position: static;
      }
    }
  `

  async connectedCallback() {
    super.connectedCallback()
    this._loading = true
    this._settings = this._normalizeSettings(await getSettings())
    this._loading = false
    if (!this._settings) {
      this._status = 'Einstellungen konnten nicht geladen werden. Bitte stelle sicher, dass der Server läuft.'
      this._statusOk = false
    }
  }

  private _normalizeSettings(settings: Settings | null): Settings | null {
    if (!settings) return null

    const provider: ProviderName = 'ollama'
    const ollamaModel = settings.ollama?.model ?? settings.model ?? 'gpt-oss:20b'
    const ollamaHost = settings.ollama?.host ?? settings.ollamaHost ?? 'http://localhost:11434'

    return {
      ...settings,
      provider,
      model: ollamaModel,
      ollamaHost,
      commandTimeoutSeconds: settings.commandTimeoutSeconds ?? 300,
      loopDetectionEnabled: settings.loopDetectionEnabled ?? true,
      maxToolCallRounds: settings.maxToolCallRounds ?? 8,
      rateLimiting: {
        enabled: settings.rateLimiting?.enabled ?? true,
        maxRequestsPerMinute: settings.rateLimiting?.maxRequestsPerMinute ?? 30,
        agentRequestDelayMs: settings.rateLimiting?.agentRequestDelayMs ?? 500,
      },
      ollama: {
        model: ollamaModel,
        host: ollamaHost,
      },
    }
  }

  private _setRoot<K extends keyof Settings>(key: K, value: Settings[K]) {
    if (!this._settings) return
    this._settings = { ...this._settings, [key]: value }
    this._status = ''
  }

  private _setOllamaModel(model: string) {
    if (!this._settings) return
    const next = {
      ...this._settings,
      ollama: { ...this._settings.ollama, model },
    }
    this._settings = {
      ...next,
      ...(next.provider === 'ollama' ? { model } : {}),
    }
    this._status = ''
  }

  private _setOllamaHost(host: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      ollamaHost: host,
      ollama: { ...this._settings.ollama, host },
    }
    this._status = ''
  }

  private _buildSavePayload(settings: Settings) {
    return {
      provider: settings.provider,
      model: settings.ollama.model,
      ollamaHost: settings.ollama.host,
      execMode: settings.execMode,
      forceTools: settings.forceTools,
      commandTimeoutSeconds: settings.commandTimeoutSeconds,
      loopDetectionEnabled: settings.loopDetectionEnabled,
      maxToolCallRounds: settings.maxToolCallRounds,
      rateLimiting: settings.rateLimiting,
      ollama: {
        model: settings.ollama.model,
        host: settings.ollama.host,
      },
    }
  }

  private _parseIntInput(value: string): number | undefined {
    const trimmed = value.trim()
    if (!trimmed) return undefined
    const parsed = Number.parseInt(trimmed, 10)
    return Number.isFinite(parsed) ? parsed : undefined
  }

  private async _save() {
    if (!this._settings) return

    if (!this._settings.ollama.host.trim()) {
      this._status = 'Ollama Host darf nicht leer sein.'
      this._statusOk = false
      return
    }

    this._loading = true
    try {
      await saveSettings(this._buildSavePayload(this._settings))
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

  private _clearHistory() {
    if (!confirm('Gesamten Verlauf wirklich löschen? Diese Aktion kann nicht rückgängig gemacht werden.')) return
    this.dispatchEvent(new CustomEvent('clear-history', { bubbles: true, composed: true }))
  }

  private _renderProviderDocumentation() {
    return html`
      <h3>Ollama Doku</h3>
      <p>Diese Optionen werden im Request an <code>/v1/chat/completions</code> gesendet (OpenAI-kompatibler Endpunkt). Kurz-Hinweise findest du direkt unter den Eingabefeldern links.</p>
      <div class="doc-group">
        <span class="doc-label">Basis</span>
        <ul class="doc-list">
          <li><code>model</code> - lokales Modell, z.B. <code>gpt-oss:20b</code>. Wirkung: steuert Fähigkeit, RAM-Bedarf und Latenz.</li>
          <li><code>host</code> - Standard: <code>http://localhost:11434</code>. Wirkung: Zielinstanz für alle Ollama-Requests.</li>
        </ul>
      </div>
      <div class="doc-group">
        <span class="doc-label">Sampling</span>
        <ul class="doc-list">
          <li>Weitere Sampling-Parameter werden im Open-Source-Build derzeit nicht persistent konfiguriert.</li>
        </ul>
      </div>
      <div class="doc-links">
        <a href="https://ollama.readthedocs.io/en/api/" target="_blank" rel="noreferrer">Ollama API Reference</a>
        <a href="https://ollama.com/blog/openai-compatibility" target="_blank" rel="noreferrer">Ollama OpenAI Compatibility</a>
      </div>
    `
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
      <div class="layout">
      <div class="settings-main">
      <div class="section">
        <div class="section-label">Verbindung</div>

        <div class="field">
          <label>Provider</label>
          <input type="text" .value=${s?.provider ?? 'ollama'} disabled />
          <div class="hint">Open-Source-Builds nutzen ausschließlich Ollama.</div>
        </div>
        <div class="field">
          <label>Ollama Modell</label>
          <input
            type="text"
            .value=${s?.ollama.model ?? ''}
            @input=${(e: InputEvent) => this._setOllamaModel((e.target as HTMLInputElement).value)}
            ?disabled=${!s || this._loading}
            placeholder="z.B. gpt-oss:20b"
          />
          <div class="hint">Steuert Qualität, Geschwindigkeit und Ressourcenbedarf.</div>
        </div>

        <div class="field">
          <label>Ollama Host</label>
          <input
            type="text"
            .value=${s?.ollama.host ?? 'http://localhost:11434'}
            @input=${(e: InputEvent) => this._setOllamaHost((e.target as HTMLInputElement).value)}
            ?disabled=${!s || this._loading}
          />
          <div class="hint">Standard lokal: <code>http://localhost:11434</code>.</div>
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
            @change=${(e: Event) => this._setRoot('execMode', (e.target as HTMLSelectElement).value as ExecMode)}
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
              @change=${(e: Event) => this._setRoot('forceTools', (e.target as HTMLInputElement).checked)}
              ?disabled=${!s || this._loading}
            />
            <label for="force-tools">Force Tools (Tool-Calling erzwingen)</label>
          </div>
        </div>

        <div class="field">
          <label>Befehl-Timeout (Sekunden)</label>
          <input
            type="number"
            step="1"
            min="1"
            .value=${s?.commandTimeoutSeconds?.toString() ?? '300'}
            @input=${(e: InputEvent) => this._setRoot('commandTimeoutSeconds', this._parseIntInput((e.target as HTMLInputElement).value) ?? 300)}
            ?disabled=${!s || this._loading}
          />
          <div class="hint">Maximale Laufzeit pro Shell-Befehl. Standard: 300 s (5 min).</div>
        </div>

        <div class="field">
          <div class="toggle-row">
            <input
              type="checkbox"
              id="loop-detection"
              .checked=${s?.loopDetectionEnabled ?? true}
              @change=${(e: Event) => this._setRoot('loopDetectionEnabled', (e.target as HTMLInputElement).checked)}
              ?disabled=${!s || this._loading}
            />
            <label for="loop-detection">Schleifenerkennung</label>
          </div>
          <div class="hint">Beendet wiederholende Tool-Call-Schleifen automatisch.</div>
        </div>

        <div class="field">
          <label>Max. Tool-Call-Runden</label>
          <input
            type="number"
            step="1"
            min="1"
            max="32"
            .value=${s?.maxToolCallRounds?.toString() ?? '8'}
            @input=${(e: InputEvent) => this._setRoot('maxToolCallRounds', this._parseIntInput((e.target as HTMLInputElement).value) ?? 8)}
            ?disabled=${!s || this._loading || !(s?.loopDetectionEnabled ?? true)}
          />
          <div class="hint">Maximale Anzahl Tool-Call-Runden pro Anfrage. Standard: 8.</div>
        </div>
      </div>

      <div class="section">
        <div class="section-label">Rate Limiting</div>

        <div class="field">
          <div class="toggle-row">
            <input
              type="checkbox"
              id="rate-limit-enabled"
              .checked=${s?.rateLimiting?.enabled ?? false}
              @change=${(e: Event) => this._setRoot('rateLimiting', { ...(s?.rateLimiting ?? { enabled: true, maxRequestsPerMinute: 30, agentRequestDelayMs: 500 }), enabled: (e.target as HTMLInputElement).checked })}
              ?disabled=${!s || this._loading}
            />
            <label for="rate-limit-enabled">Rate Limiting aktivieren</label>
          </div>
          <div class="hint">Begrenzt LLM-Anfragen global – schützt vor 429-Fehlern bei vielen Agenten.</div>
        </div>

        <div class="field">
          <label>Max. Anfragen pro Minute</label>
          <input
            type="number"
            step="1"
            min="1"
            max="600"
            .value=${s?.rateLimiting?.maxRequestsPerMinute?.toString() ?? '30'}
            @input=${(e: InputEvent) => this._setRoot('rateLimiting', { ...(s?.rateLimiting ?? { enabled: true, maxRequestsPerMinute: 30, agentRequestDelayMs: 500 }), maxRequestsPerMinute: this._parseIntInput((e.target as HTMLInputElement).value) ?? 30 })}
            ?disabled=${!s || this._loading || !(s?.rateLimiting?.enabled ?? false)}
          />
          <div class="hint">Gleitendes Fenster über 60 Sekunden. Standard: 30.</div>
        </div>

        <div class="field">
          <label>Mindestabstand zwischen Anfragen (ms)</label>
          <input
            type="number"
            step="100"
            min="0"
            .value=${s?.rateLimiting?.agentRequestDelayMs?.toString() ?? '500'}
            @input=${(e: InputEvent) => this._setRoot('rateLimiting', { ...(s?.rateLimiting ?? { enabled: true, maxRequestsPerMinute: 30, agentRequestDelayMs: 500 }), agentRequestDelayMs: this._parseIntInput((e.target as HTMLInputElement).value) ?? 500 })}
            ?disabled=${!s || this._loading || !(s?.rateLimiting?.enabled ?? false)}
          />
          <div class="hint">Minimale Pause zwischen zwei aufeinanderfolgenden LLM-Aufrufen. Standard: 500 ms.</div>
        </div>
      </div>

      <div class="section">
        <div class="section-label">Verlauf</div>
        <div class="hint">Löscht alle gespeicherten Sessions aus dem Browser-Speicher und setzt die Server-History zurück.</div>
        <div class="actions" style="margin-top: 12px;">
          <button class="danger" @click=${this._clearHistory}>
            Gesamten Verlauf löschen
          </button>
        </div>
      </div>

      <div class="divider"></div>

      <div class="actions">
        <button class="primary" @click=${this._save} ?disabled=${!s || this._loading}>
          ${this._loading ? 'Speichert…' : 'Speichern'}
        </button>
      </div>

      <div aria-live="polite" aria-atomic="true">
        ${this._status ? html`
          <div class="status-msg ${this._statusOk ? 'ok' : 'error'}" role=${this._statusOk ? 'status' : 'alert'}>
            ${this._status}
          </div>
        ` : ''}
      </div>
      </div>
      <aside class="provider-doc" aria-label="Provider Dokumentation">
        ${this._renderProviderDocumentation()}
      </aside>
      </div>
    `
  }
}
