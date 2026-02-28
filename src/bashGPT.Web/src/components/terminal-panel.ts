import { LitElement, html, css } from 'lit'
import { customElement, property } from 'lit/decorators.js'
import { repeat } from 'lit/directives/repeat.js'
import type { TerminalEntry } from '../types'

@customElement('bashgpt-terminal-panel')
export class TerminalPanel extends LitElement {
  @property({ type: Array }) entries: TerminalEntry[] = []
  @property({ type: Boolean }) loading = false

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      background: #020617;
      border-right: 1px solid #1e293b;
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
    .dot-red    { background: #ef4444; }
    .dot-yellow { background: #f59e0b; }
    .dot-green  { background: #22c55e; }

    .entries {
      flex: 1;
      overflow-y: auto;
      padding: 10px 0 16px;
    }

    .empty {
      padding: 20px 16px;
      color: #334155;
      font-size: 12px;
      font-style: italic;
    }

    .entry {
      padding: 6px 12px 10px;
      border-bottom: 1px solid #0f172a;
    }
    .entry:last-child { border-bottom: none; }

    .prompt-line {
      display: flex;
      align-items: center;
      gap: 6px;
      margin-bottom: 4px;
    }
    .prompt-sign {
      color: #22c55e;
      font-weight: 700;
      user-select: none;
    }
    .cmd-text {
      color: #f1f5f9;
      word-break: break-all;
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

    .output {
      color: #94a3b8;
      white-space: pre-wrap;
      word-break: break-all;
      margin-left: 16px;
      max-height: 180px;
      overflow-y: auto;
      line-height: 1.5;
    }
    .output.error { color: #f87171; }
    .output.skipped { color: #475569; font-style: italic; }

    .running-indicator {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      color: #60a5fa;
      margin-left: 16px;
      margin-top: 4px;
    }
    .spinner {
      width: 10px; height: 10px;
      border: 1.5px solid #1e3a5f;
      border-top-color: #60a5fa;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
  `

  updated() {
    const el = this.shadowRoot?.querySelector('.entries')
    if (el) el.scrollTop = el.scrollHeight
  }

  private _badgeClass(s: TerminalEntry['status']) {
    return { running: 'badge-running', success: 'badge-success', error: 'badge-error', skipped: 'badge-skipped' }[s]
  }

  private _badgeLabel(s: TerminalEntry['status']) {
    return { running: '●  läuft', success: 'exit 0', error: `exit ${0}`, skipped: 'übersprungen' }[s]
  }

  private _outputClass(e: TerminalEntry) {
    if (e.status === 'skipped') return 'skipped'
    if (e.status === 'error')   return 'error'
    return ''
  }

  render() {
    const hasEntries = this.entries.length > 0 || this.loading

    return html`
      <div class="panel-header">
        <div class="dot dot-red"></div>
        <div class="dot dot-yellow"></div>
        <div class="dot dot-green"></div>
        <div class="panel-title">Terminal</div>
      </div>

      <div class="entries">
        ${!hasEntries
          ? html`<div class="empty">Noch keine Befehle ausgeführt.</div>`
          : ''}

        ${repeat(
          this.entries,
          (_e, i) => i,
          e => {
            const badgeLabel = e.status === 'error'
              ? `exit ${e.exitCode}`
              : this._badgeLabel(e.status)
            return html`
              <div class="entry">
                <div class="prompt-line">
                  <span class="prompt-sign">$</span>
                  <span class="cmd-text">${e.command}</span>
                  <span class="status-badge ${this._badgeClass(e.status)}">${badgeLabel}</span>
                </div>
                ${e.status === 'running'
                  ? html`<div class="running-indicator"><div class="spinner"></div> läuft…</div>`
                  : e.output
                    ? html`<div class="output ${this._outputClass(e)}">${e.output}</div>`
                    : ''}
              </div>
            `
          }
        )}

        ${this.loading && this.entries.every(e => e.status !== 'running')
          ? html`
              <div class="entry">
                <div class="running-indicator">
                  <div class="spinner"></div> Denke…
                </div>
              </div>
            `
          : ''}
      </div>
    `
  }
}
