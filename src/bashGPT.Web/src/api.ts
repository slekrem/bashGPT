import type { ChatResponse, ExecMode, HistoryMessage } from './types'

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

export async function sendChat(prompt: string, execMode: ExecMode): Promise<ChatResponse> {
  const res = await fetch('/api/chat', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ prompt, execMode }),
  })
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
