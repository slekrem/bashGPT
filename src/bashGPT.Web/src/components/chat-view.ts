import { LitElement, html, css } from 'lit'
import { customElement, state, property } from 'lit/decorators.js'
import { repeat } from 'lit/directives/repeat.js'
import './message-bubble'
import './tool-calls-panel'
import './chat-info-panel'
import { streamChat, loadHistory, resetHistory, getContext, getSettings, getTools, cancelChat } from '../api'
import type { CommandResult, FullShellContext, Settings, ToolCallEntry, ShellContext, TokenUsage, ToolInfo } from '../types'
import type { SnapshotMessage } from '../session-history'

interface Message {
  id: number
  role: 'user' | 'assistant'
  content: string
  commands?: CommandResult[]
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
  /** One-shot Hook: wird vor dem ersten sendChat() der Session aufgerufen */
  @property({ attribute: false }) beforeSend?: () => Promise<void>
  /** Session-ID für server-seitige Persistenz (optional) */
  @property() sessionId = ''
  /** Agent-ID für agenten-spezifischen System-Prompt und Tools (optional) */
  @property() agentId = ''

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
    toolCallsOpen: true,
    infoOpen:      false,
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
  @state() private _streamingEntries: ToolCallEntry[] = []
  @state() private _panelSizes = { toolCalls: 360, info: 320 }
  @state() private _enabledTools: string[] = []
  @state() private _toolPickerOpen = false
  @state() private _availableTools: ToolInfo[] = []
  private _activeRequestId: string | null = null
  @state() private _cancelRequested = false
  private _resizeState: {
    type: 'toolCalls' | 'info'
    startX: number
    startToolCalls: number
    startInfo: number
    containerWidth: number
  } | null = null
  private readonly _layoutStorageKey = 'bashgpt_chat_layout_v1'
  private readonly _handleWidth = 6
  private readonly _minToolCallsWidth = 240
  private readonly _minChatWidth = 420
  private readonly _minInfoWidth = 260

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
      display: grid;
      overflow: hidden;
      align-items: stretch;
    }

    bashgpt-tool-calls-panel {
      min-width: 0;
      height: 100%;
    }

    .chat-column {
      min-width: 0;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    .resize-handle {
      width: 6px;
      cursor: col-resize;
      background: #0f172a;
      border-left: 1px solid #1e293b;
      border-right: 1px solid #1e293b;
      transition: background 0.15s;
      user-select: none;
      touch-action: none;
    }
    .resize-handle:hover,
    .resize-handle:focus-visible {
      background: #1e293b;
      outline: none;
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
    button.cancel {
      background: #3f1d1d;
      border-color: #7f1d1d;
      color: #fecaca;
      font-weight: 600;
    }
    button.cancel:hover:not(:disabled) { background: #5f1d1d; }

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
      min-width: 0;
      height: 100%;
      overflow: hidden;
      border-left: 1px solid #1e293b;
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
      .split-wrapper { display: flex; flex-direction: column; }
      .resize-handle { display: none; }
      bashgpt-tool-calls-panel { height: auto; border-right: none; border-bottom: 1px solid #1e293b; }
      bashgpt-chat-info-panel {
        border-left: none;
        border-top: 1px solid #1e293b;
      }
    }
  `

  async connectedCallback() {
    super.connectedCallback()
    this._loadPanelSizes()
    await this._loadHistory()
  }

  disconnectedCallback() {
    super.disconnectedCallback()
    this._stopResize()
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
      statusText: hint ?? '',
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
    this._activeRequestId = this._createRequestId()
    this._cancelRequested = false
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
            { toolName: data.name || 'tool', command: data.command, output: '', exitCode: -1, wasExecuted: false, status: 'running' },
          ]
        },
        onCommandResult: data => {
          let updated = false
          this._streamingEntries = this._streamingEntries.map(e => {
            if (!updated && e.command === data.command && e.status === 'running') {
              updated = true
              const explicitStatus = data.status ?? ''
              const status: ToolCallEntry['status'] =
                explicitStatus === 'timeout' || explicitStatus === 'user_cancelled' || explicitStatus === 'success' || explicitStatus === 'error' || explicitStatus === 'skipped'
                  ? explicitStatus
                  : (!data.wasExecuted ? 'skipped' : data.exitCode === 0 ? 'success' : 'error')
              return {
                toolName: e.toolName,
                command: data.command,
                output: data.output ?? '',
                exitCode: data.exitCode,
                wasExecuted: data.wasExecuted,
                status,
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
      }, this.sessionId || undefined, this._enabledTools.length ? this._enabledTools : undefined, this.agentId || undefined, this._activeRequestId || undefined)

      // Finale Message mit vollständigen Daten übernehmen
      const streamedCommands: CommandResult[] = this._streamingEntries
        .map(e => ({
          command: e.command,
          exitCode: e.status === 'running' ? -1 : e.exitCode,
          output: e.output ?? '',
          wasExecuted: e.status === 'running' ? false : e.wasExecuted,
        }))

      const finalCommands = (result.commands?.length ?? 0) > 0
        ? result.commands
        : streamedCommands
      const finalResponseText = result.finalStatus === 'user_cancelled'
        ? (this._streamingContent || result.response || 'Vom Nutzer abgebrochen.')
        : result.response

      const newMessages = this._chat.messages.map(m =>
        m.id === assistantId
          ? {
              ...m,
              content: finalResponseText,
              usage: result.usage ?? undefined,
              commands: finalCommands,
            }
          : m
      )
      const finalStatusText = result.finalStatus === 'user_cancelled'
        ? 'Vom Nutzer abgebrochen'
        : result.finalStatus === 'timeout'
          ? 'Timeout'
          : (this._enabledTools.length ? `Tools: ${this._enabledTools.join(', ')}` : '')
      this._chat = {
        ...this._chat,
        messages:    newMessages,
        tokenUsage:  this._sumTokenUsage(newMessages),
        shellContext: result.shellContext ?? this._chat.shellContext,
        statusText:  finalStatusText,
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
      this._activeRequestId = null
      this._cancelRequested = false
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

  private _createRequestId(): string {
    const c = globalThis.crypto as Crypto | undefined
    if (c && typeof c.randomUUID === 'function')
      return c.randomUUID()
    return `req_${Date.now()}_${Math.random().toString(16).slice(2)}`
  }

  private async _cancelRun() {
    if (!this._activeRequestId || this._cancelRequested) return
    this._cancelRequested = true
    this._chat = { ...this._chat, statusText: 'Abbruch angefordert…', statusError: false }

    try {
      await cancelChat(this._activeRequestId)
    } catch (e) {
      this._chat = {
        ...this._chat,
        statusText: `Fehler beim Abbrechen: ${e instanceof Error ? e.message : String(e)}`,
        statusError: true,
      }
    }
  }

  private _onKeydown(e: KeyboardEvent) {
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

  /** Aggregiert alle CommandResults aus allen Nachrichten als ToolCallEntries */
  private _toolCallEntries(messages: Message[]): ToolCallEntry[] {
    const entries: ToolCallEntry[] = []
    for (const msg of messages) {
      if (msg.role !== 'assistant' || !msg.commands?.length) continue
      for (const cmd of msg.commands) {
        entries.push({
          toolName: 'shell_exec',
          command: cmd.command,
          output: cmd.output,
          exitCode: cmd.exitCode,
          wasExecuted: cmd.wasExecuted,
          status: this._commandStatus(cmd),
        })
      }
    }
    return entries
  }

  private _commandStatus(cmd: CommandResult): ToolCallEntry['status'] {
    if (!cmd.wasExecuted) return 'skipped'
    if ((cmd.output ?? '').toLowerCase().includes('timed out')) return 'timeout'
    return cmd.exitCode === 0 ? 'success' : 'error'
  }

  private _loadPanelSizes() {
    try {
      const raw = localStorage.getItem(this._layoutStorageKey)
      if (!raw) return
      const parsed = JSON.parse(raw) as { toolCalls?: number; info?: number }
      if (typeof parsed.toolCalls === 'number' && Number.isFinite(parsed.toolCalls))
        this._panelSizes.toolCalls = Math.round(parsed.toolCalls)
      if (typeof parsed.info === 'number' && Number.isFinite(parsed.info))
        this._panelSizes.info = Math.round(parsed.info)
    } catch {
      // ignore invalid layout cache
    }
  }

  private _savePanelSizes() {
    localStorage.setItem(this._layoutStorageKey, JSON.stringify(this._panelSizes))
  }

  private _clamp(v: number, min: number, max: number) {
    return Math.max(min, Math.min(max, v))
  }

  private _startResize(type: 'toolCalls' | 'info', ev: PointerEvent) {
    ev.preventDefault()
    const container = this.shadowRoot?.querySelector('.split-wrapper') as HTMLElement | null
    if (!container) return
    const bounds = container.getBoundingClientRect()
    this._resizeState = {
      type,
      startX: ev.clientX,
      startToolCalls: this._panelSizes.toolCalls,
      startInfo: this._panelSizes.info,
      containerWidth: bounds.width,
    }
    window.addEventListener('pointermove', this._onResizeMove)
    window.addEventListener('pointerup', this._onResizeUp, { once: true })
  }

  private _stopResize() {
    this._resizeState = null
    window.removeEventListener('pointermove', this._onResizeMove)
  }

  private _onResizeUp = () => {
    this._savePanelSizes()
    this._stopResize()
  }

  private _onResizeMove = (ev: PointerEvent) => {
    const state = this._resizeState
    if (!state) return

    const showToolCalls = this.showTerminal && this._panels.toolCallsOpen
    const showInfo = this._panels.infoOpen
    const handles = (showToolCalls ? 1 : 0) + (showInfo ? 1 : 0)
    const availableWidth = state.containerWidth - handles * this._handleWidth

    const deltaX = ev.clientX - state.startX
    let toolCalls = state.startToolCalls
    let info = state.startInfo

    if (state.type === 'toolCalls') {
      const maxToolCalls = availableWidth - (showInfo ? info : 0) - this._minChatWidth
      toolCalls = this._clamp(state.startToolCalls + deltaX, this._minToolCallsWidth, Math.max(this._minToolCallsWidth, maxToolCalls))
      this._panelSizes = { ...this._panelSizes, toolCalls: Math.round(toolCalls) }
      return
    }

    const maxInfo = availableWidth - (showToolCalls ? toolCalls : 0) - this._minChatWidth
    info = this._clamp(state.startInfo - deltaX, this._minInfoWidth, Math.max(this._minInfoWidth, maxInfo))
    this._panelSizes = { ...this._panelSizes, info: Math.round(info) }
  }

  private _resizeByKeyboard(type: 'toolCalls' | 'info', ev: KeyboardEvent) {
    const step = ev.shiftKey ? 40 : 16
    const isLeft = ev.key === 'ArrowLeft'
    const isRight = ev.key === 'ArrowRight'
    if (!isLeft && !isRight) return

    ev.preventDefault()
    const dir = isRight ? 1 : -1
    const current = type === 'toolCalls' ? this._panelSizes.toolCalls : this._panelSizes.info
    const min = type === 'toolCalls' ? this._minToolCallsWidth : this._minInfoWidth
    const next = Math.max(min, current + dir * step)
    if (type === 'toolCalls')
      this._panelSizes = { ...this._panelSizes, toolCalls: next }
    else
      this._panelSizes = { ...this._panelSizes, info: next }
    this._savePanelSizes()
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

  render() {
    const isEmpty = this._chat.messages.length === 0
    const workingText = this._workingText()
    const showToolCalls = this.showTerminal && this._panels.toolCallsOpen
    const showInfo = this._panels.infoOpen
    const stableEntries = this._toolCallEntries(this._chat.messages)
    const layoutColumns = [
      ...(showToolCalls ? [`${this._panelSizes.toolCalls}px`, `${this._handleWidth}px`] : []),
      'minmax(0, 1fr)',
      ...(showInfo ? [`${this._handleWidth}px`, `${this._panelSizes.info}px`] : []),
    ].join(' ')

    return html`
      <div class="split-wrapper" style=${`grid-template-columns: ${layoutColumns};`}>
        ${showToolCalls ? html`
          <bashgpt-tool-calls-panel
            .entries=${[...stableEntries, ...this._streamingEntries]}
            ?loading=${this._chat.loading}
          ></bashgpt-tool-calls-panel>

          <div
            class="resize-handle"
            role="separator"
            aria-label="Breite Tool-Calls anpassen"
            aria-orientation="vertical"
            tabindex="0"
            @pointerdown=${(ev: PointerEvent) => this._startResize('toolCalls', ev)}
            @keydown=${(ev: KeyboardEvent) => this._resizeByKeyboard('toolCalls', ev)}
          ></div>
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

        ${showInfo ? html`
          <div
            class="resize-handle"
            role="separator"
            aria-label="Breite Info-Panel anpassen"
            aria-orientation="vertical"
            tabindex="0"
            @pointerdown=${(ev: PointerEvent) => this._startResize('info', ev)}
            @keydown=${(ev: KeyboardEvent) => this._resizeByKeyboard('info', ev)}
          ></div>
          <bashgpt-chat-info-panel
            .context=${this._ctx.data}
            .settings=${this._ctx.settings}
            messageCount=${this._chat.messages.length}
            .commandStats=${this._commandStats}
            .tokenUsage=${this._chat.tokenUsage}
            ?loading=${this._ctx.loading}
          ></bashgpt-chat-info-panel>
        ` : ''}
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
            placeholder="Nachricht eingeben… (Cmd+Enter zum Senden)"
            aria-label="Nachricht eingeben"
            @keydown=${this._onKeydown}
            ?disabled=${this._chat.loading}
          ></textarea>
        </div>
        <div class="controls">
          <button
            class="terminal-toggle ${this._toolPickerOpen || this._enabledTools.length > 0 ? 'active' : ''}"
            @click=${this._toggleToolPicker}
            title="Tools für diese Session konfigurieren"
            aria-pressed=${this._toolPickerOpen ? 'true' : 'false'}
          >🔧 Tools${this._enabledTools.length > 0 ? ` (${this._enabledTools.length})` : ''}</button>

          ${this.showTerminal ? html`
            <button
              class="terminal-toggle ${this._panels.toolCallsOpen ? 'active' : ''}"
              @click=${() => { this._panels = { ...this._panels, toolCallsOpen: !this._panels.toolCallsOpen } }}
              title="Tool-Calls ein-/ausblenden"
              aria-pressed=${this._panels.toolCallsOpen ? 'true' : 'false'}
              aria-label="Tool-Calls ein-/ausblenden"
            >Tool Calls</button>
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

          ${this._chat.loading ? html`
            <button
              class="cancel"
              @click=${this._cancelRun}
              ?disabled=${this._cancelRequested}
              aria-label="Laufenden Tool-Call abbrechen"
            >
              ${this._cancelRequested ? 'Abbrechen…' : 'Abbrechen'}
            </button>
          ` : ''}

          <button
            class="primary"
            @click=${this._send}
            ?disabled=${this._chat.loading}
            aria-label="Nachricht senden"
          >
            Senden
          </button>
        </div>
      </footer>
    `
  }
}




