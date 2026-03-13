import { LitElement, html, css } from 'lit'
import { customElement, property } from 'lit/decorators.js'
import { repeat } from 'lit/directives/repeat.js'
import type { InfoPanelSection } from '../types'

@customElement('bashgpt-chat-info-panel')
export class ChatInfoPanel extends LitElement {
  @property({ type: Array }) sections: InfoPanelSection[] = []
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
      padding: 12px 0 16px;
    }

    .loading-state,
    .empty-state {
      padding: 20px 16px;
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

    .section {
      padding: 10px 14px;
      border-bottom: 1px solid #0f172a;
    }
    .section:last-child { border-bottom: none; }

    .section-title {
      font-size: 10px;
      font-weight: 600;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: #334155;
      margin-bottom: 8px;
    }

    .row {
      display: flex;
      align-items: flex-start;
      gap: 8px;
      margin-bottom: 4px;
      line-height: 1.5;
    }
    .row:last-child { margin-bottom: 0; }

    .label {
      color: #475569;
      flex-shrink: 0;
      min-width: 76px;
    }

    .value {
      color: #94a3b8;
      word-break: break-word;
    }
    .value.path {
      color: #cbd5e1;
      font-size: 11px;
      word-break: break-all;
    }
    .value.muted { color: #64748b; }
    .value.accent { color: #7dd3fc; }
    .value.success { color: #86efac; }
    .value.error { color: #fca5a5; }

    .source {
      margin-top: 8px;
      font-size: 10px;
      color: #334155;
      text-transform: uppercase;
      letter-spacing: 0.06em;
    }
  `

  render() {
    if (this.loading) {
      return html`
        <div class="panel-header">
          <div class="dot dot-red"></div>
          <div class="dot dot-yellow"></div>
          <div class="dot dot-green"></div>
          <div class="panel-title">Info</div>
        </div>
        <div class="content">
          <div class="loading-state">
            <div class="spinner"></div>
            Lade Informationen…
          </div>
        </div>
      `
    }

    return html`
      <div class="panel-header">
        <div class="dot dot-red"></div>
        <div class="dot dot-yellow"></div>
        <div class="dot dot-green"></div>
        <div class="panel-title">Info</div>
      </div>

      <div class="content">
        ${this.sections.length === 0
          ? html`<div class="empty-state">Keine Informationen verfügbar.</div>`
          : repeat(
              this.sections,
              section => section.id,
              section => html`
                <div class="section">
                  <div class="section-title">${section.title}</div>
                  ${repeat(
                    section.items,
                    item => `${section.id}:${item.label}`,
                    item => html`
                      <div class="row">
                        <span class="label">${item.label}</span>
                        <span class="value ${item.tone ?? 'default'} ${hasPathShape(item.value) ? 'path' : ''}">${item.value}</span>
                      </div>
                    `,
                  )}
                  ${section.source ? html`<div class="source">${section.source}</div>` : ''}
                </div>
              `,
            )}
      </div>
    `
  }
}

function hasPathShape(value: string): boolean {
  return value.includes('\\') || value.includes('/')
}
