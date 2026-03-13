import { LitElement, html, css } from 'lit'
import { customElement, property } from 'lit/decorators.js'
import { unsafeHTML } from 'lit/directives/unsafe-html.js'
import { marked } from 'marked'

@customElement('bashgpt-chat-info-panel')
export class ChatInfoPanel extends LitElement {
  @property({ type: String }) markdown = ''
  @property({ type: Boolean }) loading = false

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      background: #020617;
      border-left: 1px solid #1e293b;
      font-family: ui-monospace, 'Cascadia Code', 'Fira Code', monospace;
      font-size: 12px;
      overflow: hidden;
    }

    .panel-header {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 8px 12px;
      border-bottom: 1px solid #1e293b;
      background: #0b1120;
      flex-shrink: 0;
    }
    .panel-title {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: #475569;
      flex: 1;
    }
    .dot { width: 8px; height: 8px; border-radius: 50%; }
    .dot-red { background: #ef4444; }
    .dot-yellow { background: #f59e0b; }
    .dot-green { background: #22c55e; }

    .content {
      flex: 1;
      overflow-y: auto;
      padding: 16px;
    }

    .loading-state,
    .empty-state {
      padding: 20px 0;
      color: #334155;
      font-size: 12px;
      font-style: italic;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .spinner {
      width: 10px;
      height: 10px;
      border: 1.5px solid #1e3a5f;
      border-top-color: #60a5fa;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    /* Markdown-Rendering */
    .md h1 {
      font-size: 14px;
      font-weight: 700;
      color: #e2e8f0;
      margin: 0 0 12px;
      padding-bottom: 6px;
      border-bottom: 1px solid #1e293b;
    }
    .md h2 {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: #475569;
      margin: 16px 0 6px;
    }
    .md p {
      color: #94a3b8;
      line-height: 1.6;
      margin: 0 0 8px;
    }
    .md ul {
      margin: 0 0 8px;
      padding-left: 16px;
    }
    .md li {
      color: #94a3b8;
      line-height: 1.6;
      margin-bottom: 2px;
    }
    .md code {
      color: #7dd3fc;
      background: #0f172a;
      padding: 1px 4px;
      border-radius: 3px;
    }
    .md table {
      width: 100%;
      border-collapse: collapse;
      margin-bottom: 12px;
    }
    .md th {
      font-size: 10px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: #334155;
      text-align: left;
      padding: 4px 6px;
      border-bottom: 1px solid #1e293b;
    }
    .md td {
      color: #94a3b8;
      padding: 4px 6px;
      border-bottom: 1px solid #0f172a;
      vertical-align: top;
    }
    .md td code {
      font-size: 11px;
    }
    .md tr:last-child td { border-bottom: none; }
  `

  render() {
    const header = html`
      <div class="panel-header">
        <div class="dot dot-red"></div>
        <div class="dot dot-yellow"></div>
        <div class="dot dot-green"></div>
        <div class="panel-title">Info</div>
      </div>
    `

    if (this.loading) {
      return html`${header}
        <div class="content">
          <div class="loading-state">
            <div class="spinner"></div>
            Lade Informationen…
          </div>
        </div>`
    }

    if (!this.markdown) {
      return html`${header}
        <div class="content">
          <div class="empty-state">Keine Informationen verfügbar.</div>
        </div>`
    }

    return html`${header}
      <div class="content">
        <div class="md">${unsafeHTML(marked.parse(this.markdown) as string)}</div>
      </div>`
  }
}
