import { LitElement, html, css } from 'lit'
import { customElement, state } from 'lit/decorators.js'
import './sidebar'
import './dashboard'
import './settings-view'
import './chat-view'
import './terminal-panel'
import { loadHistory, resetHistory, getSessions, getSession, putSession, deleteSession, clearSessions, createSession } from '../api'
import type { AppView, Session, ShellContext } from '../types'
import {
  LIVE_SESSION_ID,
  createLiveSession,
  historyToSnapshot,
  readLocalSessions,
  upsertSession,
  writeLocalSessions,
  toSession,
  type LocalSession,
  type SnapshotMessage,
} from '../session-history'

@customElement('bashgpt-app')
export class ChatApp extends LitElement {
  @state() private _view: AppView = 'dashboard'
  @state() private _sessions: Session[] = []
  @state() private _activeSessionId: string | null = null
  @state() private _pendingPrompt = ''
  @state() private _mobileMenuOpen = false
  @state() private _chatReadOnly = false

  private _localSessions: LocalSession[] = []
  private _useLocalSessionsFallback = false

  static styles = css`
    /* ── CSS custom properties (cascade to all child components) ─────────── */
    :host {
      display: flex;
      flex-direction: column;
      height: 100dvh;
      font-family: ui-sans-serif, system-ui, sans-serif;
      background: radial-gradient(circle at top, #1e293b, #020617);
      color: #e5e7eb;
      --color-border: #374151;
      --color-user: #1f2937;
      --color-assistant: #0b1220;
      --color-text: #e5e7eb;
      --color-muted: #6b7280;
      --color-accent: #22c55e;
      --sidebar-width: 220px;
    }

    /* ── Shared header ───────────────────────────────────────────────────── */
    header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 12px 20px;
      border-bottom: 1px solid #1e293b;
      background: rgba(15, 23, 42, 0.9);
      backdrop-filter: blur(8px);
      flex-shrink: 0;
      z-index: 10;
    }
    .logo {
      font-size: 18px;
      font-weight: 700;
      color: #f1f5f9;
      display: flex;
      align-items: center;
      gap: 8px;
      cursor: pointer;
      user-select: none;
    }
    .logo-dot { color: var(--color-accent); }
    .header-actions { display: flex; gap: 8px; align-items: center; }

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

    /* ── Shell layout ─────────────────────────────────────────────────────── */
    .shell {
      display: flex;
      flex: 1;
      overflow: hidden;
    }
    .content {
      flex: 1;
      overflow: hidden;
      display: flex;
      flex-direction: column;
    }

    /* ── Mobile: sidebar overlay ─────────────────────────────────────────── */
    .mobile-overlay {
      display: none;
    }
    @media (max-width: 768px) {
      .mobile-overlay {
        display: block;
        position: fixed;
        inset: 0;
        background: rgba(0,0,0,0.5);
        z-index: 20;
      }
      bashgpt-sidebar {
        position: fixed;
        top: 0;
        left: 0;
        bottom: 0;
        z-index: 30;
        transform: translateX(-100%);
        transition: transform 0.2s ease;
        width: 260px !important;
      }
      bashgpt-sidebar.open {
        transform: translateX(0);
      }
      .hamburger { display: flex !important; }
    }
    .hamburger {
      display: none;
      background: none;
      border: none;
      color: #94a3b8;
      font-size: 20px;
      padding: 4px 8px;
      cursor: pointer;
    }
  `

  async connectedCallback() {
    super.connectedCallback()
    const serverSessions = await getSessions()
    // null = Endpunkt nicht erreichbar → localStorage-Fallback; [] = verfügbar aber leer → Server-Modus
    this._useLocalSessionsFallback = serverSessions === null

    if (!this._useLocalSessionsFallback) {
      // ── Server-Modus ─────────────────────────────────────────────────────
      await this._migrateLocalSessionsToServer()
      const freshSessions = await getSessions() ?? []
      if (freshSessions.length === 0) {
        const s = await createSession()
        if (s) this._activeSessionId = s.id
        this._sessions = await getSessions() ?? []
      } else {
        this._sessions = freshSessions
        this._activeSessionId = this._sessions[0].id
      }

      // Erste Session mit Messages in die chat-view laden
      await this.updateComplete
      if (this._activeSessionId) {
        await this._loadServerSessionIntoView(this._activeSessionId)
      }
    } else {
      // ── localStorage-Fallback (kein SessionStore auf Server) ─────────────
      this._localSessions = readLocalSessions()

      // First run fallback: import existing global server history once.
      if (!this._localSessions.some(s => s.id === LIVE_SESSION_ID)) {
        try {
          const history = await loadHistory()
          if (history.length > 0) {
            this._localSessions.unshift(createLiveSession(historyToSnapshot(history)))
          }
        } catch {
          // Ignore API error; UI still works in empty state.
        }
      }

      this._sessions = this._localSessions.map(toSession)
      if (this._sessions.length > 0) {
        this._activeSessionId = this._sessions[0].id
        this._chatReadOnly = false
      }
      writeLocalSessions(this._localSessions)

      // Initiale Session sofort in die chat-view laden, damit commands nach
      // einem Reload sichtbar sind – noch bevor _loadHistory() vom Server
      // zurückkommt (der keine commands kennt).
      await this.updateComplete
      const initialId = this._activeSessionId ?? LIVE_SESSION_ID
      const initialSession = this._localSessions.find(s => s.id === initialId)
      if (initialSession && initialSession.messages.length > 0) {
        const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view') as any
        if (chatView) {
          chatView.readOnly = false
          if (initialId !== LIVE_SESSION_ID) {
            const archivedId = initialId
            chatView.beforeSend = async () => {
              chatView.beforeSend = undefined
              await this._activateArchivedSession(archivedId)
            }
            chatView.loadSnapshot?.(initialSession.messages, initialSession.shellContext,
              'Archivierte Session – Nachricht senden, um fortzufahren')
          } else {
            chatView.loadSnapshot?.(initialSession.messages, initialSession.shellContext)
          }
        }
      }
    }
  }

  private _currentSession(): Session {
    return {
      id: LIVE_SESSION_ID,
      title: 'Aktueller Chat',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
  }

  // ── Server-Session-Hilfsmethoden ─────────────────────────────────────────

  /** Einmalige Migration: localStorage-Sessions per API auf den Server hochladen. */
  private async _migrateLocalSessionsToServer() {
    const local = readLocalSessions()
    if (local.length === 0) return
    // Nur migrieren wenn der Server noch keine eigenen Sessions hat
    const existing = await getSessions() ?? []
    if (existing.length > 0) {
      // Server hat bereits Daten – localStorage bereinigen
      writeLocalSessions([])
      return
    }
    for (const s of local) {
      await putSession(s.id, {
        title: s.title,
        messages: s.messages,
        shellContext: s.shellContext,
        createdAt: s.createdAt,
      })
    }
    writeLocalSessions([])
  }

  /** Lädt eine Server-Session und befüllt die chat-view. */
  private async _loadServerSessionIntoView(id: string) {
    const session = await getSession(id)
    if (!session) return
    const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view') as any
    if (!chatView) return
    chatView.readOnly = false
    chatView.loadSnapshot?.(session.messages ?? [], session.shellContext ?? null)
  }

  // ── Event handlers ────────────────────────────────────────────────────────

  private async _onNewChat() {
    const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view') as any
    if (chatView) chatView.beforeSend = undefined

    if (this._useLocalSessionsFallback && chatView) {
      const snapshot = chatView.getSnapshot?.() as SnapshotMessage[] | undefined
      if (snapshot && snapshot.length > 0 && this._activeSessionId === LIVE_SESSION_ID) {
        const archivedId = `s-${Date.now()}`
        // ShellContext der Live-Session beim Archivieren mitübernehmen
        const liveShellContext = this._localSessions.find(s => s.id === LIVE_SESSION_ID)?.shellContext
        this._localSessions = upsertSession(this._localSessions, archivedId, snapshot, liveShellContext)
      }
    } else if (!this._useLocalSessionsFallback && chatView) {
      // Server-Modus: leere Session aufräumen, dann neue anlegen
      const snapshot = chatView.getSnapshot?.() as SnapshotMessage[] | undefined
      if ((!snapshot || snapshot.length === 0) && this._activeSessionId) {
        await deleteSession(this._activeSessionId)
      }
      const newSession = await createSession()
      if (newSession) this._activeSessionId = newSession.id
    }

    if (chatView) await chatView.reset()
    this._pendingPrompt = ''
    this._chatReadOnly = false

    if (this._useLocalSessionsFallback) {
      this._localSessions = upsertSession(this._localSessions, LIVE_SESSION_ID, [])
      writeLocalSessions(this._localSessions)
      this._sessions = this._localSessions.map(toSession)
    } else {
      this._sessions = await getSessions() ?? []
    }

    this._view = 'chat'
    this._mobileMenuOpen = false
  }

  private _onViewChange(e: CustomEvent) {
    this._view = e.detail.view
    this._mobileMenuOpen = false
  }

  private async _onSessionSelect(e: CustomEvent<{ id: string }>) {
    this._activeSessionId = e.detail.id
    this._view = 'chat'
    this._mobileMenuOpen = false

    if (this._useLocalSessionsFallback) {
      this._chatReadOnly = false
      const selected = this._localSessions.find(s => s.id === e.detail.id)
      const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view') as any
      if (selected && chatView) {
        chatView.readOnly = false
        if (e.detail.id !== LIVE_SESSION_ID) {
          const archivedId = e.detail.id
          chatView.beforeSend = async () => {
            chatView.beforeSend = undefined
            await this._activateArchivedSession(archivedId)
          }
          chatView.loadSnapshot?.(selected.messages ?? [], selected.shellContext,
            'Archivierte Session – Nachricht senden, um fortzufahren')
        } else {
          chatView.beforeSend = undefined
          chatView.loadSnapshot?.(selected.messages ?? [], selected.shellContext)
        }
      }
      return
    }

    // Server-Modus: Session-Inhalt vom Server laden (auch für 'current')
    await this._loadServerSessionIntoView(e.detail.id)
  }

  private _ensureLiveSessionActive() {
    this._chatReadOnly = false
    if (this._useLocalSessionsFallback) {
      this._activeSessionId = LIVE_SESSION_ID
      if (!this._localSessions.some(s => s.id === LIVE_SESSION_ID))
        this._localSessions = upsertSession(this._localSessions, LIVE_SESSION_ID, [])
      writeLocalSessions(this._localSessions)
      this._sessions = this._localSessions.map(toSession)
    }
    // Server-Modus: _activeSessionId bereits korrekt – nichts tun
  }

  private async _activateArchivedSession(archivedId: string) {
    await resetHistory()

    if (!this._localSessions.some(s => s.id === archivedId)) return

    // Bestehende Live-Session behandeln: leere entfernen, belegte archivieren –
    // verhindert doppelten 'current'-Eintrag in der Sidebar.
    const existingLive = this._localSessions.find(s => s.id === LIVE_SESSION_ID)
    let sessions = this._localSessions.filter(s => s.id !== LIVE_SESSION_ID)
    if (existingLive && existingLive.messages.length > 0) {
      sessions = [...sessions, { ...existingLive, id: `s-${Date.now()}`, isLive: false }]
    }

    // Archivierten Eintrag zur Live-Session promoten und an die Spitze sortieren.
    const now = new Date().toISOString()
    this._localSessions = sessions
      .map(s => s.id === archivedId ? { ...s, id: LIVE_SESSION_ID, isLive: true, updatedAt: now } : s)
      .sort((a, b) => b.updatedAt.localeCompare(a.updatedAt))

    writeLocalSessions(this._localSessions)
    this._sessions = this._localSessions.map(toSession)
    this._activeSessionId = LIVE_SESSION_ID
  }

  private async _onPromptSelected(e: CustomEvent<{ prompt: string }>) {
    // Dashboard-"Ausführen" startet immer eine neue Konversation.
    await this._onNewChat()
    this._ensureLiveSessionActive()
    this._view = 'chat'
    const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view') as any
    chatView?.setExecMode?.('auto-exec')
    // Re-trigger auch bei identischem Prompt-Text.
    this._pendingPrompt = ''
    requestAnimationFrame(() => {
      this._pendingPrompt = e.detail.prompt
    })
  }

  private _onPromptEdit(e: CustomEvent<{ prompt: string }>) {
    this._ensureLiveSessionActive()
    // Set prompt in textarea without sending
    this._pendingPrompt = ''
    this._view = 'chat'
    requestAnimationFrame(() => {
      const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view')
      if (chatView) {
        const ta = chatView.shadowRoot?.querySelector('textarea') as HTMLTextAreaElement | null
        if (ta) { ta.value = e.detail.prompt; ta.focus() }
      }
    })
  }

  private async _onClearHistory() {
    const chatView = this.shadowRoot?.querySelector('bashgpt-chat-view') as any
    if (chatView) chatView.beforeSend = undefined
    if (chatView) await chatView.reset()
    if (this._useLocalSessionsFallback) {
      this._localSessions = []
      writeLocalSessions(this._localSessions)
    } else {
      await clearSessions()
    }
    this._sessions = []
    this._activeSessionId = null
  }

  private _onChatStarted() {
    this._chatReadOnly = false
    if (this._useLocalSessionsFallback) {
      if (!this._sessions.some(s => s.id === LIVE_SESSION_ID))
        this._sessions = [this._currentSession(), ...this._sessions]
      this._activeSessionId = LIVE_SESSION_ID
    } else {
      // Server-Modus: _activeSessionId ist bereits korrekt gesetzt
      if (this._activeSessionId && !this._sessions.some(s => s.id === this._activeSessionId))
        this._sessions = [this._currentSession(), ...this._sessions]
    }
  }

  private _onMessagesChanged(e: CustomEvent<{ messages: SnapshotMessage[], shellContext?: ShellContext | null }>) {
    if (this._chatReadOnly) return

    const targetSessionId = this._activeSessionId ?? LIVE_SESSION_ID
    if (!this._activeSessionId) {
      this._activeSessionId = targetSessionId
      this._chatReadOnly = false
    }

    if (this._useLocalSessionsFallback) {
      this._localSessions = upsertSession(this._localSessions, targetSessionId, e.detail.messages, e.detail.shellContext)
      writeLocalSessions(this._localSessions)
      this._sessions = this._localSessions.map(toSession)
    } else {
      // Server-Modus: POST /api/chat persistiert bereits mit korrektem title/createdAt.
      // Nur die Sidebar-Liste aktualisieren.
      getSessions().then(s => { this._sessions = s ?? [] })
    }
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
