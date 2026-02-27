import { LitElement, html, css, unsafeCSS } from 'lit'
import { customElement, property } from 'lit/decorators.js'
import { unsafeHTML } from 'lit/directives/unsafe-html.js'
import { marked } from 'marked'
import hljs from 'highlight.js'
import hljsStyles from 'highlight.js/styles/github-dark.css?inline'
import type { CommandResult } from '../types'

marked.setOptions({
  async: false,
})

const renderer = new marked.Renderer()
renderer.code = ({ text, lang }) => {
  const language = lang && hljs.getLanguage(lang) ? lang : 'plaintext'
  const highlighted = hljs.highlight(text, { language }).value
  return `<pre><code class="hljs language-${language}">${highlighted}</code></pre>`
}
marked.use({ renderer })

@customElement('bashgpt-message')
export class MessageBubble extends LitElement {
  @property() role: 'user' | 'assistant' = 'user'
  @property() content = ''
  @property({ type: Array }) commands: CommandResult[] = []
  @property({ type: Boolean }) usedToolCalls = false
  @property({ type: Array }) logs: string[] = []

  static styles = [
    unsafeCSS(hljsStyles),
    css`
      :host { display: block; margin-bottom: 12px; }

      .bubble {
        border-radius: 12px;
        padding: 12px 16px;
        border: 1px solid var(--color-border, #374151);
        line-height: 1.6;
      }

      .bubble.user {
        background: var(--color-user, #1f2937);
        margin-left: 40px;
      }

      .bubble.assistant {
        background: var(--color-assistant, #0b1220);
        margin-right: 40px;
      }

      .meta {
        font-size: 11px;
        color: var(--color-muted, #6b7280);
        margin-bottom: 6px;
        text-transform: uppercase;
        letter-spacing: 0.05em;
      }

      .content { color: var(--color-text, #e5e7eb); }

      /* Markdown-Stile */
      .content pre {
        background: #020617;
        border: 1px solid #1e293b;
        border-radius: 8px;
        padding: 12px;
        overflow-x: auto;
        margin: 10px 0;
      }
      .content code:not(pre code) {
        background: #1e293b;
        border-radius: 4px;
        padding: 2px 5px;
        font-size: 0.88em;
        color: #93c5fd;
      }
      .content p { margin: 6px 0; }
      .content ul, .content ol { padding-left: 20px; margin: 6px 0; }
      .content h1, .content h2, .content h3 {
        margin: 10px 0 4px;
        color: #f1f5f9;
      }
      .content a { color: #38bdf8; }
      .content blockquote {
        border-left: 3px solid #475569;
        margin: 8px 0;
        padding-left: 12px;
        color: #94a3b8;
      }
      .content table { border-collapse: collapse; width: 100%; margin: 8px 0; }
      .content th, .content td {
        border: 1px solid #374151;
        padding: 6px 10px;
        text-align: left;
      }
      .content th { background: #1e293b; }

      /* Commands */
      .commands { margin-top: 10px; display: flex; flex-direction: column; gap: 6px; }

      .cmd-card {
        border: 1px solid #1e3a5f;
        border-radius: 8px;
        background: #020617;
        overflow: hidden;
      }
      .cmd-header {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 6px 10px;
        background: #0c1a2e;
        font-size: 12px;
        color: #94a3b8;
      }
      .cmd-code {
        font-family: ui-monospace, monospace;
        color: #93c5fd;
        font-size: 13px;
        flex: 1;
      }
      .badge {
        border-radius: 999px;
        padding: 2px 8px;
        font-size: 11px;
        font-weight: 600;
      }
      .badge.ok { background: #14532d; color: #86efac; }
      .badge.fail { background: #7f1d1d; color: #fca5a5; }
      .badge.skip { background: #1e293b; color: #94a3b8; }
      .cmd-output {
        padding: 8px 10px;
        font-family: ui-monospace, monospace;
        font-size: 12px;
        color: #cbd5e1;
        white-space: pre-wrap;
        max-height: 200px;
        overflow-y: auto;
      }

      /* Tool-call badge */
      .tool-badge {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        font-size: 11px;
        color: #a78bfa;
        margin-top: 6px;
      }
    `,
  ]

  private get _html() {
    return unsafeHTML(marked.parse(this.content) as string)
  }

  private _renderCommand(cmd: CommandResult) {
    const badgeClass = !cmd.wasExecuted ? 'skip' : cmd.exitCode === 0 ? 'ok' : 'fail'
    const badgeText = !cmd.wasExecuted ? 'übersprungen' : `exit ${cmd.exitCode}`
    return html`
      <div class="cmd-card">
        <div class="cmd-header">
          <span class="cmd-code">$ ${cmd.command}</span>
          <span class="badge ${badgeClass}">${badgeText}</span>
        </div>
        ${cmd.wasExecuted && cmd.output
          ? html`<div class="cmd-output">${cmd.output}</div>`
          : ''}
      </div>
    `
  }

  render() {
    return html`
      <div class="bubble ${this.role}">
        <div class="meta">${this.role === 'user' ? 'Du' : 'bashGPT'}</div>
        <div class="content">${this._html}</div>
        ${this.usedToolCalls
          ? html`<div class="tool-badge">⚡ Tool-Calls verwendet</div>`
          : ''}
        ${this.commands.length > 0
          ? html`<div class="commands">
              ${this.commands.map(c => this._renderCommand(c))}
            </div>`
          : ''}
      </div>
    `
  }
}
