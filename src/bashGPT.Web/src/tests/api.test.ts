import { afterEach, describe, expect, it, vi } from 'vitest'
import { getContext, getSettings, getSessions, sendChat } from '../api'

afterEach(() => {
  vi.unstubAllGlobals()
})

// ── getSessions ───────────────────────────────────────────────────────────────

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

// ── getContext ────────────────────────────────────────────────────────────────

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

// ── getSettings ───────────────────────────────────────────────────────────────

describe('getSettings', () => {
  it('returns null when fetch throws', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('network error')))
    expect(await getSettings()).toBeNull()
  })
})

// ── sendChat ──────────────────────────────────────────────────────────────────

describe('sendChat', () => {
  it('throws a timeout error when the request is aborted', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation((_url: string, opts: RequestInit) => {
      // Simulate abort signal being triggered
      const signal = opts.signal as AbortSignal
      if (signal?.aborted) {
        return Promise.reject(new DOMException('Aborted', 'AbortError'))
      }
      return new Promise((_resolve, reject) => {
        signal?.addEventListener('abort', () =>
          reject(new DOMException('Aborted', 'AbortError'))
        )
      })
    }))

    // Use a very short timeout by directly aborting via controller
    const controller = new AbortController()
    controller.abort()

    // sendChat internally creates its own controller + timeout;
    // we can only test the error path by triggering an AbortError
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new DOMException('Aborted', 'AbortError')))
    await expect(sendChat('hello', 'auto-exec')).rejects.toThrow('Zeitlimit erreicht')
  })
})
