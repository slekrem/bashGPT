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
  @state() private _showCerebrasApiKey = false

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

    const provider: ProviderName = settings.provider === 'cerebras' ? 'cerebras' : 'ollama'
    const cerebrasModel = settings.cerebras?.model
      ?? (provider === 'cerebras' ? settings.model : 'gpt-oss:120b-cloud')
    const ollamaModel = settings.ollama?.model
      ?? (provider === 'ollama' ? settings.model : 'gpt-oss:20b')
    const ollamaHost = settings.ollama?.host ?? settings.ollamaHost ?? 'http://localhost:11434'
    const hasApiKey = settings.hasApiKey ?? settings.cerebras?.hasApiKey ?? false

    return {
      ...settings,
      provider,
      model: provider === 'cerebras' ? cerebrasModel : ollamaModel,
      ollamaHost,
      cerebras: {
        model: cerebrasModel,
        apiKey: settings.cerebras?.apiKey ?? settings.apiKey ?? '',
        hasApiKey,
        baseUrl: settings.cerebras?.baseUrl ?? 'https://api.cerebras.ai/v1',
        temperature: settings.cerebras?.temperature,
        topP: settings.cerebras?.topP,
        maxCompletionTokens: settings.cerebras?.maxCompletionTokens,
        seed: settings.cerebras?.seed,
        reasoningEffort: settings.cerebras?.reasoningEffort,
      },
      ollama: {
        model: ollamaModel,
        host: ollamaHost,
        temperature: settings.ollama?.temperature,
        topP: settings.ollama?.topP,
        numCtx: settings.ollama?.numCtx,
        numPredict: settings.ollama?.numPredict,
        repeatPenalty: settings.ollama?.repeatPenalty,
        seed: settings.ollama?.seed,
      },
    }
  }

  private _setRoot<K extends keyof Settings>(key: K, value: Settings[K]) {
    if (!this._settings) return
    this._settings = { ...this._settings, [key]: value }
    this._status = ''
  }

  private _setProvider(provider: ProviderName) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      provider,
      model: provider === 'cerebras' ? this._settings.cerebras.model : this._settings.ollama.model,
    }
    this._status = ''
  }

  private _setCerebrasModel(model: string) {
    if (!this._settings) return
    const next = {
      ...this._settings,
      cerebras: { ...this._settings.cerebras, model },
    }
    this._settings = {
      ...next,
      ...(next.provider === 'cerebras' ? { model } : {}),
    }
    this._status = ''
  }

  private _setCerebrasApiKey(apiKey: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      apiKey,
      cerebras: { ...this._settings.cerebras, apiKey },
    }
    this._status = ''
  }

  private _setCerebrasBaseUrl(baseUrl: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      cerebras: { ...this._settings.cerebras, baseUrl },
    }
    this._status = ''
  }

  private _setCerebrasTemperature(value: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      cerebras: { ...this._settings.cerebras, temperature: this._parseFloatInput(value) },
    }
    this._status = ''
  }

  private _setCerebrasTopP(value: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      cerebras: { ...this._settings.cerebras, topP: this._parseFloatInput(value) },
    }
    this._status = ''
  }

  private _setCerebrasMaxCompletionTokens(value: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      cerebras: { ...this._settings.cerebras, maxCompletionTokens: this._parseIntInput(value) },
    }
    this._status = ''
  }

  private _setCerebrasSeed(value: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      cerebras: { ...this._settings.cerebras, seed: this._parseIntInput(value) },
    }
    this._status = ''
  }

  private _setCerebrasReasoningEffort(value: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      cerebras: { ...this._settings.cerebras, reasoningEffort: value.trim() || undefined },
    }
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

  private _setOllamaTemperature(value: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      ollama: { ...this._settings.ollama, temperature: this._parseFloatInput(value) },
    }
    this._status = ''
  }

  private _setOllamaTopP(value: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      ollama: { ...this._settings.ollama, topP: this._parseFloatInput(value) },
    }
    this._status = ''
  }

  private _setOllamaNumCtx(value: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      ollama: { ...this._settings.ollama, numCtx: this._parseIntInput(value) },
    }
    this._status = ''
  }

  private _setOllamaNumPredict(value: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      ollama: { ...this._settings.ollama, numPredict: this._parseIntInput(value) },
    }
    this._status = ''
  }

  private _setOllamaRepeatPenalty(value: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      ollama: { ...this._settings.ollama, repeatPenalty: this._parseFloatInput(value) },
    }
    this._status = ''
  }

  private _setOllamaSeed(value: string) {
    if (!this._settings) return
    this._settings = {
      ...this._settings,
      ollama: { ...this._settings.ollama, seed: this._parseIntInput(value) },
    }
    this._status = ''
  }

  private _buildSavePayload(settings: Settings) {
    const cerebrasApiKey = settings.cerebras.apiKey?.trim()
    return {
      provider: settings.provider,
      model: settings.provider === 'cerebras' ? settings.cerebras.model : settings.ollama.model,
      ...(cerebrasApiKey ? { apiKey: cerebrasApiKey } : {}),
      ollamaHost: settings.ollama.host,
      execMode: settings.execMode,
      forceTools: settings.forceTools,
      cerebras: {
        model: settings.cerebras.model,
        ...(cerebrasApiKey ? { apiKey: cerebrasApiKey } : {}),
        baseUrl: settings.cerebras.baseUrl,
        temperature: settings.cerebras.temperature,
        topP: settings.cerebras.topP,
        maxCompletionTokens: settings.cerebras.maxCompletionTokens,
        seed: settings.cerebras.seed,
        reasoningEffort: settings.cerebras.reasoningEffort,
      },
      ollama: {
        model: settings.ollama.model,
        host: settings.ollama.host,
        temperature: settings.ollama.temperature,
        topP: settings.ollama.topP,
        numCtx: settings.ollama.numCtx,
        numPredict: settings.ollama.numPredict,
        repeatPenalty: settings.ollama.repeatPenalty,
        seed: settings.ollama.seed,
      },
    }
  }

  private _parseFloatInput(value: string): number | undefined {
    const trimmed = value.trim()
    if (!trimmed) return undefined
    const parsed = Number.parseFloat(trimmed)
    return Number.isFinite(parsed) ? parsed : undefined
  }

  private _parseIntInput(value: string): number | undefined {
    const trimmed = value.trim()
    if (!trimmed) return undefined
    const parsed = Number.parseInt(trimmed, 10)
    return Number.isFinite(parsed) ? parsed : undefined
  }

  private async _save() {
    if (!this._settings) return

    if (this._settings.provider === 'cerebras' && !this._settings.cerebras.apiKey?.trim() && !this._settings.cerebras.hasApiKey) {
      this._status = 'Cerebras benötigt einen API-Key (oder einen bereits gespeicherten Key).'
      this._statusOk = false
      return
    }
    if (this._settings.provider === 'ollama' && !this._settings.ollama.host.trim()) {
      this._status = 'Ollama Host darf nicht leer sein.'
      this._statusOk = false
      return
    }

    this._loading = true
    try {
      await saveSettings(this._buildSavePayload(this._settings))
      if (this._settings.cerebras.apiKey?.trim())
        this._settings = {
          ...this._settings,
          cerebras: { ...this._settings.cerebras, hasApiKey: true },
        }
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

  private _renderProviderDocumentation(provider: ProviderName) {
    if (provider === 'cerebras') {
      return html`
        <h3>Cerebras Doku</h3>
        <p>Diese Optionen werden im Request an <code>/chat/completions</code> genutzt. Kurz-Hinweise findest du direkt unter den Eingabefeldern links.</p>
        <div class="doc-group">
          <span class="doc-label">Pflicht</span>
          <ul class="doc-list">
            <li><code>model</code> - Modell-ID, z.B. <code>gpt-oss-120b</code>. Wirkung: bestimmt Qualität, Kosten und verfügbare Features.</li>
            <li><code>apiKey</code> - Zugriffstoken für die API. Wirkung: ohne Key sind Requests nicht möglich.</li>
          </ul>
        </div>
        <div class="doc-group">
          <span class="doc-label">Sampling Und Qualität</span>
          <ul class="doc-list">
            <li><code>temperature</code> (typisch 0 bis 1.5). Wirkung: niedriger = stabiler/konservativer, höher = kreativer/variabler.</li>
            <li><code>top_p</code> - Nucleus Sampling. Wirkung: begrenzt Token-Auswahl auf wahrscheinlichste Kandidaten.</li>
            <li><code>seed</code> - optional. Wirkung: macht Antworten bei gleichem Prompt reproduzierbarer.</li>
          </ul>
        </div>
        <div class="doc-group">
          <span class="doc-label">Antwort</span>
          <ul class="doc-list">
            <li><code>max_completion_tokens</code> - harte Obergrenze. Wirkung: verhindert zu lange Antworten und reduziert Kosten.</li>
            <li><code>reasoning_effort</code> - <code>low</code>/<code>medium</code>/<code>high</code>. Wirkung: höher = gründlicher, aber oft langsamer/teurer.</li>
          </ul>
        </div>
        <div class="doc-links">
          <a href="https://inference-docs.cerebras.ai/api-reference/chat-completions" target="_blank" rel="noreferrer">Cerebras Chat Completions</a>
          <a href="https://inference-docs.cerebras.ai/api-reference/models/list" target="_blank" rel="noreferrer">Cerebras Models</a>
          <a href="https://inference-docs.cerebras.ai/capabilities/reasoning" target="_blank" rel="noreferrer">Cerebras Reasoning Guide</a>
        </div>
      `
    }

    return html`
      <h3>Ollama Doku</h3>
      <p>Diese Optionen werden im Request an <code>/api/chat</code> als <code>options</code> gesendet. Kurz-Hinweise findest du direkt unter den Eingabefeldern links.</p>
      <div class="doc-group">
        <span class="doc-label">Basis</span>
        <ul class="doc-list">
          <li><code>model</code> - lokales Modell, z.B. <code>gpt-oss:20b</code>. Wirkung: steuert Fähigkeit, RAM-Bedarf und Latenz.</li>
          <li><code>host</code> - Standard: <code>http://localhost:11434</code>. Wirkung: Zielinstanz für alle Ollama-Requests.</li>
        </ul>
      </div>
      <div class="doc-group">
        <span class="doc-label">Optimierte Defaults in bashGPT</span>
        <ul class="doc-list">
          <li><code>temperature</code>: 0.2. Wirkung: stabileres Tool-Calling und weniger Zufall.</li>
          <li><code>top_p</code>: 0.9. Wirkung: genug Vielfalt ohne starke Drift.</li>
          <li><code>num_ctx</code>: 16384. Wirkung: längere Sessions behalten mehr Kontext (mehr RAM).</li>
          <li><code>num_predict</code>: 1024. Wirkung: begrenzt Antwortlänge, verhindert endlose Ausgaben.</li>
          <li><code>repeat_penalty</code>: 1.05. Wirkung: reduziert Schleifen und Wiederholungen.</li>
          <li><code>seed</code>: optional. Wirkung: bei gesetztem Wert reproduzierbarere Antworten.</li>
        </ul>
      </div>
      <div class="doc-links">
        <a href="https://ollama.readthedocs.io/en/api/" target="_blank" rel="noreferrer">Ollama API Reference</a>
        <a href="https://pkg.go.dev/github.com/ollama/ollama/api" target="_blank" rel="noreferrer">Ollama Options Schema</a>
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
          <select
            .value=${s?.provider ?? 'cerebras'}
            @change=${(e: Event) => this._setProvider((e.target as HTMLSelectElement).value as ProviderName)}
            ?disabled=${!s || this._loading}
          >
            <option value="cerebras">cerebras</option>
            <option value="ollama">ollama</option>
          </select>
        </div>
        ${s?.provider === 'cerebras' ? html`
          <div class="field">
            <label>Cerebras Modell</label>
            <input
              type="text"
              .value=${s?.cerebras.model ?? ''}
              @input=${(e: InputEvent) => this._setCerebrasModel((e.target as HTMLInputElement).value)}
              ?disabled=${!s || this._loading}
              placeholder="z.B. gpt-oss-120b-cloud"
            />
            <div class="hint">Bestimmt Qualität, Kosten und Feature-Set der Antworten.</div>
          </div>

          <div class="field">
            <label>Cerebras API-Key</label>
            <div class="input-with-action">
              <input
                type=${this._showCerebrasApiKey ? 'text' : 'password'}
                .value=${s?.cerebras.apiKey ?? ''}
                @input=${(e: InputEvent) => this._setCerebrasApiKey((e.target as HTMLInputElement).value)}
                ?disabled=${!s || this._loading}
                placeholder=${s?.cerebras.hasApiKey ? 'Bereits gesetzt (bei Bedarf ändern)' : 'API-Key eingeben'}
                autocomplete="off"
              />
              <button
                class="icon-btn"
                type="button"
                @click=${() => { this._showCerebrasApiKey = !this._showCerebrasApiKey }}
                ?disabled=${!s || this._loading}
                title=${this._showCerebrasApiKey ? 'API-Key verbergen' : 'API-Key anzeigen'}
                aria-label=${this._showCerebrasApiKey ? 'API-Key verbergen' : 'API-Key anzeigen'}
              >
                ${this._showCerebrasApiKey ? 'Hide' : 'Show'}
              </button>
            </div>
            <div class="hint">
              ${s?.cerebras.hasApiKey
                ? 'Ein API-Key ist gespeichert und kann bei Bedarf angepasst werden.'
                : 'Cerebras benötigt einen API-Key.'}
            </div>
          </div>

          <div class="field">
            <label>Cerebras Base URL</label>
            <input
              type="text"
              .value=${s?.cerebras.baseUrl ?? 'https://api.cerebras.ai/v1'}
              @input=${(e: InputEvent) => this._setCerebrasBaseUrl((e.target as HTMLInputElement).value)}
              ?disabled=${!s || this._loading}
            />
            <div class="hint">Nur ändern, wenn du gegen einen anderen Endpoint/Gateway sprichst.</div>
          </div>

          <div class="field">
            <label>Cerebras Temperature</label>
            <input
              type="number"
              step="0.01"
              .value=${s?.cerebras.temperature?.toString() ?? ''}
              @input=${(e: InputEvent) => this._setCerebrasTemperature((e.target as HTMLInputElement).value)}
              ?disabled=${!s || this._loading}
              placeholder="Optional"
            />
            <div class="hint">Niedriger = stabiler, höher = kreativer.</div>
          </div>

          <div class="field">
            <label>Cerebras top_p</label>
            <input
              type="number"
              step="0.01"
              .value=${s?.cerebras.topP?.toString() ?? ''}
              @input=${(e: InputEvent) => this._setCerebrasTopP((e.target as HTMLInputElement).value)}
              ?disabled=${!s || this._loading}
              placeholder="Optional"
            />
            <div class="hint">Begrenzt die Token-Auswahl auf wahrscheinlichere Kandidaten.</div>
          </div>

          <div class="field">
            <label>Cerebras max_completion_tokens</label>
            <input
              type="number"
              step="1"
              .value=${s?.cerebras.maxCompletionTokens?.toString() ?? ''}
              @input=${(e: InputEvent) => this._setCerebrasMaxCompletionTokens((e.target as HTMLInputElement).value)}
              ?disabled=${!s || this._loading}
              placeholder="Optional"
            />
            <div class="hint">Harte Obergrenze für Antwortlänge und Kosten.</div>
          </div>

          <div class="field">
            <label>Cerebras Seed</label>
            <input
              type="number"
              step="1"
              .value=${s?.cerebras.seed?.toString() ?? ''}
              @input=${(e: InputEvent) => this._setCerebrasSeed((e.target as HTMLInputElement).value)}
              ?disabled=${!s || this._loading}
              placeholder="Optional"
            />
            <div class="hint">Für reproduzierbarere Antworten beim Debugging setzen.</div>
          </div>

          <div class="field">
            <label>Cerebras Reasoning Effort</label>
            <select
              .value=${s?.cerebras.reasoningEffort ?? ''}
              @change=${(e: Event) => this._setCerebrasReasoningEffort((e.target as HTMLSelectElement).value)}
              ?disabled=${!s || this._loading}
            >
              <option value="">Standard</option>
              <option value="none">none</option>
              <option value="low">low</option>
              <option value="medium">medium</option>
              <option value="high">high</option>
            </select>
            <div class="hint">Höher = gründlicheres Reasoning, oft langsamer/teurer.</div>
          </div>
        ` : html`
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

          <div class="field">
            <label>Ollama Temperature</label>
            <input
              type="number"
              step="0.01"
              .value=${s?.ollama.temperature?.toString() ?? ''}
              @input=${(e: InputEvent) => this._setOllamaTemperature((e.target as HTMLInputElement).value)}
              ?disabled=${!s || this._loading}
              placeholder="Optional"
            />
            <div class="hint">Niedriger = stabiler Tool-Output, höher = variabler.</div>
          </div>

          <div class="field">
            <label>Ollama top_p</label>
            <input
              type="number"
              step="0.01"
              .value=${s?.ollama.topP?.toString() ?? ''}
              @input=${(e: InputEvent) => this._setOllamaTopP((e.target as HTMLInputElement).value)}
              ?disabled=${!s || this._loading}
              placeholder="Optional"
            />
            <div class="hint">Alternative/Ergänzung zu Temperature für Sampling-Kontrolle.</div>
          </div>

          <div class="field">
            <label>Ollama num_ctx</label>
            <input
              type="number"
              step="1"
              .value=${s?.ollama.numCtx?.toString() ?? ''}
              @input=${(e: InputEvent) => this._setOllamaNumCtx((e.target as HTMLInputElement).value)}
              ?disabled=${!s || this._loading}
              placeholder="Optional"
            />
            <div class="hint">Mehr Kontext, aber höherer RAM-Bedarf.</div>
          </div>

          <div class="field">
            <label>Ollama num_predict</label>
            <input
              type="number"
              step="1"
              .value=${s?.ollama.numPredict?.toString() ?? ''}
              @input=${(e: InputEvent) => this._setOllamaNumPredict((e.target as HTMLInputElement).value)}
              ?disabled=${!s || this._loading}
              placeholder="Optional"
            />
            <div class="hint">Begrenzt Antwortlänge; erhöhen, wenn Antworten abgeschnitten sind.</div>
          </div>

          <div class="field">
            <label>Ollama repeat_penalty</label>
            <input
              type="number"
              step="0.01"
              .value=${s?.ollama.repeatPenalty?.toString() ?? ''}
              @input=${(e: InputEvent) => this._setOllamaRepeatPenalty((e.target as HTMLInputElement).value)}
              ?disabled=${!s || this._loading}
              placeholder="Optional"
            />
            <div class="hint">Reduziert Wiederholungen und Schleifen im Output.</div>
          </div>

          <div class="field">
            <label>Ollama Seed</label>
            <input
              type="number"
              step="1"
              .value=${s?.ollama.seed?.toString() ?? ''}
              @input=${(e: InputEvent) => this._setOllamaSeed((e.target as HTMLInputElement).value)}
              ?disabled=${!s || this._loading}
              placeholder="Optional"
            />
            <div class="hint">Für reproduzierbare Antworten bei identischem Prompt setzen.</div>
          </div>
        `}

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
        ${this._renderProviderDocumentation(s?.provider ?? 'cerebras')}
      </aside>
      </div>
    `
  }
}
