import { LitElement, html, css } from 'lit'
import { customElement, property } from 'lit/decorators.js'
import { repeat } from 'lit/directives/repeat.js'
import type { ToolCallEntry } from '../types'

@customElement('bashgpt-tool-calls-panel')
export class ToolCallsPanel extends LitElement {
  @property({ type: Array }) entries: ToolCallEntry[] = []
  @property({ type: Boolean }) loading = false

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      background: #020617;
      border-right: 1px solid #1e293b;
      overflow: hidden;
    }

    .panel-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      padding: 10px 12px;
      border-bottom: 1px solid #1e293b;
      background: #0b1120;
      flex-shrink: 0;
    }
    .panel-title {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: #94a3b8;
    }
    .panel-count {
      font-size: 11px;
      color: #64748b;
    }

    .entries {
      flex: 1;
      overflow: auto;
      padding: 8px 0 12px;
    }

    .empty {
      padding: 20px 16px;
      color: #334155;
      font-size: 12px;
      font-style: italic;
    }

    .entry {
      margin: 0 10px 10px;
      border: 1px solid #1e293b;
      border-radius: 10px;
      background: #0b1220;
      overflow: hidden;
    }

    .entry-head {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 10px;
      border-bottom: 1px solid #1e293b;
      background: #0f172a;
      min-width: 0;
    }
    .tool-name {
      font-size: 11px;
      font-weight: 600;
      color: #38bdf8;
      background: #082f49;
      border: 1px solid #0c4a6e;
      border-radius: 999px;
      padding: 2px 8px;
      white-space: nowrap;
    }
    .status-badge {
      margin-left: auto;
      font-size: 10px;
      font-weight: 600;
      padding: 1px 6px;
      border-radius: 999px;
      white-space: nowrap;
      flex-shrink: 0;
    }
    .badge-running  { background: #1e3a5f; color: #60a5fa; }
    .badge-success  { background: #14532d; color: #86efac; }
    .badge-error    { background: #7f1d1d; color: #fca5a5; }
    .badge-skipped  { background: #1e293b; color: #64748b; }

    .entry-body {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding: 10px;
    }
    .label {
      font-size: 11px;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: #64748b;
      margin-bottom: 3px;
    }
    .command {
      color: #e2e8f0;
      white-space: pre-wrap;
      word-break: break-word;
      font-family: ui-monospace, 'Cascadia Code', 'Fira Code', monospace;
      font-size: 12px;
      line-height: 1.5;
      margin: 0;
    }
    .output {
      color: #94a3b8;
      white-space: pre-wrap;
      word-break: break-word;
      font-family: ui-monospace, 'Cascadia Code', 'Fira Code', monospace;
      font-size: 12px;
      line-height: 1.5;
      margin: 0;
    }
    .output.error { color: #f87171; }
    .output.skipped { color: #64748b; font-style: italic; }
  `

  updated() {
    const el = this.shadowRoot?.querySelector('.entries')
    if (el) el.scrollTop = el.scrollHeight
  }

  private _badgeClass(s: ToolCallEntry['status']) {
    return { running: 'badge-running', success: 'badge-success', error: 'badge-error', skipped: 'badge-skipped' }[s]
  }

  private _badgeLabel(e: ToolCallEntry) {
    if (e.status === 'running') return 'running'
    if (e.status === 'skipped') return 'skipped'
    if (e.status === 'success') return 'ok'
    return `exit ${e.exitCode}`
  }

  private _outputClass(e: ToolCallEntry) {
    if (e.status === 'skipped') return 'skipped'
    if (e.status === 'error')   return 'error'
    return ''
  }

  render() {
    const hasEntries = this.entries.length > 0 || this.loading

    return html`
      <div class="panel-header">
        <div class="panel-title">Tool Calls</div>
        <div class="panel-count">${this.entries.length}</div>
      </div>

      <div class="entries">
        ${!hasEntries
          ? html`<div class="empty">Noch keine Tool-Calls.</div>`
          : ''}

        ${repeat(
          this.entries,
          (_e, i) => i,
          e => html`
            <div class="entry">
              <div class="entry-head">
                <span class="tool-name">${e.toolName || 'tool'}</span>
                <span class="status-badge ${this._badgeClass(e.status)}">${this._badgeLabel(e)}</span>
              </div>
              <div class="entry-body">
                <div>
                  <div class="label">Command</div>
                  <pre class="command">${e.command}</pre>
                </div>
                ${e.status === 'running'
                  ? html``
                  : html`
                    <div>
                      <div class="label">Output</div>
                      <pre class="output ${this._outputClass(e)}">${e.output?.length ? e.output : '(keine Ausgabe)'}</pre>
                    </div>
                  `}
              </div>
            </div>
          `
        )}
      </div>
    `
  }
}
