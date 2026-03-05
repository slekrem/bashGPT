import type { Agent, ChatResponse, CommandResult, ExecMode, FullShellContext, HistoryMessage, Session, Settings, ToolInfo } from './types'
import { CHAT_TIMEOUT_MS } from './constants'

async function readErrorMessage(res: Response): Promise<string> {
  try {
    const data = await res.json()
    if (data && typeof data.error === 'string' && data.error.trim().length > 0)
      return data.error
  } catch {
    // Fall through to default message.
  }

  return `HTTP ${res.status}`
}

async function assertOk(res: Response): Promise<void> {
  if (res.ok) return
  throw new Error(await readErrorMessage(res))
}

export async function sendChat(prompt: string, execMode: ExecMode, sessionId?: string): Promise<ChatResponse> {
  const controller = new AbortController()
  const timeout = setTimeout(() => controller.abort(), CHAT_TIMEOUT_MS)
  let res: Response
  try {
    res = await fetch('/api/chat', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ prompt, execMode, ...(sessionId ? { sessionId } : {}) }),
      signal: controller.signal,
    })
  } catch (err) {
    if (err instanceof DOMException && err.name === 'AbortError')
      throw new Error(`Zeitlimit erreicht (${Math.round(CHAT_TIMEOUT_MS / 1000)}s)`)
    throw err
  } finally {
    clearTimeout(timeout)
  }

  await assertOk(res)
  return res.json()
}

export type StreamHandlers = {
  onToken?: (token: string) => void
  onToolCall?: (data: { name: string; command: string }) => void
  onCommandResult?: (data: CommandResult) => void
  onRoundStart?: (data: { round: number }) => void
}

export async function streamChat(
  prompt: string,
  execMode: ExecMode,
  handlers: StreamHandlers,
  sessionId?: string
): Promise<ChatResponse> {
  const res = await fetch('/api/chat/stream', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ prompt, execMode, ...(sessionId ? { sessionId } : {}) }),
  })

  if (!res.ok)
    throw new Error(await readErrorMessage(res))

  const reader = res.body!.getReader()
  const decoder = new TextDecoder()
  let buf = ''
  let chatResponse: ChatResponse | null = null

  try {
    while (true) {
      const { done, value } = await reader.read()
      if (done) break
      buf += decoder.decode(value, { stream: true })

      const lines = buf.split('\n')
      buf = lines.pop() ?? ''

      for (const line of lines) {
        if (!line.startsWith('data: ')) continue
        const payload = line.slice(6).trim()
        if (payload === '[DONE]') continue

        let parsed: any
        try { parsed = JSON.parse(payload) } catch { continue }

        const delta = parsed?.choices?.[0]?.delta
        if (!delta) continue

        if (delta.bashgpt) {
          const { event, data } = delta.bashgpt
          if (event === 'tool_call') handlers.onToolCall?.(data)
          else if (event === 'command_result') handlers.onCommandResult?.(data)
          else if (event === 'round_start') handlers.onRoundStart?.(data)
          else if (event === 'error') throw new Error(data?.message ?? 'Serverfehler')
        } else if (delta.content) {
          handlers.onToken?.(delta.content)
        }

        if (parsed?.bashgpt?.event === 'done') {
          const bg = parsed.bashgpt
          chatResponse = {
            response:      bg.response,
            usedToolCalls: bg.usedToolCalls,
            logs:          bg.logs ?? [],
            shellContext:  bg.shellContext,
            commands:      bg.commands ?? [],
            usage:         parsed.usage
              ? { inputTokens: parsed.usage.promptTokens, outputTokens: parsed.usage.completionTokens }
              : undefined,
          }
        }
      }
    }
  } finally {
    reader.releaseLock()
  }

  if (!chatResponse)
    throw new Error('Keine Antwort vom Server erhalten.')

  return chatResponse
}

export async function loadHistory(): Promise<HistoryMessage[]> {
  const res = await fetch('/api/history')
  await assertOk(res)
  const data = await res.json()
  return Array.isArray(data.history) ? data.history : []
}

export async function resetHistory(): Promise<void> {
  const res = await fetch('/api/reset', { method: 'POST' })
  await assertOk(res)
}

// ── Sessions API ─────────────────────────────────────────────────────────────

export async function getSessions(): Promise<Session[] | null> {
  try {
    const res = await fetch('/api/sessions')
    if (!res.ok) return null
    const data = await res.json()
    return Array.isArray(data.sessions) ? data.sessions : []
  } catch {
    return null
  }
}

export async function createSession(): Promise<Session | null> {
  try {
    const res = await fetch('/api/sessions', { method: 'POST' })
    if (!res.ok) return null
    return res.json()
  } catch { return null }
}

export async function getSession(id: string): Promise<{ messages: any[]; shellContext?: any } | null> {
  try {
    const res = await fetch(`/api/sessions/${id}`)
    if (!res.ok) return null
    return res.json()
  } catch {
    return null
  }
}

export async function putSession(
  id: string,
  data: { title?: string; messages: any[]; shellContext?: any; createdAt?: string }
): Promise<void> {
  try {
    await fetch(`/api/sessions/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...data, id }),
    })
  } catch {
    // ignore – localStorage bleibt als Fallback
  }
}

export async function deleteSession(id: string): Promise<void> {
  try {
    await fetch(`/api/sessions/${id}`, { method: 'DELETE' })
  } catch {
    // ignore
  }
}

export async function clearSessions(): Promise<void> {
  try {
    await fetch('/api/sessions/clear', { method: 'POST' })
  } catch {
    // ignore
  }
}

// ── Context API ───────────────────────────────────────────────────────────────

export async function getContext(): Promise<FullShellContext | null> {
  try {
    const res = await fetch('/api/context')
    if (!res.ok) return null
    return res.json()
  } catch { return null }
}

// ── Settings (v2 API – graceful fallback if not yet implemented) ────────────

export async function getSettings(): Promise<Settings | null> {
  try {
    const res = await fetch('/api/settings')
    if (!res.ok) return null
    return res.json()
  } catch {
    return null
  }
}

export async function saveSettings(settings: Partial<Settings>): Promise<void> {
  const res = await fetch('/api/settings', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(settings),
  })
  await assertOk(res)
}

export async function testConnection(): Promise<{ ok: boolean; latencyMs?: number; error?: string }> {
  try {
    const res = await fetch('/api/settings/test', { method: 'POST' })
    const data = await res.json()
    return data
  } catch (e) {
    return { ok: false, error: e instanceof Error ? e.message : String(e) }
  }
}

// ── Agents API ────────────────────────────────────────────────────────────────

export async function getAgents(): Promise<Agent[]> {
  try {
    const res = await fetch('/api/agents')
    if (!res.ok) return []
    const data = await res.json()
    return Array.isArray(data.agents) ? data.agents : []
  } catch { return [] }
}

export async function createAgent(payload: {
  name: string
  type: 'git' | 'http' | 'llm'
  path?: string
  url?: string
  intervalSeconds?: number
  systemPrompt?: string
  loopInstruction?: string
  execMode?: string
  enabledTools?: string[]
}): Promise<Agent> {
  const res = await fetch('/api/agents', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
  await assertOk(res)
  return res.json()
}

export async function patchAgent(id: string, patch: {
  isActive?: boolean
  name?: string
  intervalSeconds?: number
  systemPrompt?: string | null
  loopInstruction?: string
  execMode?: string
  enabledTools?: string[]
}): Promise<Agent> {
  const res = await fetch(`/api/agents/${id}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(patch),
  })
  await assertOk(res)
  return res.json()
}

export async function deleteAgent(id: string): Promise<void> {
  const res = await fetch(`/api/agents/${id}`, { method: 'DELETE' })
  await assertOk(res)
}

// ── Tools API ─────────────────────────────────────────────────────────────────

export async function getTools(): Promise<ToolInfo[]> {
  try {
    const res = await fetch('/api/tools')
    if (!res.ok) return []
    const data = await res.json()
    return Array.isArray(data.tools) ? data.tools : []
  } catch { return [] }
}
