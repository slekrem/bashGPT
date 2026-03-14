import { LitElement, html, css } from 'lit'
import { customElement, state } from 'lit/decorators.js'
import { repeat } from 'lit/directives/repeat.js'
import type { Agent } from '../types'
import { getAgents } from '../api'

@customElement('bashgpt-agents-view')
export class AgentsView extends LitElement {
  @state() private _agents: Agent[] = []
  @state() private _loading = true
  @state() private _error = ''

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

    .agent-icon {
      font-size: 20px;
      line-height: 1;
      margin-top: 2px;
    }

    .agent-body { flex: 1; min-width: 0; }

    .agent-name {
      font-size: 14px;
      font-weight: 600;
      color: #f1f5f9;
      margin-bottom: 4px;
    }

    .agent-meta {
      font-size: 12px;
      color: #475569;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .agent-tools {
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
      margin-top: 6px;
    }

    .tool-badge {
      font-size: 10px;
      background: #0f2d1a;
      border: 1px solid #166534;
      color: #86efac;
      padding: 2px 6px;
      border-radius: 4px;
    }

    .agent-actions {
      display: flex;
      gap: 6px;
      flex-shrink: 0;
      align-items: flex-start;
    }

    .btn-chat {
      background: #14532d;
      border: 1px solid #16a34a;
      color: #86efac;
      font-size: 12px;
      padding: 5px 10px;
      border-radius: 6px;
      cursor: pointer;
      transition: background 0.12s;
    }
    .btn-chat:hover { background: #166534; }

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

  private _startChat(agent: Agent) {
    this.dispatchEvent(new CustomEvent('agent-chat-start', {
      detail: { agentId: agent.id },
      bubbles: true,
      composed: true,
    }))
  }

  render() {
    return html`
      <h2>Agenten</h2>
      <div class="subtitle">Vordefinierte KI-Assistenten – wähle einen aus und starte einen Chat</div>

      ${this._error ? html`<div class="error-msg">${this._error}</div>` : ''}

      ${this._loading
        ? html`<div class="empty">Lade Agenten…</div>`
        : this._agents.length === 0
          ? html`<div class="empty">Keine Agenten verfügbar.</div>`
          : html`
            <div class="agent-list">
              ${repeat(this._agents, a => a.id, a => this._renderAgent(a))}
            </div>
          `}
    `
  }

  private _renderAgent(a: Agent) {
    return html`
      <div class="agent-card">
        <div class="agent-icon">🤖</div>
        <div class="agent-body">
          <div class="agent-name">${a.name}</div>
          <div class="agent-meta">${a.id}</div>
        </div>
        <div class="agent-actions">
          <button class="btn-chat" @click=${() => this._startChat(a)}>Chat starten</button>
        </div>
      </div>
    `
  }
}
