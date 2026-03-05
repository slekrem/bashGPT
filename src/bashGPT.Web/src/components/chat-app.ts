import { LitElement, html } from 'lit'
import { customElement, state } from 'lit/decorators.js'
import './sidebar'
import './dashboard'
import './settings-view'
import './agents-view'
import './chat-view'
import { resetHistory, getSessions, getSession } from '../api'
import type { AppView, Session } from '../types'
import { LIVE_SESSION_ID, type SnapshotMessage } from '../session-history'
import { SessionManager } from '../session-manager'
import { chatAppStyles } from './chat-app.styles'

@customElement('bashgpt-app')
export class ChatApp extends LitElement {
  @state() private _view: AppView = 'dashboard'
  @state() private _sessions: Session[] = []
  @state() private _activeSessionId: string | null = null
  @state() private _pendingPrompt = ''
  @state() private _mobileMenuOpen = false
  @state() private _chatReadOnly = false

  private readonly _sm = new SessionManager()
  private _pollInterval: ReturnType<typeof setInterval> | null = null
  private _pollInFlight = false
  private _agentSessionUpdatedAt: string | null = null

  static styles = chatAppStyles

  async connectedCallback() {
    super.connectedCallback()
    const { sessions, activeId } = await this._sm.init()
    this._sessions = sessions
    this._activeSessionId = activeId
    await this.updateComplete
    if (activeId) await this._loadSessionIntoView(activeId)
    this._pollInterval = setInterval(() => this._pollAgentUpdates(), 5_000)
  }

  disconnectedCallback() {
    super.disconnectedCallback()
    if (this._pollInterval !== null) {
      clearInterval(this._pollInterval)
      this._pollInterval = null
    }
  }

  private async _pollAgentUpdates() {
    if (this._pollInFlight) return
    this._pollInFlight = true

    try {
      // Sessions-Liste aktualisieren
      const fresh = await getSessions()
      if (fresh) {
        // Nur updaten wenn sich etwas geändert hat (per updatedAt-Vergleich)
        const changed = fresh.some(f => {
          const existing = this._sessions.find(s => s.id === f.id)
          return !existing || existing.updatedAt !== f.updatedAt
        }) || fresh.length !== this._sessions.length
        if (changed) this._sessions = fresh
      }

      // Wenn die aktive Session eine Agent-Session ist, nur bei Änderung neu laden
      const id = this._activeSessionId
      if (!id?.startsWith('agent-') || this._view !== 'chat') return

      const sessionMeta = fresh?.find(s => s.id === id)
      if (!sessionMeta || (this._agentSessionUpdatedAt && sessionMeta.updatedAt <= this._agentSessionUpdatedAt)) return

      const data = await getSession(id)
      if (!data) return

      // Während des Fetches kann der User Session/View gewechselt haben.
      if (this._activeSessionId !== id || this._view !== 'chat') return

      this._agentSessionUpdatedAt = sessionMeta.updatedAt
      const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view') as any
      chatView?.refreshFromAgent?.(data.messages, data.shellContext)
    } finally {
      this._pollInFlight = false
    }
  }

  // ── Session helpers ───────────────────────────────────────────────────────

  private async _loadSessionIntoView(id: string) {
    const data = await this._sm.loadSession(id)
    if (!data) return
    const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view') as any
    if (!chatView) return
    chatView.readOnly = false
    if (data.isArchived) {
      const archivedId = id
      chatView.beforeSend = async () => {
        chatView.beforeSend = undefined
        await this._doActivateArchived(archivedId)
      }
      chatView.loadSnapshot?.(data.messages, data.shellContext,
        'Archivierte Session – Nachricht senden, um fortzufahren')
      chatView.scrollToBottom?.()
    } else {
      chatView.beforeSend = undefined
      chatView.loadSnapshot?.(data.messages, data.shellContext)
      chatView.scrollToBottom?.()
    }
  }

  private async _doActivateArchived(archivedId: string) {
    await resetHistory()
    const { sessions, activeId } = await this._sm.activateArchived(archivedId)
    this._sessions = sessions
    this._activeSessionId = activeId
  }

  private _ensureLiveSessionActive() {
    this._chatReadOnly = false
    if (this._sm.useFallback) {
      this._activeSessionId = LIVE_SESSION_ID
      const updated = this._sm.ensureLiveSession()
      if (updated.length > 0) this._sessions = updated
    }
  }

  // ── Event handlers ────────────────────────────────────────────────────────

  private async _onNewChat() {
    const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view') as any
    if (chatView) chatView.beforeSend = undefined
    const snapshot = (chatView?.getSnapshot?.() as SnapshotMessage[] | undefined) ?? []
    const liveCtx = this._sm.localSessions.find(s => s.id === LIVE_SESSION_ID)?.shellContext
    const { sessions, activeId } = await this._sm.prepareNewChat(snapshot, this._activeSessionId, liveCtx)
    this._sessions = sessions
    this._activeSessionId = activeId
    if (chatView) await chatView.reset()
    this._pendingPrompt = ''
    this._chatReadOnly = false
    this._view = 'chat'
    this._mobileMenuOpen = false
  }

  private _onViewChange(e: CustomEvent) {
    this._view = e.detail.view
    this._mobileMenuOpen = false
  }

  private async _onSessionSelect(e: CustomEvent<{ id: string }>) {
    this._activeSessionId = e.detail.id
    this._agentSessionUpdatedAt = null
    this._view = 'chat'
    this._mobileMenuOpen = false
    await this._loadSessionIntoView(e.detail.id)
  }

  private async _onPromptSelected(e: CustomEvent<{ prompt: string }>) {
    await this._onNewChat()
    this._ensureLiveSessionActive()
    this._view = 'chat'
    const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view') as any
    chatView?.setExecMode?.('auto-exec')
    this._pendingPrompt = ''
    requestAnimationFrame(() => { this._pendingPrompt = e.detail.prompt })
  }

  private _onPromptEdit(e: CustomEvent<{ prompt: string }>) {
    this._ensureLiveSessionActive()
    this._pendingPrompt = ''
    this._view = 'chat'
    requestAnimationFrame(() => {
      const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view')
      const ta = chatView?.shadowRoot?.querySelector('textarea') as HTMLTextAreaElement | null
      if (ta) { ta.value = e.detail.prompt; ta.focus() }
    })
  }

  private async _onClearHistory() {
    const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view') as any
    if (chatView) { chatView.beforeSend = undefined; await chatView.reset() }
    await this._sm.clearAll()
    this._sessions = []
    this._activeSessionId = null
  }

  private _onChatStarted() {
    this._chatReadOnly = false
    const activeId = this._activeSessionId
    if (!activeId) return
    if (!this._sessions.some(s => s.id === activeId)) {
      const now = new Date().toISOString()
      this._sessions = [{ id: activeId, title: 'Aktueller Chat', createdAt: now, updatedAt: now }, ...this._sessions]
    }
    if (this._sm.useFallback) this._activeSessionId = LIVE_SESSION_ID
  }

  private async _onMessagesChanged(e: CustomEvent<{ messages: SnapshotMessage[], shellContext?: any }>) {
    if (this._chatReadOnly) return
    const id = this._activeSessionId ?? LIVE_SESSION_ID
    if (!this._activeSessionId) this._activeSessionId = id
    this._sessions = await this._sm.persistMessages(id, e.detail.messages, e.detail.shellContext)
  }

  render() {
    return html`
      <header>
        <div class="logo" @click=${() => { this._view = 'dashboard'; this._mobileMenuOpen = false }}>
          <span class="logo-dot">●</span> bashGPT
        </div>
        <div class="header-actions">
          <button
            class="hamburger"
            @click=${() => { this._mobileMenuOpen = !this._mobileMenuOpen }}
            aria-label="Menü"
          >☰</button>
        </div>
      </header>

      ${this._mobileMenuOpen
        ? html`<div class="mobile-overlay" @click=${() => { this._mobileMenuOpen = false }}></div>`
        : ''}

      <div class="shell">
        <bashgpt-sidebar
          class="${this._mobileMenuOpen ? 'open' : ''}"
          view=${this._view}
          .sessions=${this._sessions}
          activeSessionId=${this._activeSessionId ?? ''}
          @new-chat=${this._onNewChat}
          @view-change=${this._onViewChange}
          @session-select=${this._onSessionSelect}
        ></bashgpt-sidebar>

        <div class="content">
          ${this._view === 'dashboard' ? html`
            <bashgpt-dashboard
              @prompt-selected=${this._onPromptSelected}
              @prompt-edit=${this._onPromptEdit}
            ></bashgpt-dashboard>
          ` : ''}

          ${this._view === 'settings' ? html`
            <bashgpt-settings-view
              @clear-history=${this._onClearHistory}
            ></bashgpt-settings-view>
          ` : ''}

          ${this._view === 'agents' ? html`
            <bashgpt-agents-view></bashgpt-agents-view>
          ` : ''}

          <bashgpt-chat-view
            style="display: ${this._view === 'chat' ? 'flex' : 'none'}; flex-direction: column; height: 100%;"
            pendingPrompt=${this._pendingPrompt}
            sessionId=${this._activeSessionId ?? ''}
            ?showTerminal=${true}
            ?active=${this._view === 'chat'}
            ?readOnly=${this._chatReadOnly}
            @chat-started=${this._onChatStarted}
            @messages-changed=${this._onMessagesChanged}
          ></bashgpt-chat-view>
        </div>
      </div>
    `
  }
}
