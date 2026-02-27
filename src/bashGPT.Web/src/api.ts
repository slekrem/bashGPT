import type { ChatResponse, ExecMode, HistoryMessage } from './types'

export async function sendChat(prompt: string, execMode: ExecMode): Promise<ChatResponse> {
  const res = await fetch('/api/chat', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ prompt, execMode }),
  })
  if (!res.ok) {
    const data = await res.json().catch(() => ({}))
    throw new Error(data.error ?? `HTTP ${res.status}`)
  }
  return res.json()
}

export async function loadHistory(): Promise<HistoryMessage[]> {
  const res = await fetch('/api/history')
  const data = await res.json()
  return data.history ?? []
}

export async function resetHistory(): Promise<void> {
  await fetch('/api/reset', { method: 'POST' })
}
