import type { ChatResponse, ExecMode, FullShellContext, HistoryMessage, Session, Settings } from './types'

const CHAT_TIMEOUT_MS = 120_000

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
