import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  readLocalSessions,
  titleFromMessages,
  upsertSession,
  writeLocalSessions,
  type LocalSession,
} from '../session-history'

// happy-dom v20 does not expose a full Storage API for localStorage;
// we provide a minimal mock so that getItem / setItem / removeItem work.
const STORAGE_KEY = 'bashgpt_sessions_v2'

function makeLocalStorageMock() {
  const store: Record<string, string> = {}
  return {
    getItem:    vi.fn((key: string) => store[key] ?? null),
    setItem:    vi.fn((key: string, value: string) => { store[key] = value }),
    removeItem: vi.fn((key: string) => { delete store[key] }),
    _store: store,
  }
}

let lsMock: ReturnType<typeof makeLocalStorageMock>

beforeEach(() => {
  lsMock = makeLocalStorageMock()
  vi.stubGlobal('localStorage', lsMock)
})

afterEach(() => {
  vi.unstubAllGlobals()
})

// ── readLocalSessions ─────────────────────────────────────────────────────────

describe('readLocalSessions', () => {
  it('returns an empty array when localStorage is empty', () => {
    expect(readLocalSessions()).toEqual([])
  })

  it('returns parsed sessions when valid data is stored', () => {
    const sessions: LocalSession[] = [{
      id: 's-1',
      title: 'Test',
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
      messages: [],
    }]
    lsMock._store[STORAGE_KEY] = JSON.stringify(sessions)
    expect(readLocalSessions()).toEqual(sessions)
  })

  it('returns an empty array on malformed JSON', () => {
    lsMock._store[STORAGE_KEY] = '{invalid json}'
    expect(readLocalSessions()).toEqual([])
  })

  it('filters out entries that are missing required fields', () => {
    const data = [
      { id: 's-valid', title: 'Valid', createdAt: '', updatedAt: '', messages: [] },
      { title: 'Missing id', messages: [] },
      { id: 42, messages: [] }, // id is not a string
    ]
    lsMock._store[STORAGE_KEY] = JSON.stringify(data)
    expect(readLocalSessions()).toHaveLength(1)
    expect(readLocalSessions()[0].id).toBe('s-valid')
  })
})

// ── upsertSession ─────────────────────────────────────────────────────────────

describe('upsertSession', () => {
  it('inserts a new session when the ID does not yet exist', () => {
    const result = upsertSession([], 's-new', [{ role: 'user', content: 'Hello' }])
    expect(result).toHaveLength(1)
    expect(result[0].id).toBe('s-new')
    expect(result[0].title).toBe('Hello')
  })

  it('updates title and messages of an existing session', () => {
    const initial = upsertSession([], 's-1', [{ role: 'user', content: 'First' }])
    const updated = upsertSession(initial, 's-1', [{ role: 'user', content: 'Updated message' }])
    expect(updated).toHaveLength(1)
    expect(updated[0].title).toBe('Updated message')
  })
})

// ── titleFromMessages ─────────────────────────────────────────────────────────

describe('titleFromMessages', () => {
  it('returns the first user message as the title', () => {
    expect(titleFromMessages([
      { role: 'assistant', content: 'Hi' },
      { role: 'user', content: 'Show files' },
    ])).toBe('Show files')
  })

  it('truncates titles longer than 40 characters', () => {
    const long = 'a'.repeat(50)
    expect(titleFromMessages([{ role: 'user', content: long }])).toBe('a'.repeat(40) + '…')
  })

  it('returns "Neuer Chat" when there are no user messages', () => {
    expect(titleFromMessages([])).toBe('Neuer Chat')
  })
})

// ── writeLocalSessions (smoke test) ──────────────────────────────────────────

describe('writeLocalSessions', () => {
  it('persists sessions to localStorage', () => {
    const sessions: LocalSession[] = [{
      id: 's-1', title: 'Saved', createdAt: '', updatedAt: '', messages: [],
    }]
    writeLocalSessions(sessions)
    expect(lsMock.setItem).toHaveBeenCalledWith(STORAGE_KEY, JSON.stringify(sessions))
  })
})
