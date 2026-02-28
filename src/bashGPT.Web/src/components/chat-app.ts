import { LitElement, html, css } from 'lit'
import { customElement, state } from 'lit/decorators.js'
import { repeat } from 'lit/directives/repeat.js'
import './message-bubble'
import { sendChat, loadHistory, resetHistory } from '../api'
import type { ExecMode, CommandResult } from '../types'

interface Message {
  id: number
  role: 'user' | 'assistant'
  content: string
  commands?: CommandResult[]
  usedToolCalls?: boolean
}

@customElement('bashgpt-app')
export class ChatApp extends LitElement {
  @state() private _messages: Message[] = []
  @state() private _loading = false
  @state() private _status = ''
  @state() private _statusError = false
  @state() private _mode: ExecMode = 'ask'
  private _idCounter = 0

  static styles = css`
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
    }

    header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 12px 20px;
      border-bottom: 1px solid #1e293b;
      background: rgba(15, 23, 42, 0.8);
      backdrop-filter: blur(8px);
      flex-shrink: 0;
    }

    .logo {
      font-size: 18px;
      font-weight: 700;
      color: #f1f5f9;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .logo-dot { color: var(--color-accent); }

    .header-actions { display: flex; gap: 8px; align-items: center; }

    #chat {
      flex: 1;
      overflow-y: auto;
      padding: 20px;
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
    .empty-state .icon { font-size: 48px; }
    .empty-state p { font-size: 16px; }

    footer {
      padding: 12px 20px 20px;
      border-top: 1px solid #1e293b;
      background: rgba(15, 23, 42, 0.8);
      backdrop-filter: blur(8px);
      flex-shrink: 0;
    }

    .input-row {
      display: flex;
      gap: 8px;
      align-items: flex-end;
    }

    textarea {
      flex: 1;
      min-height: 56px;
      max-height: 200px;
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
    textarea::placeholder { color: #4b5563; }

    .controls {
      display: flex;
      gap: 8px;
      margin-top: 8px;
      align-items: center;
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

    button {
      background: #111827;
      color: #e5e7eb;
      border: 1px solid #374151;
      border-radius: 8px;
      padding: 7px 14px;
      font-size: 13px;
      cursor: pointer;
      transition: background 0.15s, border-color 0.15s;
    }
    button:hover { background: #1f2937; border-color: #4b5563; }
    button:disabled { opacity: 0.4; cursor: not-allowed; }

    button.primary {
      background: #14532d;
      border-color: #16a34a;
      color: #dcfce7;
      font-weight: 600;
      padding: 7px 20px;
    }
    button.primary:hover:not(:disabled) { background: #166534; }

    .status {
      font-size: 12px;
      color: #4b5563;
      flex: 1;
      text-align: right;
    }
    .status.loading { color: #6b7280; }
    .status.error { color: #ef4444; }

    .spinner {
      display: inline-block;
      width: 12px;
      height: 12px;
      border: 2px solid #374151;
      border-top-color: var(--color-accent);
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
      vertical-align: middle;
      margin-right: 4px;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
  `

  async connectedCallback() {
    super.connectedCallback()
    await this._loadHistory()
  }

  private async _loadHistory() {
    try {
      const history = await loadHistory()
      this._messages = history.map(m => ({
        id: this._idCounter++,
        role: m.role,
        content: m.content,
      }))
      this._statusError = false
    } catch (e) {
      this._statusError = true
      this._status = `Fehler: ${e instanceof Error ? e.message : String(e)}`
    }
  }

  private async _send() {
    const textarea = this.shadowRoot!.querySelector('textarea')!
    const prompt = textarea.value.trim()
    if (!prompt || this._loading) return

    textarea.value = ''
    this._messages = [
      ...this._messages,
      { id: this._idCounter++, role: 'user', content: prompt },
    ]
    this._loading = true
    this._status = ''
    this._statusError = false
    this._scrollToBottom()

    try {
      const result = await sendChat(prompt, this._mode)
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
        parts.push(`${result.commands.length} Befehle`)
      this._status = parts.join(' · ')
      this._statusError = false
    } catch (e) {
      this._status = `Fehler: ${e instanceof Error ? e.message : String(e)}`
      this._statusError = true
      this._messages = [
        ...this._messages,
        { id: this._idCounter++, role: 'assistant', content: `⚠️ ${this._status}` },
      ]
    } finally {
      this._loading = false
      this._scrollToBottom()
    }
  }

  private async _reset() {
    try {
      await resetHistory()
      this._messages = []
      this._status = 'Verlauf gelöscht'
      this._statusError = false
    } catch (e) {
      this._status = `Fehler: ${e instanceof Error ? e.message : String(e)}`
      this._statusError = true
    }
  }

  private _scrollToBottom() {
    requestAnimationFrame(() => {
      const chat = this.shadowRoot?.querySelector('#chat')
      if (chat) chat.scrollTop = chat.scrollHeight
    })
  }

  private _onKeydown(e: KeyboardEvent) {
    if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
      e.preventDefault()
      this._send()
    }
  }

  render() {
    const isEmpty = this._messages.length === 0

    return html`
      <header>
        <div class="logo">
          <span class="logo-dot">●</span> bashGPT
        </div>
        <div class="header-actions">
          <button @click=${this._reset} ?disabled=${this._loading}>
            Verlauf löschen
          </button>
        </div>
      </header>

      <div id="chat">
        ${isEmpty
          ? html`
              <div class="empty-state">
                <div class="icon">⌨️</div>
                <p>Stell mir eine Frage oder gib einen Shell-Befehl ein.</p>
              </div>
            `
          : repeat(
              this._messages,
              m => m.id,
              m => html`
                <bashgpt-message
                  role=${m.role}
                  content=${m.content}
                  .commands=${m.commands ?? []}
                  ?usedToolCalls=${m.usedToolCalls ?? false}
                ></bashgpt-message>
              `
            )}
      </div>

      <footer>
        <div class="input-row">
          <textarea
            placeholder="Nachricht eingeben… (Cmd+Enter zum Senden)"
            @keydown=${this._onKeydown}
            ?disabled=${this._loading}
          ></textarea>
        </div>
        <div class="controls">
          <select
            .value=${this._mode}
            @change=${(e: Event) => {
              this._mode = (e.target as HTMLSelectElement).value as ExecMode
            }}
            ?disabled=${this._loading}
          >
            <option value="ask">ask</option>
            <option value="dry-run">dry-run</option>
            <option value="auto-exec">auto-exec</option>
            <option value="no-exec">no-exec</option>
          </select>
          <span class="status ${this._loading ? 'loading' : ''} ${this._statusError ? 'error' : ''}">
            ${this._loading
              ? html`<span class="spinner"></span> Denke...`
              : this._status}
          </span>
          <button
            class="primary"
            @click=${this._send}
            ?disabled=${this._loading}
          >
            Senden
          </button>
        </div>
      </footer>
    `
  }
}
