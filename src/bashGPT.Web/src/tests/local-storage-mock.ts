import { vi } from 'vitest'

export interface LocalStorageMock {
  getItem: ReturnType<typeof vi.fn<(key: string) => string | null>>
  setItem: ReturnType<typeof vi.fn<(key: string, value: string) => void>>
  removeItem: ReturnType<typeof vi.fn<(key: string) => void>>
  _store: Record<string, string>
}

export function makeLocalStorageMock(): LocalStorageMock {
  const store: Record<string, string> = {}
  return {
    getItem: vi.fn((key: string) => store[key] ?? null),
    setItem: vi.fn((key: string, value: string) => { store[key] = value }),
    removeItem: vi.fn((key: string) => { delete store[key] }),
    _store: store,
  }
}
