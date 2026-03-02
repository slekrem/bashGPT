import { LitElement, html, css } from 'lit'
import { customElement, property } from 'lit/decorators.js'
import type { ExecMode, FullShellContext, Settings, TokenUsage } from '../types'

interface CommandStats {
  total: number
  success: number
  error: number
  skipped: number
}

@customElement('bashgpt-chat-info-panel')
export class ChatInfoPanel extends LitElement {
  @property({ type: Object }) context: FullShellContext | null = null
  @property({ type: Object }) settings: Settings | null = null
  @property() execMode: ExecMode = 'ask'
  @property({ type: Number }) messageCount = 0
  @property({ type: Object }) commandStats: CommandStats = { total: 0, success: 0, error: 0, skipped: 0 }
  @property({ type: Object }) tokenUsage: TokenUsage | null = null
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
    .dot-red    { background: #ef4444; }
    .dot-yellow { background: #f59e0b; }
    .dot-green  { background: #22c55e; }

    .content {
      flex: 1;
      overflow-y: auto;
      padding: 12px 0 16px;
    }

    .loading-state {
      padding: 20px 16px;
      color: #334155;
      font-size: 12px;
      font-style: italic;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .spinner {
      width: 10px; height: 10px;
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
      gap: 6px;
      margin-bottom: 4px;
      line-height: 1.5;
    }
    .row:last-child { margin-bottom: 0; }

    .label {
      color: #475569;
      flex-shrink: 0;
      min-width: 52px;
    }

    .value {
      color: #94a3b8;
      word-break: break-all;
    }

    .value.mono {
      font-family: ui-monospace, 'Cascadia Code', 'Fira Code', monospace;
      color: #7dd3fc;
    }

    .value.path {
      font-family: ui-monospace, 'Cascadia Code', 'Fira Code', monospace;
      color: #cbd5e1;
      font-size: 11px;
      word-break: break-all;
    }

    .title-row {
      margin-bottom: 6px;
    }
    .dir-title {
      color: #e2e8f0;
      font-weight: 600;
      font-size: 13px;
      font-family: ui-monospace, 'Cascadia Code', 'Fira Code', monospace;
      word-break: break-all;
    }

    .badge {
      display: inline-block;
      font-size: 10px;
      font-weight: 600;
      padding: 1px 7px;
      border-radius: 999px;
      white-space: nowrap;
    }
    .badge-success { background: #14532d; color: #86efac; }
    .badge-error   { background: #7f1d1d; color: #fca5a5; }
    .badge-neutral { background: #1e293b; color: #64748b; }
    .badge-branch  { background: #0c2a4a; color: #60a5fa; }
    .badge-mode    { background: #1a1a2e; color: #a78bfa; }

    .stats-row {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-wrap: wrap;
    }
    .stat {
      display: flex;
      align-items: center;
      gap: 3px;
      color: #475569;
      font-size: 11px;
    }
    .stat .count { color: #94a3b8; }
    .stat.success .count { color: #86efac; }
    .stat.error .count { color: #fca5a5; }
    .stat.skipped .count { color: #64748b; }
  `

  private _basename(path: string): string {
    const normalized = path.replace(/\\/g, '/').replace(/\/+$/, '')
    return normalized.split('/').at(-1) || path
  }

  private _resolveContextWindow(model: string | undefined): number | null {
    if (!model) return null
    const normalized = model.trim().toLowerCase()
    if (!normalized) return null

    const kMatch = normalized.match(/(?:^|[-_:])(\d{1,4})k(?:$|[-_:])/)
    if (kMatch) {
      const k = Number(kMatch[1])
      if (Number.isFinite(k) && k > 0)
        return k * 1024
    }

    return null
  }

  private _fmtNumber(value: number | undefined, fractionDigits = 2): string {
    if (value == null) return '-'
    return Number.isInteger(value)
      ? value.toString()
      : value.toFixed(fractionDigits).replace(/\.?0+$/, '')
  }

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
            Lade Kontext…
          </div>
        </div>
      `
    }

    const ctx = this.context
    const stats = this.commandStats
    const inputTokens = this.tokenUsage?.inputTokens ?? 0
    const outputTokens = this.tokenUsage?.outputTokens ?? 0
    const totalTokens = this.tokenUsage?.totalTokens ?? (inputTokens + outputTokens)
    const cachedInputTokens = this.tokenUsage?.cachedInputTokens ?? 0
    const usedTokens = totalTokens
    const contextWindow = this.settings?.contextWindowTokens ?? this._resolveContextWindow(this.settings?.model)
    const contextUsagePct = contextWindow && usedTokens > 0
      ? Math.min(100, Math.round((usedTokens / contextWindow) * 100))
      : null

    return html`
      <div class="panel-header">
        <div class="dot dot-red"></div>
        <div class="dot dot-yellow"></div>
        <div class="dot dot-green"></div>
        <div class="panel-title">Info</div>
      </div>

      <div class="content">
        <!-- 1. Arbeitsbereich -->
        <div class="section">
          <div class="section-title">Arbeitsbereich</div>
          ${ctx ? html`
            <div class="title-row">
              <div class="dir-title">${this._basename(ctx.cwd)}</div>
            </div>
            <div class="row">
              <span class="label">Pfad</span>
              <span class="value path">${ctx.cwd}</span>
            </div>
            <div class="row">
              <span class="label">OS</span>
              <span class="value">${ctx.os}</span>
            </div>
            <div class="row">
              <span class="label">Shell</span>
              <span class="value mono">${ctx.shell}</span>
            </div>
          ` : html`
            <div class="row"><span class="value" style="color:#334155;font-style:italic">Nicht verfügbar</span></div>
          `}
        </div>

        <!-- 2. Git (nur wenn verfügbar) -->
        ${ctx?.git ? html`
          <div class="section">
            <div class="section-title">Git</div>
            <div class="row">
              <span class="label">Branch</span>
              <span class="badge badge-branch">${ctx.git.branch}</span>
            </div>
            ${ctx.git.lastCommit ? html`
              <div class="row">
                <span class="label">Commit</span>
                <span class="value mono">${ctx.git.lastCommit.length > 40
                  ? ctx.git.lastCommit.slice(0, 40) + '…'
                  : ctx.git.lastCommit}</span>
              </div>
            ` : ''}
            <div class="row">
              <span class="label">Änder.</span>
              <span class="value">${ctx.git.changedFilesCount === 0
                ? html`<span style="color:#334155">keine</span>`
                : html`<span class="badge badge-neutral">${ctx.git.changedFilesCount} Datei${ctx.git.changedFilesCount !== 1 ? 'en' : ''}</span>`
              }</span>
            </div>
          </div>
        ` : ''}

        <!-- 3. Provider & Modell -->
        <div class="section">
          <div class="section-title">Provider & Modell</div>
          ${this.settings ? html`
            <div class="row">
              <span class="label">Provider</span>
              <span class="value mono">${this.settings.provider}</span>
            </div>
            <div class="row">
              <span class="label">Modell</span>
              <span class="value mono">${this.settings.model}</span>
            </div>
            <div class="row">
              <span class="label">Kontext</span>
              <span class="value mono">
                ${contextWindow
                  ? `${contextWindow.toLocaleString()} Tokens`
                  : html`<span style="color:#334155;font-style:italic">Unbekannt</span>`}
              </span>
            </div>
            ${this.settings.provider === 'ollama' ? html`
              <div class="row">
                <span class="label">Host</span>
                <span class="value mono">${this.settings.ollama.host}</span>
              </div>
              <div class="row">
                <span class="label">Temp</span>
                <span class="value mono">${this._fmtNumber(this.settings.ollama.temperature ?? 0.2)}</span>
              </div>
              <div class="row">
                <span class="label">top_p</span>
                <span class="value mono">${this._fmtNumber(this.settings.ollama.topP ?? 0.9)}</span>
              </div>
              <div class="row">
                <span class="label">seed</span>
                <span class="value mono">${this.settings.ollama.seed ?? '-'}</span>
              </div>
            ` : html`
              <div class="row">
                <span class="label">Base URL</span>
                <span class="value mono">${this.settings.cerebras.baseUrl ?? 'https://api.cerebras.ai/v1'}</span>
              </div>
              <div class="row">
                <span class="label">Temp</span>
                <span class="value mono">${this._fmtNumber(this.settings.cerebras.temperature ?? 0.2)}</span>
              </div>
              <div class="row">
                <span class="label">top_p</span>
                <span class="value mono">${this._fmtNumber(this.settings.cerebras.topP ?? 0.9)}</span>
              </div>
              <div class="row">
                <span class="label">max_tok</span>
                <span class="value mono">${this._fmtNumber(this.settings.cerebras.maxCompletionTokens ?? 2048, 0)}</span>
              </div>
              <div class="row">
                <span class="label">reason</span>
                <span class="value mono">${this.settings.cerebras.reasoningEffort ?? 'medium'}</span>
              </div>
              <div class="row">
                <span class="label">seed</span>
                <span class="value mono">${this.settings.cerebras.seed ?? '-'}</span>
              </div>
            `}
          ` : html`
            <div class="row"><span class="value" style="color:#334155;font-style:italic">Nicht verfügbar</span></div>
          `}
        </div>

        <!-- 4. Ausführung -->
        <div class="section">
          <div class="section-title">Ausführung</div>
          <div class="row">
            <span class="label">Modus</span>
            <span class="badge badge-mode">${this.execMode}</span>
          </div>
        </div>

        <!-- 5. Session -->
        <div class="section">
          <div class="section-title">Session</div>
          <div class="row">
            <span class="label">Nachr.</span>
            <span class="value">${this.messageCount}</span>
          </div>
          ${stats.total > 0 ? html`
            <div class="row">
              <span class="label">Befehle</span>
              <div class="stats-row">
                <span class="stat success">
                  <span>✓</span><span class="count">${stats.success}</span>
                </span>
                <span class="stat error">
                  <span>✗</span><span class="count">${stats.error}</span>
                </span>
                <span class="stat skipped">
                  <span>—</span><span class="count">${stats.skipped}</span>
                </span>
              </div>
            </div>
          ` : html`
            <div class="row">
              <span class="label">Befehle</span>
              <span class="value" style="color:#334155;font-style:italic">keine</span>
            </div>
          `}
          ${this.tokenUsage && (inputTokens > 0 || outputTokens > 0) ? html`
            <div class="row">
              <span class="label">Tokens</span>
              <div class="stats-row">
                <span class="stat" title="Input-Tokens">
                  <span style="color:#475569">↑</span>
                  <span class="count">${inputTokens.toLocaleString()}</span>
                </span>
                <span class="stat" title="Output-Tokens">
                  <span style="color:#475569">↓</span>
                  <span class="count">${outputTokens.toLocaleString()}</span>
                </span>
                ${cachedInputTokens > 0 ? html`
                  <span class="stat" title="Cached Input-Tokens">
                    <span style="color:#475569">↺</span>
                    <span class="count">${cachedInputTokens.toLocaleString()}</span>
                  </span>
                ` : ''}
              </div>
            </div>
            <div class="row">
              <span class="label">Kontext</span>
              <span class="value mono">
                ${usedTokens.toLocaleString()}${contextWindow
                  ? ` / ${contextWindow.toLocaleString()} (${contextUsagePct}%)`
                  : ' Tokens genutzt'}
              </span>
            </div>
          ` : ''}
        </div>
      </div>
    `
  }
}
