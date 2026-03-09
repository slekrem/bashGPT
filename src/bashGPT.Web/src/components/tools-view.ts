import { LitElement, html, css } from 'lit'
import { customElement, state } from 'lit/decorators.js'
import { repeat } from 'lit/directives/repeat.js'
import type { ToolInfo } from '../types'
import { getTools } from '../api'

@customElement('bashgpt-tools-view')
export class ToolsView extends LitElement {
  @state() private _tools: ToolInfo[] = []
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
      color: #64748b;
      margin-bottom: 24px;
    }

    .error {
      color: #ef4444;
      font-size: 13px;
      padding: 12px;
      background: #1c0a0a;
      border: 1px solid #7f1d1d;
      border-radius: 8px;
      margin-bottom: 16px;
    }

    .loading {
      color: #475569;
      font-size: 13px;
    }

    .empty {
      color: #475569;
      font-size: 13px;
      padding: 32px 0;
      text-align: center;
    }

    .tool-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .tool-card {
      background: #0f172a;
      border: 1px solid #1e293b;
      border-radius: 10px;
      padding: 16px;
    }

    .tool-header {
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 6px;
    }

    .tool-name {
      font-size: 14px;
      font-weight: 600;
      color: #f1f5f9;
      font-family: monospace;
    }

    .tool-desc {
      font-size: 13px;
      color: #94a3b8;
      margin-bottom: 10px;
      line-height: 1.5;
    }

    .params-label {
      font-size: 11px;
      font-weight: 600;
      color: #475569;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      margin-bottom: 6px;
    }

    .param-row {
      display: flex;
      align-items: baseline;
      gap: 8px;
      padding: 5px 0;
      border-top: 1px solid #1e293b;
      font-size: 12px;
    }

    .param-name {
      font-family: monospace;
      color: #7dd3fc;
      min-width: 100px;
    }

    .param-type {
      color: #a78bfa;
      min-width: 60px;
    }

    .param-desc {
      color: #64748b;
      flex: 1;
    }

    .param-required {
      font-size: 10px;
      font-weight: 600;
      padding: 1px 6px;
      border-radius: 999px;
      background: #14532d;
      color: #86efac;
      flex-shrink: 0;
    }

    .param-optional {
      font-size: 10px;
      font-weight: 600;
      padding: 1px 6px;
      border-radius: 999px;
      background: #1e293b;
      color: #64748b;
      flex-shrink: 0;
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
      this._tools = await getTools()
    } catch (e) {
      this._error = e instanceof Error ? e.message : String(e)
    } finally {
      this._loading = false
    }
  }

  render() {
    return html`
      <h2>Tools</h2>
      <div class="subtitle">Registrierte Tools – nutzbar im Chat und von Agenten.</div>

      ${this._error ? html`<div class="error">${this._error}</div>` : ''}

      ${this._loading
        ? html`<div class="loading">Lade Tools…</div>`
        : this._tools.length === 0
          ? html`<div class="empty">Keine Tools registriert.</div>`
          : html`
            <div class="tool-list">
              ${repeat(this._tools, t => t.name, t => html`
                <div class="tool-card">
                  <div class="tool-header">
                    <span class="tool-name">${t.name}</span>
                  </div>
                  ${t.description ? html`<div class="tool-desc">${t.description}</div>` : ''}
                  ${t.parameters?.length ? html`
                    <div class="params-label">Parameter</div>
                    ${t.parameters.map(p => html`
                      <div class="param-row">
                        <span class="param-name">${p.name}</span>
                        <span class="param-type">${p.type}</span>
                        <span class="param-desc">${p.description}</span>
                        <span class="${p.required ? 'param-required' : 'param-optional'}">
                          ${p.required ? 'required' : 'optional'}
                        </span>
                      </div>
                    `)}
                  ` : ''}
                </div>
              `)}
            </div>
          `}
    `
  }
}
