import { LitElement, html, css } from 'lit'
import { customElement, state } from 'lit/decorators.js'
import { getSettings, saveSettings, testConnection } from '../api'
import type { Settings, ProviderName } from '../types'

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
      this._status = 'Settings could not be loaded. Please ensure that the server is running.'
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
      ollama: {
        model: ollamaModel,
        host: ollamaHost,
      },
    }
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
      ollama: {
        model: settings.ollama.model,
        host: settings.ollama.host,
      },
    }
  }

  private async _save() {
    if (!this._settings) return

    if (!this._settings.ollama.host.trim()) {
      this._status = 'Ollama Host must not be empty.'
      this._statusOk = false
      return
    }

    this._loading = true
    try {
      await saveSettings(this._buildSavePayload(this._settings))
      this._status = 'Settings saved.'
      this._statusOk = true
    } catch (e) {
      this._status = `Error: ${e instanceof Error ? e.message : String(e)}`
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
      this._status = `Connection OK${result.latencyMs != null ? ` (${result.latencyMs} ms)` : ''}`
      this._statusOk = true
    } else {
      this._status = `Connection failed: ${result.error ?? 'Unknown'}`
      this._statusOk = false
    }
  }

  private _clearHistory() {
    if (!confirm('Really delete entire history? This action cannot be undone.')) return
    this.dispatchEvent(new CustomEvent('clear-history', { bubbles: true, composed: true }))
  }

  private _renderProviderDocumentation() {
    return html`
      <h3>Ollama Docs</h3>
      <p>These options are sent in the request to <code>/v1/chat/completions</code> (OpenAI-compatible endpoint). Brief hints are shown directly below the input fields on the left.</p>
      <div class="doc-group">
        <span class="doc-label">Basic</span>
        <ul class="doc-list">
          <li><code>model</code> - local model, e.g. <code>gpt-oss:20b</code>. Effect: controls capability, RAM requirement, and latency.</li>
          <li><code>host</code> - Default: <code>http://localhost:11434</code>. Effect: target instance for all Ollama requests.</li>
        </ul>
      </div>
      <div class="doc-group">
        <span class="doc-label">Sampling</span>
        <ul class="doc-list">
          <li>Additional sampling parameters are not currently persistently configured in the open-source build.</li>
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
        <h2>Settings</h2>
        <div class="loading-placeholder">
          <div class="spinner"></div> Loading settings…
        </div>
      `
    }

    return html`
      <h2>Settings</h2>
      <div class="layout">
      <div class="settings-main">
      <div class="section">
        <div class="section-label">Connection</div>

        <div class="field">
          <label>Provider</label>
          <input type="text" .value=${s?.provider ?? 'ollama'} disabled />
          <div class="hint">Open-source builds use Ollama exclusively.</div>
        </div>
        <div class="field">
          <label>Ollama Model</label>
          <input
            type="text"
            .value=${s?.ollama.model ?? ''}
            @input=${(e: InputEvent) => this._setOllamaModel((e.target as HTMLInputElement).value)}
            ?disabled=${!s || this._loading}
            placeholder="e.g. gpt-oss:20b"
          />
          <div class="hint">Controls quality, speed, and resource usage.</div>
        </div>

        <div class="field">
          <label>Ollama Host</label>
          <input
            type="text"
            .value=${s?.ollama.host ?? 'http://localhost:11434'}
            @input=${(e: InputEvent) => this._setOllamaHost((e.target as HTMLInputElement).value)}
            ?disabled=${!s || this._loading}
          />
          <div class="hint">Default locally: <code>http://localhost:11434</code>.</div>
        </div>

        <div class="actions">
          <button @click=${this._test} ?disabled=${!s || this._testing || this._loading}>
            ${this._testing ? 'Testing…' : 'Test connection'}
          </button>
        </div>
      </div>

      <div class="section">
        <div class="section-label">History</div>
        <div class="hint">Deletes all saved sessions from browser storage and resets server history.</div>
        <div class="actions" style="margin-top: 12px;">
          <button class="danger" @click=${this._clearHistory}>
            Delete entire history
          </button>
        </div>
      </div>

      <div class="divider"></div>

      <div class="actions">
        <button class="primary" @click=${this._save} ?disabled=${!s || this._loading}>
          ${this._loading ? 'Saving…' : 'Save'}
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
      <aside class="provider-doc" aria-label="Provider Documentation">
        ${this._renderProviderDocumentation()}
      </aside>
      </div>
    `
  }
}
