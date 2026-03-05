import { LitElement, html, css } from 'lit'
import { customElement, state } from 'lit/decorators.js'
import { repeat } from 'lit/directives/repeat.js'
import type { Agent } from '../types'
import { getAgents, createAgent, patchAgent, deleteAgent } from '../api'

@customElement('bashgpt-agents-view')
export class AgentsView extends LitElement {
  @state() private _agents: Agent[] = []
  @state() private _loading = true
  @state() private _error = ''
  @state() private _showForm = false
  @state() private _formType: 'git' | 'http' | 'llm' = 'git'
  @state() private _formName = ''
  @state() private _formPath = ''
  @state() private _formUrl = ''
  @state() private _formInterval = 60
  @state() private _formSystemPrompt = ''
  @state() private _formLoopInstruction = ''
  @state() private _formExecMode = 'no-exec'
  @state() private _formError = ''
  @state() private _saving = false

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      padding: 24px;
      overflow-y: auto;
      box-sizing: border-box;
    }

    h2 {
      margin: 0 0 4px;
      font-size: 18px;
      font-weight: 700;
      color: #f1f5f9;
    }

    .subtitle {
      font-size: 13px;
      color: #475569;
      margin-bottom: 20px;
    }

    .toolbar {
      display: flex;
      justify-content: flex-end;
      margin-bottom: 16px;
    }

    .btn-primary {
      background: #14532d;
      border: 1px solid #16a34a;
      color: #dcfce7;
      font-size: 13px;
      font-weight: 600;
      padding: 7px 14px;
      border-radius: 8px;
      cursor: pointer;
      transition: background 0.12s;
    }
    .btn-primary:hover { background: #166534; }
    .btn-primary:disabled { opacity: 0.4; cursor: not-allowed; }

    .btn-ghost {
      background: none;
      border: 1px solid #334155;
      color: #94a3b8;
      font-size: 12px;
      padding: 5px 10px;
      border-radius: 6px;
      cursor: pointer;
      transition: border-color 0.12s, color 0.12s;
    }
    .btn-ghost:hover { border-color: #64748b; color: #e2e8f0; }

    .btn-danger {
      background: none;
      border: 1px solid #7f1d1d;
      color: #fca5a5;
      font-size: 12px;
      padding: 5px 10px;
      border-radius: 6px;
      cursor: pointer;
      transition: border-color 0.12s, color 0.12s;
    }
    .btn-danger:hover { border-color: #ef4444; color: #fca5a5; }

    .empty {
      text-align: center;
      color: #475569;
      font-size: 14px;
      padding: 48px 0;
    }

    .agent-list {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .agent-card {
      background: #0f172a;
      border: 1px solid #1e293b;
      border-radius: 10px;
      padding: 14px 16px;
      display: flex;
      align-items: flex-start;
      gap: 12px;
    }
    .agent-card.inactive { opacity: 0.55; }

    .agent-icon {
      font-size: 20px;
      line-height: 1;
      margin-top: 2px;
    }

    .agent-body { flex: 1; min-width: 0; }

    .agent-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 4px;
    }

    .agent-name {
      font-size: 14px;
      font-weight: 600;
      color: #f1f5f9;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .badge {
      font-size: 10px;
      font-weight: 700;
      padding: 2px 6px;
      border-radius: 4px;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      flex-shrink: 0;
    }
    .badge-active   { background: #14532d; color: #86efac; }
    .badge-inactive { background: #1e293b; color: #64748b; }
    .badge-fail     { background: #7f1d1d; color: #fca5a5; }

    .agent-meta {
      font-size: 12px;
      color: #475569;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .agent-last {
      font-size: 12px;
      color: #64748b;
      margin-top: 4px;
    }

    .agent-actions {
      display: flex;
      gap: 6px;
      flex-shrink: 0;
      align-items: flex-start;
    }

    /* ── Form ────────────────────────────────────────────────────────────── */

    .form-card {
      background: #0f172a;
      border: 1px solid #22c55e44;
      border-radius: 10px;
      padding: 20px;
      margin-bottom: 16px;
    }

    .form-card h3 {
      margin: 0 0 16px;
      font-size: 15px;
      color: #f1f5f9;
    }

    .form-row {
      display: flex;
      flex-direction: column;
      gap: 6px;
      margin-bottom: 12px;
    }

    label {
      font-size: 12px;
      font-weight: 600;
      color: #94a3b8;
    }

    input, select, textarea {
      background: #020617;
      border: 1px solid #1e293b;
      border-radius: 6px;
      color: #f1f5f9;
      font-size: 13px;
      padding: 7px 10px;
      font-family: inherit;
      outline: none;
      transition: border-color 0.12s;
    }
    input:focus, select:focus, textarea:focus { border-color: #22c55e; }
    textarea { resize: vertical; min-height: 72px; }

    .type-tabs {
      display: flex;
      gap: 8px;
      margin-bottom: 16px;
    }

    .type-tab {
      flex: 1;
      padding: 8px;
      border-radius: 8px;
      cursor: pointer;
      font-size: 13px;
      font-weight: 600;
      text-align: center;
      border: 1px solid #1e293b;
      background: none;
      color: #64748b;
      transition: all 0.12s;
    }
    .type-tab.active {
      background: #14532d;
      border-color: #16a34a;
      color: #dcfce7;
    }

    .form-actions {
      display: flex;
      gap: 8px;
      justify-content: flex-end;
      margin-top: 16px;
    }

    .form-error {
      font-size: 12px;
      color: #fca5a5;
      margin-top: 8px;
    }

    .error-msg {
      color: #fca5a5;
      font-size: 13px;
      padding: 12px;
      background: #1e0a0a;
      border-radius: 8px;
      border: 1px solid #7f1d1d;
    }
  `

  async connectedCallback() {
    super.connectedCallback()
    await this._load()
  }

  private async _load() {
    this._loading = true
    this._error = ''
    try {
      this._agents = await getAgents()
    } catch (e) {
      this._error = e instanceof Error ? e.message : String(e)
    } finally {
      this._loading = false
    }
  }

  private async _toggleActive(agent: Agent) {
    try {
      const updated = await patchAgent(agent.id, { isActive: !agent.isActive })
      this._agents = this._agents.map(a => a.id === updated.id ? updated : a)
    } catch (e) {
      this._error = e instanceof Error ? e.message : String(e)
    }
  }

  private async _delete(agent: Agent) {
    if (!confirm(`Agent "${agent.name}" wirklich löschen?`)) return
    try {
      await deleteAgent(agent.id)
      this._agents = this._agents.filter(a => a.id !== agent.id)
    } catch (e) {
      this._error = e instanceof Error ? e.message : String(e)
    }
  }

  private async _submit() {
    this._formError = ''
    if (!this._formName.trim()) { this._formError = 'Name ist erforderlich.'; return }
    if (this._formType === 'git' && !this._formPath.trim()) { this._formError = 'Pfad ist erforderlich.'; return }
    if (this._formType === 'http' && !this._formUrl.trim()) { this._formError = 'URL ist erforderlich.'; return }
    if (this._formType === 'llm' && !this._formLoopInstruction.trim()) { this._formError = 'Loop-Anweisung ist erforderlich.'; return }

    this._saving = true
    try {
      const agent = await createAgent({
        name: this._formName.trim(),
        type: this._formType,
        path: this._formType === 'git' ? this._formPath.trim() : undefined,
        url:  this._formType === 'http' ? this._formUrl.trim() : undefined,
        intervalSeconds: this._formInterval,
        systemPrompt:    this._formType === 'llm' && this._formSystemPrompt.trim() ? this._formSystemPrompt.trim() : undefined,
        loopInstruction: this._formType === 'llm' ? this._formLoopInstruction.trim() : undefined,
        execMode:        this._formType === 'llm' ? this._formExecMode : undefined,
      })
      this._agents = [...this._agents, agent]
      this._resetForm()
    } catch (e) {
      this._formError = e instanceof Error ? e.message : String(e)
    } finally {
      this._saving = false
    }
  }

  private _resetForm() {
    this._showForm = false
    this._formName = ''
    this._formPath = ''
    this._formUrl = ''
    this._formInterval = 60
    this._formSystemPrompt = ''
    this._formLoopInstruction = ''
    this._formExecMode = 'no-exec'
    this._formError = ''
  }

  private _formatDate(iso?: string) {
    if (!iso) return 'Noch nie'
    try { return new Date(iso).toLocaleString('de-DE', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }) }
    catch { return iso }
  }

  render() {
    return html`
      <h2>Agenten</h2>
      <div class="subtitle">Kontinuierliche Überwachung mit automatischer LLM-Reaktion</div>

      <div class="toolbar">
        <button class="btn-primary" @click=${() => { this._showForm = !this._showForm; this._formError = '' }}>
          ${this._showForm ? '✕ Abbrechen' : '+ Neuer Agent'}
        </button>
      </div>

      ${this._showForm ? this._renderForm() : ''}

      ${this._error ? html`<div class="error-msg">${this._error}</div>` : ''}

      ${this._loading
        ? html`<div class="empty">Lade Agenten…</div>`
        : this._agents.length === 0
          ? html`<div class="empty">Noch keine Agenten. Erstelle deinen ersten Agenten!</div>`
          : html`
            <div class="agent-list">
              ${repeat(this._agents, a => a.id, a => this._renderAgent(a))}
            </div>
          `}
    `
  }

  private _renderAgent(a: Agent) {
    const icon = a.type === 'gitstatus' ? '⎇' : a.type === 'llmagent' ? '🤖' : '🌐'
    const meta = a.type === 'gitstatus' ? a.path : a.type === 'llmagent' ? (a.loopInstruction ?? '') : a.url
    const badgeClass = !a.isActive ? 'badge-inactive' : !a.lastCheckSucceeded ? 'badge-fail' : 'badge-active'
    const badgeLabel = !a.isActive ? 'Pausiert' : !a.lastCheckSucceeded ? 'Fehler' : 'Aktiv'

    return html`
      <div class="agent-card ${a.isActive ? '' : 'inactive'}">
        <div class="agent-icon">${icon}</div>
        <div class="agent-body">
          <div class="agent-header">
            <div class="agent-name">${a.name}</div>
            <span class="badge ${badgeClass}">${badgeLabel}</span>
          </div>
          <div class="agent-meta">${meta} · alle ${a.intervalSeconds}s</div>
          ${a.lastMessage ? html`<div class="agent-last">${a.lastMessage}</div>` : ''}
          <div class="agent-last">Letzter Check: ${this._formatDate(a.lastRun)}</div>
        </div>
        <div class="agent-actions">
          <button class="btn-ghost" @click=${() => this._toggleActive(a)}>
            ${a.isActive ? 'Pausieren' : 'Fortsetzen'}
          </button>
          <button class="btn-danger" @click=${() => this._delete(a)}>Löschen</button>
        </div>
      </div>
    `
  }

  private _renderForm() {
    return html`
      <div class="form-card">
        <h3>Neuer Agent</h3>

        <div class="type-tabs">
          <button
            class="type-tab ${this._formType === 'git' ? 'active' : ''}"
            @click=${() => { this._formType = 'git' }}
          >⎇ Git-Status</button>
          <button
            class="type-tab ${this._formType === 'http' ? 'active' : ''}"
            @click=${() => { this._formType = 'http' }}
          >🌐 HTTP-Status</button>
          <button
            class="type-tab ${this._formType === 'llm' ? 'active' : ''}"
            @click=${() => { this._formType = 'llm' }}
          >🤖 LLM-Agent</button>
        </div>

        <div class="form-row">
          <label>Name</label>
          <input
            type="text"
            placeholder="z.B. mein-repo"
            .value=${this._formName}
            @input=${(e: Event) => { this._formName = (e.target as HTMLInputElement).value }}
          />
        </div>

        ${this._formType === 'git' ? html`
          <div class="form-row">
            <label>Repository-Pfad</label>
            <input
              type="text"
              placeholder="/home/user/mein-projekt"
              .value=${this._formPath}
              @input=${(e: Event) => { this._formPath = (e.target as HTMLInputElement).value }}
            />
          </div>
        ` : this._formType === 'http' ? html`
          <div class="form-row">
            <label>URL</label>
            <input
              type="url"
              placeholder="https://example.com/health"
              .value=${this._formUrl}
              @input=${(e: Event) => { this._formUrl = (e.target as HTMLInputElement).value }}
            />
          </div>
        ` : html`
          <div class="form-row">
            <label>System-Prompt (optional)</label>
            <textarea
              placeholder="Du bist ein autonomer Assistent. Führe die gegebene Aufgabe präzise aus."
              .value=${this._formSystemPrompt}
              @input=${(e: Event) => { this._formSystemPrompt = (e.target as HTMLTextAreaElement).value }}
            ></textarea>
          </div>
          <div class="form-row">
            <label>Loop-Anweisung</label>
            <textarea
              placeholder="z.B. Prüfe den Festplattenverbrauch und berichte über kritische Partitionen."
              .value=${this._formLoopInstruction}
              @input=${(e: Event) => { this._formLoopInstruction = (e.target as HTMLTextAreaElement).value }}
            ></textarea>
          </div>
          <div class="form-row">
            <label>Ausführungsmodus</label>
            <select
              .value=${this._formExecMode}
              @change=${(e: Event) => { this._formExecMode = (e.target as HTMLSelectElement).value }}
            >
              <option value="no-exec">no-exec – Kein Befehl ausführen</option>
              <option value="dry-run">dry-run – Befehle anzeigen, nicht ausführen</option>
              <option value="auto-exec">auto-exec – Befehle automatisch ausführen</option>
            </select>
          </div>
        `}

        <div class="form-row">
          <label>Intervall (Sekunden)</label>
          <input
            type="number"
            min="10"
            .value=${String(this._formInterval)}
            @input=${(e: Event) => { this._formInterval = parseInt((e.target as HTMLInputElement).value) || 60 }}
          />
        </div>

        ${this._formError ? html`<div class="form-error">${this._formError}</div>` : ''}

        <div class="form-actions">
          <button class="btn-ghost" @click=${this._resetForm}>Abbrechen</button>
          <button class="btn-primary" ?disabled=${this._saving} @click=${this._submit}>
            ${this._saving ? 'Speichern…' : 'Agent erstellen'}
          </button>
        </div>
      </div>
    `
  }
}
