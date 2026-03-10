import { afterEach, describe, expect, it, vi } from 'vitest'
import { getContext, getSettings, getSessions } from '../api'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('getSessions', () => {
  it('returns null when fetch throws a network error', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline')))
    expect(await getSessions()).toBeNull()
  })

  it('returns the sessions array on a successful response', async () => {
    const sessions = [{ id: 's-1', title: 'Test', createdAt: '', updatedAt: '' }]
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ sessions }),
    }))
    expect(await getSessions()).toEqual(sessions)
  })

  it('returns null on a non-ok HTTP response', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false, status: 503 }))
    expect(await getSessions()).toBeNull()
  })
})

describe('getContext', () => {
  it('returns null when the response status is not ok (e.g. 404)', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false, status: 404 }))
    expect(await getContext()).toBeNull()
  })

  it('returns context data on a successful response', async () => {
    const ctx = { user: 'alice', host: 'mac', cwd: '/home', os: 'macOS', shell: 'zsh' }
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ctx,
    }))
    expect(await getContext()).toEqual(ctx)
  })
})

describe('getSettings', () => {
  it('returns null when fetch throws', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('network error')))
    expect(await getSettings()).toBeNull()
  })
})

