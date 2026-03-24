import { LitElement, html, css, unsafeCSS } from 'lit'
import { customElement, property } from 'lit/decorators.js'
import { unsafeHTML } from 'lit/directives/unsafe-html.js'
import DOMPurify from 'dompurify'
import { marked } from 'marked'
import hljs from 'highlight.js/lib/core'
import bash from 'highlight.js/lib/languages/bash'
import csharp from 'highlight.js/lib/languages/csharp'
import diff from 'highlight.js/lib/languages/diff'
import javascript from 'highlight.js/lib/languages/javascript'
import json from 'highlight.js/lib/languages/json'
import plaintext from 'highlight.js/lib/languages/plaintext'
import powershell from 'highlight.js/lib/languages/powershell'
import typescript from 'highlight.js/lib/languages/typescript'
import xml from 'highlight.js/lib/languages/xml'
import hljsStyles from 'highlight.js/styles/github-dark.css?inline'

hljs.registerLanguage('bash', bash)
hljs.registerLanguage('sh', bash)
hljs.registerLanguage('shell', bash)
hljs.registerLanguage('powershell', powershell)
hljs.registerLanguage('ps1', powershell)
hljs.registerLanguage('json', json)
hljs.registerLanguage('javascript', javascript)
hljs.registerLanguage('js', javascript)
hljs.registerLanguage('typescript', typescript)
hljs.registerLanguage('ts', typescript)
hljs.registerLanguage('xml', xml)
hljs.registerLanguage('html', xml)
hljs.registerLanguage('csharp', csharp)
hljs.registerLanguage('cs', csharp)
hljs.registerLanguage('diff', diff)
hljs.registerLanguage('plaintext', plaintext)
hljs.registerLanguage('text', plaintext)

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
  @property({ type: Boolean }) reasoning = false

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

      /* Reasoning mode: muted display */
      .reasoning-label {
        font-size: 10px;
        color: #475569;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        margin-bottom: 4px;
      }
      .content.is-reasoning {
        color: #64748b;
        font-style: italic;
        white-space: pre-wrap;
      }

      /* Markdown styles */
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

      /* Exec-mode badge */
      .meta-row {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 6px;
      }
    `,
  ]

  private get _html() {
    if (this.reasoning) {
      // Reasoning live: plain text, no Markdown parsing
      return html`<span>${this.content}</span>`
    }
    // Remove thinking blocks (<thinking>…</thinking>) from content
    const clean = this.content
      .replace(/<thinking>[\s\S]*?<\/thinking>/gi, '')
      .trim()
    const parsed = marked.parse(clean) as string
    const sanitized = DOMPurify.sanitize(parsed, {
      FORBID_TAGS: ['script', 'style', 'iframe', 'object', 'embed'],
    })
    return unsafeHTML(sanitized)
  }

  render() {
    return html`
      <div class="bubble ${this.role}">
        <div class="meta-row">
          <span class="meta">${this.role === 'user' ? 'You' : 'bashGPT'}</span>
        </div>
        ${this.reasoning ? html`<div class="reasoning-label">Thinking…</div>` : ''}
        <div class="content ${this.reasoning ? 'is-reasoning' : ''}">${this._html}</div>
      </div>
    `
  }
}
