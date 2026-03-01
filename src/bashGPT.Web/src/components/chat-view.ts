import { LitElement, html, css } from 'lit'
import { customElement, state, property } from 'lit/decorators.js'
import { repeat } from 'lit/directives/repeat.js'
import './message-bubble'
import './terminal-panel'
import { sendChat, loadHistory, resetHistory } from '../api'
import type { ExecMode, CommandResult, TerminalEntry, ShellContext } from '../types'
import type { SnapshotMessage } from '../session-history'

interface Message {
  id: number
  role: 'user' | 'assistant'
  content: string
  execMode?: ExecMode
  commands?: CommandResult[]
  usedToolCalls?: boolean
}

@customElement('bashgpt-chat-view')
export class ChatView extends LitElement {
  /** Wird von außen gesetzt (Dashboard-Prompt) – löst sofortigen Send aus */
  @property() pendingPrompt = ''
  /** v2-Modus: zeigt Terminal-Panel links neben dem Chat */
  @property({ type: Boolean }) showTerminal = false
  /** Gesetzt wenn die View aktiv (sichtbar) ist – lädt History wenn leer */
  @property({ type: Boolean }) active = false
  /** Readonly-Modus für archivierte Sessions ohne Server-Kontext */
  @property({ type: Boolean }) readOnly = false
  /** One-shot Hook: wird vor dem ersten sendChat() der Session aufgerufen */
  @property({ attribute: false }) beforeSend?: () => Promise<void>
  /** Session-ID für server-seitige Persistenz (optional) */
  @property() sessionId = ''

  @state() private _messages: Message[] = []
  @state() private _loading = false
  @state() private _statusText = ''
  @state() private _statusError = false
  @state() private _mode: ExecMode = 'auto-exec'
  @state() private _terminalOpen = true
  @state() private _shellContext: ShellContext | null = null
  private _idCounter = 0
  private _historyLoadSeq = 0
  private _lastHandledPendingPrompt = ''

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow: hidden;
    }

    /* ── Split layout ───────────────────────────────────────────────────── */
    .split-wrapper {
      flex: 1;
      display: flex;
      overflow: hidden;
    }

    bashgpt-terminal-panel {
      flex: 1;
      min-width: 0;
      transition: flex 0.2s ease, opacity 0.2s ease;
    }
    bashgpt-terminal-panel.collapsed {
      flex: 0;
      overflow: hidden;
      opacity: 0;
    }

    .chat-column {
      flex: 1;
      min-width: 0;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    /* ── Chat area ──────────────────────────────────────────────────────── */
    #chat {
      flex: 1;
      overflow-y: auto;
      padding: 16px 20px;
      display: flex;
      flex-direction: column;
      gap: 4px;
      scroll-behavior: smooth;
    }

    .empty-state {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 12px;
      color: #475569;
    }
    .empty-state .icon { font-size: 40px; }
    .empty-state p { font-size: 15px; }

    /* ── Working indicator bar ──────────────────────────────────────────── */
    .working-bar {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 6px 20px;
      background: #0b1a2e;
      border-top: 1px solid #1e3a5f;
      font-size: 12px;
      color: #60a5fa;
      flex-shrink: 0;
    }
    .working-bar .spinner {
      width: 12px; height: 12px;
      border: 1.5px solid #1e3a5f;
      border-top-color: #60a5fa;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    /* ── Footer ─────────────────────────────────────────────────────────── */
    footer {
      padding: 10px 16px 16px;
      border-top: 1px solid #1e293b;
      background: rgba(15, 23, 42, 0.8);
      backdrop-filter: blur(8px);
      flex-shrink: 0;
    }

    .input-row { display: flex; gap: 8px; align-items: flex-end; }

    textarea {
      flex: 1;
      min-height: 52px;
      max-height: 160px;
      resize: vertical;
      background: #111827;
      color: #e5e7eb;
      border: 1px solid #374151;
      border-radius: 10px;
      padding: 10px 14px;
      font-family: inherit;
      font-size: 14px;
      line-height: 1.5;
      outline: none;
      transition: border-color 0.15s;
    }
    textarea:focus { border-color: #4b5563; }
    textarea:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
    textarea::placeholder { color: #4b5563; }
    textarea:disabled { opacity: 0.5; }

    .controls {
      display: flex;
      gap: 8px;
      margin-top: 8px;
      align-items: center;
      flex-wrap: wrap;
    }

    select {
      background: #111827;
      color: #e5e7eb;
      border: 1px solid #374151;
      border-radius: 8px;
      padding: 7px 10px;
      font-size: 13px;
      cursor: pointer;
      outline: none;
    }
    select:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }

    button {
      background: #111827;
      color: #e5e7eb;
      border: 1px solid #374151;
      border-radius: 8px;
      padding: 7px 14px;
      font-size: 13px;
      cursor: pointer;
      transition: background 0.15s, border-color 0.15s;
      font-family: inherit;
    }
    button:hover:not(:disabled) { background: #1f2937; border-color: #4b5563; }
    button:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
    button:disabled { opacity: 0.4; cursor: not-allowed; }

    button.primary {
      background: #14532d;
      border-color: #16a34a;
      color: #dcfce7;
      font-weight: 600;
      padding: 7px 20px;
    }
    button.primary:hover:not(:disabled) { background: #166534; }

    button.terminal-toggle {
      padding: 7px 10px;
      font-size: 12px;
      color: #64748b;
    }
    button.terminal-toggle.active { color: #22c55e; border-color: #166534; }

    .status {
      font-size: 12px;
      color: #4b5563;
      flex: 1;
      text-align: right;
    }
    .status.error { color: #ef4444; }

    /* ── Mobile ─────────────────────────────────────────────────────────── */
    @media (max-width: 768px) {
      bashgpt-terminal-panel { flex: none; width: 100%; border-right: none; border-bottom: 1px solid #1e293b; }
      .split-wrapper { flex-direction: column; }
    }
  `

  async connectedCallback() {
    super.connectedCallback()
    await this._loadHistory()
  }

  updated(changed: Map<string, unknown>) {
    if (changed.has('pendingPrompt') && this.pendingPrompt) {
      // Verhindert Duplikate beim gleichen String, erlaubt aber erneutes Ausführen
      // nachdem pendingPrompt im Parent kurz auf '' zurückgesetzt wurde.
      if (this.pendingPrompt !== this._lastHandledPendingPrompt) {
        this._lastHandledPendingPrompt = this.pendingPrompt
        this._sendPrompt(this.pendingPrompt)
      }
    } else if (changed.has('pendingPrompt') && !this.pendingPrompt) {
      this._lastHandledPendingPrompt = ''
    }
    // History nachladen, wenn die View aktiv wird und noch keine Nachrichten vorhanden sind
    if (changed.has('active') && this.active && this._messages.length === 0) {
      this._loadHistory()
    }
  }

  /** Öffentlich: History neu laden (nach Session-Wechsel) */
  async reloadHistory() {
    this._messages = []
    await this._loadHistory(true)
  }

  /** Öffentlich: Snapshot-Messages laden (für archivierte Sessions) */
  loadSnapshot(messages: SnapshotMessage[], shellContext?: ShellContext | null, hint?: string) {
    // Laufendes _loadHistory() abbrechen – sonst würde der Server-Stand
    // (text-only, ohne commands) die soeben gesetzten Daten überschreiben.
    this._historyLoadSeq++
    this._messages = messages.map(m => ({
      id: this._idCounter++,
      role: m.role,
      content: m.content,
      commands: m.commands,
      execMode: m.execMode,
    }))
    if (shellContext !== undefined) this._shellContext = shellContext ?? null
    this._statusText = hint ?? (this.readOnly ? 'Archivierte Session (nur lesen)' : '')
    this._statusError = false
    this._scrollToBottom()
  }

  /** Öffentlich: Aktuelle Messages als Snapshot auslesen */
  getSnapshot(): SnapshotMessage[] {
    return this._messages.map(m => ({
      role: m.role,
      content: m.content,
      ...(m.commands?.length ? { commands: m.commands } : {}),
      ...(m.execMode ? { execMode: m.execMode } : {}),
    }))
  }

  /** Öffentlich: Exec-Mode von außen setzen (z. B. Dashboard "Ausführen"). */
  setExecMode(mode: ExecMode) {
    this._mode = mode
  }

  /** Öffentlich: Session zurücksetzen */
  async reset() {
    try {
      await resetHistory()
      this._messages = []
      this._statusText = 'Verlauf gelöscht'
      this._statusError = false
      this._emitMessagesChanged()
    } catch (e) {
      this._statusText = `Fehler: ${e instanceof Error ? e.message : String(e)}`
      this._statusError = true
    }
  }

  private async _loadHistory(force = false) {
    const loadSeq = ++this._historyLoadSeq
    try {
      const history = await loadHistory()
      if (loadSeq !== this._historyLoadSeq) return
      // Wenn zwischen Start und Ende bereits neue Messages entstanden sind
      // (z. B. Dashboard-Prompt wurde gesendet), darf History diese nicht überschreiben.
      if (!force && this._messages.length > 0) return

      this._messages = history.map(m => ({
        id: this._idCounter++,
        role: m.role,
        content: m.content,
      }))
      this._statusError = false
      this._emitMessagesChanged()
    } catch (e) {
      this._statusError = true
      this._statusText = `Fehler: ${e instanceof Error ? e.message : String(e)}`
    }
  }

  private async _sendPrompt(prompt: string) {
    if (!prompt.trim() || this._loading) return

    // Verhindert, dass ein parallel laufendes _loadHistory() den gerade
    // angelegten User-Input nachträglich überschreibt.
    this._historyLoadSeq++
    const execMode = this._mode
    this._messages = [
      ...this._messages,
      { id: this._idCounter++, role: 'user', content: prompt, execMode },
    ]
    this._loading = true
    this._statusText = ''
    this._statusError = false
    this._scrollToBottom()
    this.dispatchEvent(new CustomEvent('chat-started', { bubbles: true, composed: true }))

    try {
      if (this.beforeSend) await this.beforeSend()
      const result = await sendChat(prompt, execMode, this.sessionId || undefined)
      if (result.shellContext)
        this._shellContext = result.shellContext
      this._messages = [
        ...this._messages,
        {
          id: this._idCounter++,
          role: 'assistant',
          content: result.response,
          commands: result.commands,
          usedToolCalls: result.usedToolCalls,
        },
      ]
      const parts = [`tool_calls=${result.usedToolCalls ? 'ja' : 'nein'}`]
      if (result.commands.length > 0)
        parts.push(`${result.commands.length} Befehl${result.commands.length > 1 ? 'e' : ''}`)
      this._statusText = parts.join(' · ')
      this._statusError = false
      this._emitMessagesChanged()
    } catch (e) {
      this._statusText = `Fehler: ${e instanceof Error ? e.message : String(e)}`
      this._statusError = true
      this._messages = [
        ...this._messages,
        { id: this._idCounter++, role: 'assistant', content: `⚠️ ${this._statusText}` },
      ]
      this._emitMessagesChanged()
    } finally {
      this._loading = false
      this._scrollToBottom()
    }
  }

  private async _send() {
    const textarea = this.shadowRoot!.querySelector('textarea')!
    const prompt = textarea.value.trim()
    if (!prompt) return
    textarea.value = ''
    await this._sendPrompt(prompt)
  }

  private _scrollToBottom() {
    requestAnimationFrame(() => {
      const chat = this.shadowRoot?.querySelector('#chat')
      if (chat) chat.scrollTop = chat.scrollHeight
    })
  }

  private _onKeydown(e: KeyboardEvent) {
    if (this.readOnly) return
    if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
      e.preventDefault()
      this._send()
    }
  }

  private _emitMessagesChanged() {
    this.dispatchEvent(new CustomEvent('messages-changed', {
      bubbles: true,
      composed: true,
      detail: { messages: this.getSnapshot(), shellContext: this._shellContext },
    }))
  }

  /** Aggregiert alle CommandResults aus allen Nachrichten als TerminalEntries */
  private get _terminalEntries(): TerminalEntry[] {
    const entries: TerminalEntry[] = []
    for (const msg of this._messages) {
      if (msg.role !== 'assistant' || !msg.commands?.length) continue
      for (const cmd of msg.commands) {
        let status: TerminalEntry['status']
        if (!cmd.wasExecuted) status = 'skipped'
        else if (cmd.exitCode === 0) status = 'success'
        else status = 'error'
        entries.push({
          command: cmd.command,
          output: cmd.output,
          exitCode: cmd.exitCode,
          wasExecuted: cmd.wasExecuted,
          status,
        })
      }
    }
    return entries
  }

  private _workingText() {
    if (!this._loading) return ''
    const last = this._messages.at(-1)
    // Wenn letzter Eintrag vom User kommt, sind wir noch in der LLM-Phase
    return last?.role === 'user' ? 'Denke…' : 'Verarbeite Tool-Ergebnis…'
  }

  private _fallbackShellContext(): ShellContext {
    return {
      user: 'benutzer',
      host: window.location.hostname || 'maschine',
      cwd: '~',
    }
  }

  render() {
    const isEmpty = this._messages.length === 0
    const workingText = this._workingText()
    const showPanel = this.showTerminal && this._terminalOpen

    return html`
      <div class="split-wrapper">
        ${this.showTerminal ? html`
          <bashgpt-terminal-panel
            class="${showPanel ? '' : 'collapsed'}"
            .entries=${this._terminalEntries}
            .shellContext=${this._shellContext ?? this._fallbackShellContext()}
            ?loading=${this._loading}
          ></bashgpt-terminal-panel>
        ` : ''}

        <div class="chat-column">
          <div id="chat">
            ${isEmpty
              ? html`
                  <div class="empty-state">
                    <div class="icon">⌨️</div>
                    <p>Stell mir eine Frage oder wähle einen Use-Case.</p>
                  </div>
                `
              : repeat(
                  this._messages,
                  m => m.id,
                  m => html`
                    <bashgpt-message
                      role=${m.role}
                      content=${m.content}
                      execMode=${m.execMode ?? ''}
                    ></bashgpt-message>
                  `
                )}
          </div>

          ${this._loading ? html`
            <div class="working-bar">
              <div class="spinner"></div>
              ${workingText}
            </div>
          ` : ''}
        </div>
      </div>

      <footer>
        <div class="input-row">
          <textarea
            placeholder=${this.readOnly
              ? 'Archivierte Session (nur lesen)'
              : 'Nachricht eingeben… (Cmd+Enter zum Senden)'}
            aria-label="Nachricht eingeben"
            @keydown=${this._onKeydown}
            ?disabled=${this._loading || this.readOnly}
          ></textarea>
        </div>
        <div class="controls">
          <select
            .value=${this._mode}
            @change=${(e: Event) => { this._mode = (e.target as HTMLSelectElement).value as ExecMode }}
            ?disabled=${this._loading || this.readOnly}
            aria-label="Ausführungsmodus"
          >
            <option value="ask">ask</option>
            <option value="dry-run">dry-run</option>
            <option value="auto-exec">auto-exec</option>
            <option value="no-exec">no-exec</option>
          </select>

          ${this.showTerminal ? html`
            <button
              class="terminal-toggle ${this._terminalOpen ? 'active' : ''}"
              @click=${() => { this._terminalOpen = !this._terminalOpen }}
              title="Terminal ein-/ausblenden"
              aria-pressed=${this._terminalOpen ? 'true' : 'false'}
              aria-label="Terminal ein-/ausblenden"
            >⌃ Terminal</button>
          ` : ''}

          <span
            class="status ${this._statusError ? 'error' : ''}"
            aria-live="polite"
            aria-atomic="true"
          >
            ${this._statusText}
          </span>

          <button
            class="primary"
            @click=${this._send}
            ?disabled=${this._loading || this.readOnly}
            aria-label="Nachricht senden"
          >
            Senden
          </button>
        </div>
      </footer>
    `
  }
}
