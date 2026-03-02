import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { SessionManager } from '../session-manager'
import { LIVE_SESSION_ID } from '../session-history'

// ── localStorage mock ─────────────────────────────────────────────────────────

function makeLocalStorageMock() {
  const store: Record<string, string> = {}
  return {
    getItem:    vi.fn((key: string) => store[key] ?? null),
    setItem:    vi.fn((key: string, value: string) => { store[key] = value }),
    removeItem: vi.fn((key: string) => { delete store[key] }),
    _store: store,
  }
}

// fetch mock that returns 503 → triggers localStorage fallback
const offlineFetch = vi.fn().mockResolvedValue({ ok: false, status: 503 })

beforeEach(() => {
  vi.stubGlobal('localStorage', makeLocalStorageMock())
  vi.stubGlobal('fetch', offlineFetch)
})

afterEach(() => {
  vi.unstubAllGlobals()
})

// ── init ──────────────────────────────────────────────────────────────────────

describe('SessionManager.init()', () => {
  it('activates localStorage fallback when server is unreachable', async () => {
    const sm = new SessionManager()
    await sm.init()
    expect(sm.useFallback).toBe(true)
  })

  it('returns empty sessions when localStorage is empty', async () => {
    const sm = new SessionManager()
    const { sessions, activeId } = await sm.init()
    expect(sessions).toHaveLength(0)
    expect(activeId).toBeNull()
  })

  it('returns existing localStorage sessions', async () => {
    const stored = [
      { id: 's-1', title: 'Test', createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z', messages: [] },
    ]
    localStorage.setItem('bashgpt_sessions_v2', JSON.stringify(stored))
    const sm = new SessionManager()
    const { sessions } = await sm.init()
    expect(sessions).toHaveLength(1)
    expect(sessions[0].id).toBe('s-1')
  })
})

// ── prepareNewChat ────────────────────────────────────────────────────────────

describe('SessionManager.prepareNewChat()', () => {
  it('archives the current session when it has messages', async () => {
    const sm = new SessionManager()
    await sm.init()
    const msgs = [{ role: 'user' as const, content: 'Hello' }]
    const { sessions } = await sm.prepareNewChat(msgs, LIVE_SESSION_ID, null)
    // 1 archived + 1 new live session
    expect(sessions.length).toBeGreaterThanOrEqual(2)
    const archivedSession = sessions.find(s => s.id !== LIVE_SESSION_ID)
    expect(archivedSession).toBeDefined()
  })

  it('does not archive an empty session', async () => {
    const sm = new SessionManager()
    await sm.init()
    const { sessions } = await sm.prepareNewChat([], LIVE_SESSION_ID, null)
    // Only the new live session
    expect(sessions).toHaveLength(1)
    expect(sessions[0].id).toBe(LIVE_SESSION_ID)
  })
})

// ── persistMessages ───────────────────────────────────────────────────────────

describe('SessionManager.persistMessages()', () => {
  it('updates localStorage and returns updated sessions', async () => {
    const sm = new SessionManager()
    await sm.init()
    const msgs = [{ role: 'user' as const, content: 'Saved message' }]
    const sessions = await sm.persistMessages(LIVE_SESSION_ID, msgs, null)
    expect(sessions).toHaveLength(1)
    expect(sessions[0].title).toBe('Saved message')
  })

  it('updates the title based on the first user message', async () => {
    const sm = new SessionManager()
    await sm.init()
    const msgs = [
      { role: 'assistant' as const, content: 'Hi there' },
      { role: 'user' as const, content: 'Show me the files' },
    ]
    const sessions = await sm.persistMessages(LIVE_SESSION_ID, msgs, null)
    expect(sessions[0].title).toBe('Show me the files')
  })
})

// ── activateArchived ──────────────────────────────────────────────────────────

describe('SessionManager.activateArchived()', () => {
  it('promotes an archived session to become the live session', async () => {
    const sm = new SessionManager()
    await sm.init()
    // Create an archived session via prepareNewChat
    const msgs = [{ role: 'user' as const, content: 'Old message' }]
    const { sessions: afterNew } = await sm.prepareNewChat(msgs, LIVE_SESSION_ID, null)
    const archivedId = afterNew.find(s => s.id !== LIVE_SESSION_ID)!.id

    const { sessions, activeId } = await sm.activateArchived(archivedId)
    expect(activeId).toBe(LIVE_SESSION_ID)
    expect(sessions.some(s => s.id === LIVE_SESSION_ID)).toBe(true)
  })

  it('stashes a non-empty live session when activating an archived one', async () => {
    const sm = new SessionManager()
    await sm.init()
    // Create archived session
    const msgs = [{ role: 'user' as const, content: 'Archived' }]
    const { sessions: afterNew } = await sm.prepareNewChat(msgs, LIVE_SESSION_ID, null)
    const archivedId = afterNew.find(s => s.id !== LIVE_SESSION_ID)!.id
    // Give the live session some content
    await sm.persistMessages(LIVE_SESSION_ID, [{ role: 'user', content: 'Live content' }])

    const { sessions } = await sm.activateArchived(archivedId)
    // Old live gets stashed → should have at least 2 sessions
    expect(sessions.length).toBeGreaterThanOrEqual(2)
  })
})

// ── clearAll ──────────────────────────────────────────────────────────────────

describe('SessionManager.clearAll()', () => {
  it('removes all local sessions and clears localStorage', async () => {
    const sm = new SessionManager()
    await sm.init()
    await sm.persistMessages(LIVE_SESSION_ID, [{ role: 'user', content: 'hi' }], null)
    await sm.clearAll()
    const { sessions } = await sm.init()
    expect(sessions).toHaveLength(0)
  })
})

// ── ensureLiveSession ─────────────────────────────────────────────────────────

describe('SessionManager.ensureLiveSession()', () => {
  it('adds a live session entry when none exists', async () => {
    const sm = new SessionManager()
    await sm.init()
    const sessions = sm.ensureLiveSession()
    expect(sessions.some(s => s.id === LIVE_SESSION_ID)).toBe(true)
  })

  it('returns empty array in server mode (noop)', async () => {
    // Simulate server mode by stubbing a successful fetch
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ sessions: [] }),
    }))
    const sm = new SessionManager()
    // Don't call init() – useFallback defaults to false
    const sessions = sm.ensureLiveSession()
    expect(sessions).toHaveLength(0)
  })
})
