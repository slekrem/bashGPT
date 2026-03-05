import { LitElement, html, css } from 'lit'
import { customElement, property } from 'lit/decorators.js'
import { repeat } from 'lit/directives/repeat.js'
import type { AppView, Session } from '../types'

@customElement('bashgpt-sidebar')
export class Sidebar extends LitElement {
  @property() view: AppView = 'dashboard'
  @property({ type: Array }) sessions: Session[] = []
  @property() activeSessionId: string | null = null
  @property({ type: Boolean }) loading = false

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      width: var(--sidebar-width, 220px);
      background: rgba(11, 17, 32, 0.95);
      border-right: 1px solid #1e293b;
      flex-shrink: 0;
      overflow: hidden;
    }

    .section-label {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.08em;
      color: #475569;
      text-transform: uppercase;
      padding: 16px 14px 6px;
    }

    .session-list {
      flex: 1;
      overflow-y: auto;
      padding: 0 8px;
    }

    .session-item {
      border-radius: 8px;
      padding: 8px 10px;
      cursor: pointer;
      transition: background 0.12s;
      border: 1px solid transparent;
      background: none;
      width: 100%;
      text-align: left;
      font-family: inherit;
      color: inherit;
    }
    .session-item:hover { background: #1e293b; }
    .session-item:focus-visible {
      outline: 2px solid #22c55e;
      outline-offset: 1px;
    }
    .session-item.active {
      background: #0f2d1a;
      border-color: #166534;
    }
    .session-title {
      font-size: 13px;
      font-weight: 500;
      color: #e2e8f0;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .session-date {
      font-size: 11px;
      color: #475569;
      margin-top: 2px;
    }

    .empty-sessions {
      padding: 12px 14px;
      font-size: 12px;
      color: #475569;
      font-style: italic;
    }

    .divider {
      height: 1px;
      background: #1e293b;
      margin: 8px 14px;
    }

    .nav-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 9px 14px;
      margin: 2px 8px;
      border-radius: 8px;
      cursor: pointer;
      font-size: 13px;
      color: #94a3b8;
      background: none;
      border: none;
      width: calc(100% - 16px);
      text-align: left;
      transition: background 0.12s, color 0.12s;
    }
    .nav-btn:hover { background: #1e293b; color: #e2e8f0; }
    .nav-btn:focus-visible { outline: 2px solid #22c55e; outline-offset: 1px; }
    .nav-btn.active { background: #1e293b; color: #f1f5f9; font-weight: 600; }
    .nav-btn .icon { font-size: 15px; }

    .new-chat-btn {
      margin: 12px 8px 4px;
      padding: 8px 12px;
      border-radius: 8px;
      background: #14532d;
      border: 1px solid #16a34a;
      color: #dcfce7;
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 6px;
      transition: background 0.12s;
    }
    .new-chat-btn:hover { background: #166534; }
    .new-chat-btn:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
    .new-chat-btn:disabled { opacity: 0.4; cursor: not-allowed; }

    @media (max-width: 768px) {
      :host { width: 100%; border-right: none; border-bottom: 1px solid #1e293b; }
    }
  `

  private _dispatch(event: string, detail?: object) {
    this.dispatchEvent(new CustomEvent(event, { detail, bubbles: true, composed: true }))
  }

  private _formatDate(iso: string) {
    try {
      return new Date(iso).toLocaleDateString('de-DE', { day: '2-digit', month: 'short' })
    } catch {
      return ''
    }
  }

  render() {
    return html`
      <button
        class="new-chat-btn"
        ?disabled=${this.loading}
        @click=${() => this._dispatch('new-chat')}
      >
        + Neuer Chat
      </button>

      <div class="section-label">Verlauf</div>

      <div class="session-list">
        ${this.sessions.length === 0
          ? html`<div class="empty-sessions">Noch keine Sessions</div>`
          : repeat(
              this.sessions,
              s => s.id,
              s => html`
                <button
                  class="session-item ${s.id === this.activeSessionId ? 'active' : ''}"
                  @click=${() => this._dispatch('session-select', { id: s.id })}
                  aria-current=${s.id === this.activeSessionId ? 'page' : 'false'}
                >
                  <div class="session-title">${s.title}</div>
                  <div class="session-date">${this._formatDate(s.createdAt)}</div>
                </button>
              `
            )}
      </div>

      <div class="divider"></div>

      <button
        class="nav-btn ${this.view === 'agents' ? 'active' : ''}"
        @click=${() => this._dispatch('view-change', { view: 'agents' })}
      >
        <span class="icon">⎇</span> Agenten
      </button>

      <button
        class="nav-btn ${this.view === 'settings' ? 'active' : ''}"
        @click=${() => this._dispatch('view-change', { view: 'settings' })}
        style="margin-bottom: 12px;"
      >
        <span class="icon">⚙</span> Einstellungen
      </button>
    `
  }
}
