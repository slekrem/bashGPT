import { describe, expect, it } from 'vitest'
import { buildInfoPanelSections } from '../info-panel'
import type { Agent, FullShellContext, Settings, ToolInfo, TokenUsage } from '../types'

describe('buildInfoPanelSections', () => {
  it('builds generic sections for session, runtime, model, agent, and tools', () => {
    const context: FullShellContext = {
      user: 'stefan',
      host: 'workstation',
      cwd: 'c:\\repo\\bashGPT',
      os: 'Windows',
      shell: 'powershell',
      git: {
        branch: 'feat/info',
        lastCommit: 'abc123',
        changedFilesCount: 2,
      },
    }
    const settings: Settings = {
      provider: 'cerebras',
      model: 'gpt-oss-120k',
      execMode: 'ask',
      forceTools: false,
      commandTimeoutSeconds: 30,
      loopDetectionEnabled: true,
      maxToolCallRounds: 8,
      rateLimiting: { enabled: false, maxRequestsPerMinute: 30, agentRequestDelayMs: 0 },
      cerebras: { model: 'gpt-oss-120k' },
      ollama: { model: 'llama3', host: 'http://localhost:11434' },
    }
    const tokenUsage: TokenUsage = {
      inputTokens: 100,
      outputTokens: 40,
      totalTokens: 140,
      cachedInputTokens: 10,
    }
    const activeAgent: Agent = {
      id: 'dev',
      name: 'Dev Agent',
      systemPrompt: 'Du bist ein spezialisierter Entwicklungsagent.',
      enabledTools: ['shell_exec', 'git_status'],
    }
    const enabledTools: ToolInfo[] = [
      {
        name: 'shell_exec',
        description: 'Runs a shell command in the current workspace.',
        parameters: [],
      },
    ]

    const sections = buildInfoPanelSections({
      context,
      settings,
      messageCount: 4,
      commandStats: { total: 3, success: 2, error: 1, skipped: 0 },
      tokenUsage,
      activeAgent,
      enabledTools,
    })

    expect(sections.map(section => section.id)).toEqual(['session', 'runtime', 'model', 'agent', 'tools'])
    expect(sections[0].items.some(item => item.label === 'Agent' && item.value === 'Dev Agent')).toBe(true)
    expect(sections[3].items.some(item => item.label === 'Prompt')).toBe(true)
    expect(sections[4].items).toHaveLength(1)
  })

  it('keeps the panel useful without agent and tools', () => {
    const sections = buildInfoPanelSections({
      context: null,
      settings: null,
      messageCount: 1,
      commandStats: { total: 0, success: 0, error: 0, skipped: 0 },
      tokenUsage: null,
      activeAgent: null,
      enabledTools: [],
    })

    expect(sections).toHaveLength(1)
    expect(sections[0].id).toBe('session')
    expect(sections[0].items.some(item => item.label === 'Agent' && item.value === 'Kein Agent aktiv')).toBe(true)
  })
})
