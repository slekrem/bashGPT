import type { CommandResult, HistoryMessage, Session, TokenUsage } from './types'

export const LIVE_SESSION_ID = 'current'
const LOCAL_SESSIONS_KEY = 'bashgpt_sessions_v2'
const MAX_LOCAL_SESSIONS = 20

export interface SnapshotMessage {
  role: 'user' | 'assistant'
  content: string
  commands?: CommandResult[]
  usage?: TokenUsage
}

export interface LocalSession extends Session {
  messages: SnapshotMessage[]
}

export function historyToSnapshot(history: HistoryMessage[]): SnapshotMessage[] {
  return history.map(h => ({ role: h.role, content: h.content }))
}

export function titleFromMessages(messages: SnapshotMessage[]): string {
  const firstUser = messages.find(m => m.role === 'user')?.content?.trim()
  if (!firstUser) return 'Neuer Chat'
  return firstUser.length > 40 ? `${firstUser.slice(0, 40)}…` : firstUser
}

export function createLiveSession(messages: SnapshotMessage[]): LocalSession {
  const now = new Date().toISOString()
  return {
    id: LIVE_SESSION_ID,
    title: titleFromMessages(messages),
    createdAt: now,
    updatedAt: now,
    messages,
  }
}

export function toSession(session: LocalSession): Session {
  return {
    id: session.id,
    title: session.title,
    createdAt: session.createdAt,
    updatedAt: session.updatedAt,
  }
}

export function readLocalSessions(): LocalSession[] {
  try {
    const raw = localStorage.getItem(LOCAL_SESSIONS_KEY)
    if (!raw) return []
    const parsed = JSON.parse(raw)
    if (!Array.isArray(parsed)) return []
    return parsed.filter(s => s && typeof s.id === 'string' && Array.isArray(s.messages))
  } catch {
    return []
  }
}

export function writeLocalSessions(sessions: LocalSession[]) {
  localStorage.setItem(LOCAL_SESSIONS_KEY, JSON.stringify(sessions))
}

export function upsertSession(
  sessions: LocalSession[],
  id: string,
  messages: SnapshotMessage[],
): LocalSession[] {
  const now = new Date().toISOString()
  const idx = sessions.findIndex(s => s.id === id)

  if (idx >= 0) {
    const existing = sessions[idx]
    sessions[idx] = {
      ...existing,
      title: titleFromMessages(messages),
      updatedAt: now,
      messages,
    }
  } else {
    sessions.unshift({
      id,
      title: titleFromMessages(messages),
      createdAt: now,
      updatedAt: now,
      messages,
    })
  }

  return sessions
    .sort((a, b) => {
      const c = b.updatedAt.localeCompare(a.updatedAt)
      return c !== 0 ? c : b.createdAt.localeCompare(a.createdAt)
    })
    .slice(0, MAX_LOCAL_SESSIONS)
}
