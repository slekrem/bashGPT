import {
  getSessions, getSession, putSession, deleteSession,
  clearSessions, createSession, loadHistory,
} from './api'
import type { Session } from './types'
import { SESSION_ID_PREFIX } from './constants'
import {
  LIVE_SESSION_ID, createLiveSession, historyToSnapshot,
  readLocalSessions, upsertSession, writeLocalSessions,
  toSession, type LocalSession, type SnapshotMessage,
} from './session-history'

export class SessionManager {
  private _localSessions: LocalSession[] = []
  private _useFallback = false

  get useFallback()   { return this._useFallback }
  get localSessions() { return this._localSessions }

  async init(): Promise<{ sessions: Session[], activeId: string | null }> {
    const serverSessions = await getSessions()
    this._useFallback = serverSessions === null

    if (!this._useFallback) {
      await this._migrateLocalSessionsToServer()
      const fresh = await getSessions() ?? []
      if (fresh.length === 0) {
        const s = await createSession()
        return { sessions: await getSessions() ?? [], activeId: s?.id ?? null }
      }
      return { sessions: fresh, activeId: fresh[0].id }
    }

    this._localSessions = readLocalSessions()
    if (!this._localSessions.some(s => s.id === LIVE_SESSION_ID)) {
      try {
        const history = await loadHistory()
        if (history.length > 0)
          this._localSessions.unshift(createLiveSession(historyToSnapshot(history)))
      } catch {}
    }
    writeLocalSessions(this._localSessions)
    const sessions = this._localSessions.map(toSession)
    return { sessions, activeId: sessions[0]?.id ?? null }
  }

  async loadSession(id: string): Promise<{
    messages: SnapshotMessage[]
    enabledTools?: string[]
    agentId?: string | null
    isArchived: boolean
  } | null> {
    if (this._useFallback) {
      const s = this._localSessions.find(ls => ls.id === id)
      if (!s) return null
      return { messages: s.messages, isArchived: id !== LIVE_SESSION_ID }
    }
    const s = await getSession(id)
    if (!s) return null
    return {
      messages: s.messages ?? [],
      enabledTools: s.enabledTools ?? undefined,
      agentId: s.agentId ?? null,
      isArchived: false,
    }
  }

  async prepareNewChat(
    currentSnapshot: SnapshotMessage[],
    currentActiveId: string | null,
  ): Promise<{ sessions: Session[], activeId: string | null }> {
    if (this._useFallback) {
      if (currentSnapshot.length > 0 && currentActiveId === LIVE_SESSION_ID) {
        const archivedId = `${SESSION_ID_PREFIX}${Date.now()}`
        this._localSessions = upsertSession(this._localSessions, archivedId, currentSnapshot)
      }
      this._localSessions = upsertSession(this._localSessions, LIVE_SESSION_ID, [])
      writeLocalSessions(this._localSessions)
      return { sessions: this._localSessions.map(toSession), activeId: LIVE_SESSION_ID }
    }
    if ((!currentSnapshot || currentSnapshot.length === 0) && currentActiveId)
      await deleteSession(currentActiveId)
    const newSession = await createSession()
    return { sessions: await getSessions() ?? [], activeId: newSession?.id ?? null }
  }

  async persistMessages(
    id: string,
    msgs: SnapshotMessage[],
  ): Promise<Session[]> {
    if (this._useFallback) {
      this._localSessions = upsertSession(this._localSessions, id, msgs)
      writeLocalSessions(this._localSessions)
      return this._localSessions.map(toSession)
    }
    return await getSessions() ?? []
  }

  async activateArchived(archivedId: string): Promise<{ sessions: Session[], activeId: string }> {
    const existingLive = this._localSessions.find(s => s.id === LIVE_SESSION_ID)
    let sessions = this._localSessions.filter(s => s.id !== LIVE_SESSION_ID)
    if (existingLive && existingLive.messages.length > 0)
      sessions = [...sessions, { ...existingLive, id: `${SESSION_ID_PREFIX}${Date.now()}` }]
    const now = new Date().toISOString()
    this._localSessions = sessions
      .map(s => s.id === archivedId
        ? { ...s, id: LIVE_SESSION_ID, updatedAt: now }
        : s)
      .sort((a, b) => b.updatedAt.localeCompare(a.updatedAt))
    writeLocalSessions(this._localSessions)
    return { sessions: this._localSessions.map(toSession), activeId: LIVE_SESSION_ID }
  }

  async clearAll(): Promise<void> {
    if (this._useFallback) {
      this._localSessions = []
      writeLocalSessions([])
    } else {
      await clearSessions()
    }
  }

  ensureLiveSession(): Session[] {
    if (!this._useFallback) return []
    if (!this._localSessions.some(s => s.id === LIVE_SESSION_ID))
      this._localSessions = upsertSession(this._localSessions, LIVE_SESSION_ID, [])
    writeLocalSessions(this._localSessions)
    return this._localSessions.map(toSession)
  }

  private async _migrateLocalSessionsToServer(): Promise<void> {
    const local = readLocalSessions()
    if (local.length === 0) return
    const existing = await getSessions() ?? []
    if (existing.length > 0) { writeLocalSessions([]); return }
    for (const s of local)
      await putSession(s.id, {
        title: s.title,
        messages: s.messages,
        createdAt: s.createdAt,
      })
    writeLocalSessions([])
  }
}
