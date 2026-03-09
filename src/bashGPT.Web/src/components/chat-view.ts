import { LitElement, html, css } from 'lit'
import { customElement, state, property } from 'lit/decorators.js'
import { repeat } from 'lit/directives/repeat.js'
import './message-bubble'
import './terminal-panel'
import './chat-info-panel'
import { streamChat, loadHistory, resetHistory, getContext, getSettings, getTools } from '../api'
import type { CommandResult, FullShellContext, Settings, TerminalEntry, ShellContext, TokenUsage, ToolInfo } from '../types'
import type { SnapshotMessage } from '../session-history'

interface Message {
  id: number
  role: 'user' | 'assistant'
  content: string
  commands?: CommandResult[]
  usedToolCalls?: boolean
  usage?: TokenUsage
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

  // ── Grouped reactive state (3 @state instead of 13) ───────────────────────

  @state() private _chat = {
    messages:    [] as Message[],
    loading:     false,
    statusText:  '',
    statusError: false,
    shellContext: null as ShellContext | null,
    tokenUsage:  { inputTokens: 0, outputTokens: 0, totalTokens: 0 } as TokenUsage,
  }

  @state() private _panels = {
    terminalOpen: true,
    infoOpen:     false,
  }

  @state() private _ctx = {
    data:     null as FullShellContext | null,
    settings: null as Settings | null,
    loaded:   false,
    loading:  false,
  }

  private _idCounter = 0
  private _historyLoadSeq = 0
  private _lastHandledPendingPrompt = ''
  @state() private _streamingContent = ''
  @state() private _reasoningContent = ''
  @state() private _streamingId: number | null = null
  private _newRoundPending = false
  @state() private _streamingEntries: TerminalEntry[] = []
  @state() private _enabledTools: string[] = []
  @state() private _toolPickerOpen = false
  @state() private _availableTools: ToolInfo[] = []

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

    /* ── Info Panel ─────────────────────────────────────────────────────── */
    bashgpt-chat-info-panel {
      width: 280px;
      flex-shrink: 0;
      overflow: hidden;
      border-left: 1px solid #1e293b;
      transition: width 0.2s ease, opacity 0.2s ease;
    }
    bashgpt-chat-info-panel.collapsed {
      width: 0;
      opacity: 0;
    }

    /* ── Tool-Picker ────────────────────────────────────────────────────── */
    .tool-picker {
      background: #0d1b2e;
      border: 1px solid #1e3a5f;
      border-radius: 10px;
      padding: 10px 12px;
      margin-bottom: 8px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .tool-picker-title {
      font-size: 11px;
      color: #60a5fa;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .tool-picker-list {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
    }
    .tool-chip {
      display: flex;
      align-items: center;
      gap: 5px;
      padding: 4px 10px;
      border-radius: 20px;
      font-size: 12px;
      border: 1px solid #1e3a5f;
      background: #111827;
      color: #94a3b8;
      cursor: pointer;
      transition: background 0.15s, border-color 0.15s, color 0.15s;
    }
    .tool-chip:hover { background: #1e293b; }
    .tool-chip.active { background: #14532d; border-color: #16a34a; color: #dcfce7; }
    .tool-chip.active::before { content: '✓ '; }

    /* ── Mobile ─────────────────────────────────────────────────────────── */
    @media (max-width: 768px) {
      bashgpt-terminal-panel { flex: none; width: 100%; border-right: none; border-bottom: 1px solid #1e293b; }
      .split-wrapper { flex-direction: column; }
      bashgpt-chat-info-panel {
        width: 100%;
        border-left: none;
        border-top: 1px solid #1e293b;
      }
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
    if (changed.has('active') && this.active && this._chat.messages.length === 0) {
      this._loadHistory()
    }
  }

  /** Öffentlich: History neu laden (nach Session-Wechsel) */
  async reloadHistory() {
    this._chat = { ...this._chat, messages: [] }
    this._enabledTools = []
    this._toolPickerOpen = false
    await this._loadHistory(true)
  }

  /** Öffentlich: Snapshot-Messages laden (für archivierte Sessions) */
  loadSnapshot(messages: SnapshotMessage[], shellContext?: ShellContext | null, hint?: string, enabledTools?: string[]) {
    // Laufendes _loadHistory() abbrechen – sonst würde der Server-Stand
    // (text-only, ohne commands) die soeben gesetzten Daten überschreiben.
    this._historyLoadSeq++
    const newMessages = messages.map(m => ({
      id: this._idCounter++,
      role: m.role,
      content: m.content,
      commands: m.commands,
      usage: m.usage,
    }))
    this._chat = {
      ...this._chat,
      messages: newMessages,
      tokenUsage: this._sumTokenUsage(newMessages),
      shellContext: shellContext !== undefined ? (shellContext ?? null) : this._chat.shellContext,
      statusText: hint ?? (this.readOnly ? 'Archivierte Session (nur lesen)' : ''),
      statusError: false,
    }
    this._enabledTools = enabledTools ?? []
    this._toolPickerOpen = false
  }

  /** Öffentlich: Chat direkt zur letzten Nachricht scrollen */
  scrollToBottom() {
    void this.updateComplete.then(() => {
      const chatEl = this.shadowRoot?.querySelector('#chat') as HTMLElement | null
      if (!chatEl) return
      chatEl.scrollTop = chatEl.scrollHeight
    })
  }

  refreshFromAgent(messages: SnapshotMessage[], shellContext?: ShellContext | null) {
    this._historyLoadSeq++
    const chatEl = this.shadowRoot?.querySelector('#chat') as HTMLElement | null
    const prevScrollTop = chatEl?.scrollTop ?? 0

    const newMessages = messages.map((m, index) => ({
      id: this._chat.messages[index]?.id ?? this._idCounter++,
      role: m.role,
      content: m.content,
      commands: m.commands,
      usage: m.usage,
    }))
    this._chat = {
      ...this._chat,
      messages: newMessages,
      tokenUsage: this._sumTokenUsage(newMessages),
      shellContext: shellContext !== undefined ? (shellContext ?? null) : this._chat.shellContext,
    }

    void this.updateComplete.then(() => {
      const updatedChatEl = this.shadowRoot?.querySelector('#chat') as HTMLElement | null
      if (!updatedChatEl) return
      const maxScrollTop = Math.max(0, updatedChatEl.scrollHeight - updatedChatEl.clientHeight)
      updatedChatEl.scrollTop = Math.min(prevScrollTop, maxScrollTop)
    })
  }

  /** Öffentlich: Aktuelle Messages als Snapshot auslesen */
  getSnapshot(): SnapshotMessage[] {
    return this._chat.messages.map(m => ({
      role: m.role,
      content: m.content,
      ...(m.commands?.length ? { commands: m.commands } : {}),
      ...(m.usage ? { usage: m.usage } : {}),
    }))
  }

  /** Öffentlich: Session zurücksetzen */
  async reset() {
    try {
      await resetHistory()
      this._chat = {
        ...this._chat,
        messages:   [],
        tokenUsage: { inputTokens: 0, outputTokens: 0, totalTokens: 0 },
        statusText: 'Verlauf gelöscht',
        statusError: false,
      }
      this._enabledTools = []
      this._toolPickerOpen = false
      this._emitMessagesChanged()
    } catch (e) {
      this._chat = {
        ...this._chat,
        statusText:  `Fehler: ${e instanceof Error ? e.message : String(e)}`,
        statusError: true,
      }
    }
  }

  private async _loadHistory(force = false) {
    const loadSeq = ++this._historyLoadSeq
    try {
      const history = await loadHistory()
      if (loadSeq !== this._historyLoadSeq) return
      // Wenn zwischen Start und Ende bereits neue Messages entstanden sind
      // (z. B. Dashboard-Prompt wurde gesendet), darf History diese nicht überschreiben.
      if (!force && this._chat.messages.length > 0) return

      const newMessages = history.map(m => ({
        id: this._idCounter++,
        role: m.role,
        content: m.content,
      }))
      this._chat = {
        ...this._chat,
        messages:    newMessages,
        tokenUsage:  this._sumTokenUsage(newMessages),
        statusError: false,
      }
    } catch (e) {
      this._chat = {
        ...this._chat,
        statusError: true,
        statusText:  `Fehler: ${e instanceof Error ? e.message : String(e)}`,
      }
    }
  }

  private async _sendPrompt(prompt: string) {
    if (!prompt.trim() || this._chat.loading) return

    // Verhindert, dass ein parallel laufendes _loadHistory() den gerade
    // angelegten User-Input nachträglich überschreibt.
    this._historyLoadSeq++

    // Platzhalter für die Assistant-Antwort, wird live befüllt
    const assistantId = this._idCounter++
    this._streamingId = assistantId
    this._streamingContent = ''
    this._reasoningContent = ''
    this._newRoundPending = false
    this._streamingEntries = []
    this._chat = {
      ...this._chat,
      messages: [
        ...this._chat.messages,
        { id: this._idCounter++, role: 'user', content: prompt },
        { id: assistantId, role: 'assistant' as const, content: '' },
      ],
      loading:     true,
      statusText:  'Denke…',
      statusError: false,
    }
    this.dispatchEvent(new CustomEvent('chat-started', { bubbles: true, composed: true }))

    try {
      if (this.beforeSend) await this.beforeSend()

      const result = await streamChat(prompt, {
        onReasoningToken: token => {
          if (this._newRoundPending) {
            // Erst beim ersten Token der neuen Runde resetten – bis dahin bleibt der alte Text sichtbar
            this._reasoningContent = token
            this._newRoundPending = false
          } else {
            this._reasoningContent += token
          }
        },
        onToken: token => {
          this._streamingContent += token
          this._chat = {
            ...this._chat,
            messages: this._chat.messages.map(m =>
              m.id === assistantId ? { ...m, content: this._streamingContent } : m
            ),
          }
        },
        onToolCall: data => {
          this._chat = { ...this._chat, statusText: `Führe aus: ${data.command}` }
          this._streamingEntries = [
            ...this._streamingEntries,
            { command: data.command, output: '', exitCode: -1, wasExecuted: false, status: 'running' },
          ]
        },
        onCommandResult: data => {
          let updated = false
          this._streamingEntries = this._streamingEntries.map(e => {
            if (!updated && e.command === data.command && e.status === 'running') {
              updated = true
              return {
                command: data.command,
                output: data.output ?? '',
                exitCode: data.exitCode,
                wasExecuted: data.wasExecuted,
                status: !data.wasExecuted ? 'skipped' : data.exitCode === 0 ? 'success' : 'error',
              }
            }
            return e
          })
          this._chat = { ...this._chat, statusText: 'Verarbeite Ergebnis…' }
        },
        onRoundStart: data => {
          this._chat = { ...this._chat, statusText: `Tool-Runde ${data.round}…` }
          this._newRoundPending = true
        },
      }, this.sessionId || undefined, this._enabledTools.length ? this._enabledTools : undefined)

      // Finale Message mit vollständigen Daten übernehmen
      const newMessages = this._chat.messages.map(m =>
        m.id === assistantId
          ? { ...m, content: result.response, usage: result.usage ?? undefined }
          : m
      )
      this._chat = {
        ...this._chat,
        messages:    newMessages,
        tokenUsage:  this._sumTokenUsage(newMessages),
        shellContext: result.shellContext ?? this._chat.shellContext,
        statusText:  this._enabledTools.length ? `Tools: ${this._enabledTools.join(', ')}` : '',
        statusError: false,
      }
      this._streamingContent = ''
      this._reasoningContent = ''
      this._streamingId = null
      this._streamingEntries = []
      this._emitMessagesChanged()
    } catch (e) {
      const errText = `Fehler: ${e instanceof Error ? e.message : String(e)}`
      const newMessages = this._chat.messages.map(m =>
        m.id === assistantId ? { ...m, content: `⚠️ ${errText}` } : m
      )
      this._chat = {
        ...this._chat,
        messages:    newMessages,
        statusText:  errText,
        statusError: true,
      }
      this._streamingContent = ''
      this._reasoningContent = ''
      this._streamingId = null
      this._streamingEntries = []
      this._emitMessagesChanged()
    } finally {
      this._chat = { ...this._chat, loading: false }
    }
  }

  private async _send() {
    const textarea = this.shadowRoot!.querySelector('textarea')!
    const prompt = textarea.value.trim()
    if (!prompt) return
    textarea.value = ''
    await this._sendPrompt(prompt)
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
      detail: { messages: this.getSnapshot(), shellContext: this._chat.shellContext },
    }))
  }

  /** Aggregiert alle CommandResults aus allen Nachrichten als TerminalEntries */
  private get _terminalEntries(): TerminalEntry[] {
    const entries: TerminalEntry[] = []
    for (const msg of this._chat.messages) {
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

  private async _toggleInfo() {
    const newOpen = !this._panels.infoOpen
    this._panels = { ...this._panels, infoOpen: newOpen }
    if (newOpen && !this._ctx.loaded) {
      this._ctx = { ...this._ctx, loaded: true, loading: true }
      try {
        const [data, settings] = await Promise.all([getContext(), getSettings()])
        this._ctx = { ...this._ctx, data, settings }
      } finally {
        this._ctx = { ...this._ctx, loading: false }
      }
    }
  }

  private async _toggleToolPicker() {
    this._toolPickerOpen = !this._toolPickerOpen
    if (this._toolPickerOpen && this._availableTools.length === 0)
      this._availableTools = await getTools()
  }

  private _toggleTool(name: string) {
    this._enabledTools = this._enabledTools.includes(name)
      ? this._enabledTools.filter(t => t !== name)
      : [...this._enabledTools, name]
  }

  private _sumTokenUsage(messages: Message[]): TokenUsage {
    let inputTokens = 0
    let outputTokens = 0
    let cachedInputTokens = 0
    for (const message of messages) {
      if (!message.usage) continue
      inputTokens += message.usage.inputTokens
      outputTokens += message.usage.outputTokens
      cachedInputTokens += message.usage.cachedInputTokens ?? 0
    }
    return { inputTokens, outputTokens, totalTokens: inputTokens + outputTokens, cachedInputTokens }
  }

  private get _commandStats() {
    let total = 0, success = 0, error = 0, skipped = 0
    for (const m of this._chat.messages)
      for (const c of m.commands ?? []) {
        total++
        if (!c.wasExecuted) skipped++
        else if (c.exitCode === 0) success++
        else error++
      }
    return { total, success, error, skipped }
  }

  private _workingText() {
    if (!this._chat.loading) return ''
    return this._chat.statusText || 'Denke…'
  }

  private _fallbackShellContext(): ShellContext {
    return {
      user: 'benutzer',
      host: window.location.hostname || 'maschine',
      cwd: '~',
    }
  }

  render() {
    const isEmpty = this._chat.messages.length === 0
    const workingText = this._workingText()
    const showPanel = this.showTerminal && this._panels.terminalOpen

    return html`
      <div class="split-wrapper">
        ${this.showTerminal ? html`
          <bashgpt-terminal-panel
            class="${showPanel ? '' : 'collapsed'}"
            .entries=${[...this._terminalEntries, ...this._streamingEntries]}
            .shellContext=${this._chat.shellContext ?? this._fallbackShellContext()}
            ?loading=${this._chat.loading}
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
                  this._chat.messages,
                  m => m.id,
                  m => html`
                    <bashgpt-message
                      role=${m.role}
                      content=${m.id === this._streamingId && this._reasoningContent && !this._streamingContent
                        ? this._reasoningContent
                        : m.content}
                      ?reasoning=${m.id === this._streamingId && !!this._reasoningContent && !this._streamingContent}
                    ></bashgpt-message>
                  `
                )}
          </div>

          ${this._chat.loading ? html`
            <div class="working-bar">
              <div class="spinner"></div>
              ${workingText}
            </div>
          ` : ''}
        </div>

        <bashgpt-chat-info-panel
          class="${this._panels.infoOpen ? '' : 'collapsed'}"
          .context=${this._ctx.data}
          .settings=${this._ctx.settings}
          messageCount=${this._chat.messages.length}
          .commandStats=${this._commandStats}
          .tokenUsage=${this._chat.tokenUsage}
          ?loading=${this._ctx.loading}
        ></bashgpt-chat-info-panel>
      </div>

      <footer>
        ${this._toolPickerOpen ? html`
          <div class="tool-picker">
            <div class="tool-picker-title">🔧 Tools für diese Session</div>
            ${this._availableTools.length === 0
              ? html`<span style="font-size:12px;color:#64748b">Keine Tools verfügbar.</span>`
              : html`
                <div class="tool-picker-list">
                  ${this._availableTools.map(t => html`
                    <button
                      class="tool-chip ${this._enabledTools.includes(t.name) ? 'active' : ''}"
                      @click=${() => this._toggleTool(t.name)}
                      title=${t.description}
                    >${t.name}</button>
                  `)}
                </div>
              `}
          </div>
        ` : ''}

        <div class="input-row">
          <textarea
            placeholder=${this.readOnly
              ? 'Archivierte Session (nur lesen)'
              : 'Nachricht eingeben… (Cmd+Enter zum Senden)'}
            aria-label="Nachricht eingeben"
            @keydown=${this._onKeydown}
            ?disabled=${this._chat.loading || this.readOnly}
          ></textarea>
        </div>
        <div class="controls">
          ${!this.readOnly ? html`
            <button
              class="terminal-toggle ${this._toolPickerOpen || this._enabledTools.length > 0 ? 'active' : ''}"
              @click=${this._toggleToolPicker}
              title="Tools für diese Session konfigurieren"
              aria-pressed=${this._toolPickerOpen ? 'true' : 'false'}
            >🔧 Tools${this._enabledTools.length > 0 ? ` (${this._enabledTools.length})` : ''}</button>
          ` : ''}

          ${this.showTerminal ? html`
            <button
              class="terminal-toggle ${this._panels.terminalOpen ? 'active' : ''}"
              @click=${() => { this._panels = { ...this._panels, terminalOpen: !this._panels.terminalOpen } }}
              title="Terminal ein-/ausblenden"
              aria-pressed=${this._panels.terminalOpen ? 'true' : 'false'}
              aria-label="Terminal ein-/ausblenden"
            >⌃ Terminal</button>
          ` : ''}

          <button
            class="terminal-toggle ${this._panels.infoOpen ? 'active' : ''}"
            @click=${this._toggleInfo}
            title="Info-Panel ein-/ausblenden"
            aria-pressed=${this._panels.infoOpen ? 'true' : 'false'}
          >ℹ Info</button>

          <span
            class="status ${this._chat.statusError ? 'error' : ''}"
            aria-live="polite"
            aria-atomic="true"
          >
            ${this._chat.statusText}
          </span>

          <button
            class="primary"
            @click=${this._send}
            ?disabled=${this._chat.loading || this.readOnly}
            aria-label="Nachricht senden"
          >
            Senden
          </button>
        </div>
      </footer>
    `
  }
}
