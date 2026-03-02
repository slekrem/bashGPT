import { beforeAll, describe, expect, it } from 'vitest'
import type { CommandResult, TokenUsage } from '../types'

// Private methods are accessed via `any` cast – a common pattern in TS unit tests.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
let el: any

describe('ChatView – private logic', () => {
  beforeAll(async () => {
    const mod = await import('../components/chat-view')
    el = new mod.ChatView()
  })

  // ── _commandStats getter ─────────────────────────────────────────────────

  it('_commandStats aggregates success / error / skipped correctly', () => {
    const commands: CommandResult[] = [
      { command: 'ls',   exitCode: 0, output: '', wasExecuted: true  }, // success
      { command: 'rm',   exitCode: 1, output: '', wasExecuted: true  }, // error
      { command: 'cat',  exitCode: 0, output: '', wasExecuted: false }, // skipped
    ]
    el._messages = [{ id: 1, role: 'user', content: 'test', commands }]
    expect(el._commandStats).toEqual({ total: 3, success: 1, error: 1, skipped: 1 })
  })

  it('_commandStats returns all-zeros when messages have no commands', () => {
    el._messages = [{ id: 1, role: 'user', content: 'hello' }]
    expect(el._commandStats).toEqual({ total: 0, success: 0, error: 0, skipped: 0 })
  })

  // ── _sumTokenUsage method ────────────────────────────────────────────────

  it('_sumTokenUsage sums inputTokens / outputTokens / cachedInputTokens across messages', () => {
    const usage1: TokenUsage = { inputTokens: 10, outputTokens: 20, totalTokens: 30 }
    const usage2: TokenUsage = { inputTokens: 5,  outputTokens: 15, totalTokens: 20, cachedInputTokens: 3 }
    const messages = [
      { id: 1, role: 'user',      content: 'a', usage: usage1 },
      { id: 2, role: 'assistant', content: 'b', usage: usage2 },
    ]
    const result: TokenUsage = el._sumTokenUsage(messages)
    expect(result.inputTokens).toBe(15)
    expect(result.outputTokens).toBe(35)
    expect(result.totalTokens).toBe(50)
    expect(result.cachedInputTokens).toBe(3)
  })
})
