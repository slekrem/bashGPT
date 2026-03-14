import { beforeAll, describe, expect, it } from 'vitest'
import type { CommandResult, TokenUsage } from '../types'

type TestMessage = {
  id: number
  role: 'user' | 'assistant'
  content: string
  commands?: CommandResult[]
  usage?: TokenUsage
}

type ChatViewUnderTest = {
  _chat: { messages: TestMessage[] }
  _sumTokenUsage: (messages: TestMessage[]) => TokenUsage
}

let el: ChatViewUnderTest

describe('ChatView – private logic', () => {
  beforeAll(async () => {
    const mod = await import('../components/chat-view')
    el = new mod.ChatView() as unknown as ChatViewUnderTest
  })

  // ── _sumTokenUsage method ────────────────────────────────────────────────

  it('_sumTokenUsage sums inputTokens / outputTokens / cachedInputTokens across messages', () => {
    const usage1: TokenUsage = { inputTokens: 10, outputTokens: 20, totalTokens: 30 }
    const usage2: TokenUsage = { inputTokens: 5,  outputTokens: 15, totalTokens: 20, cachedInputTokens: 3 }
    const messages: TestMessage[] = [
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
