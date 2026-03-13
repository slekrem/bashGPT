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

  it('builds model section for ollama provider with host and temperature', () => {
    const settings: Settings = {
      provider: 'ollama',
      model: 'llama3',
      execMode: 'no-exec',
      forceTools: false,
      commandTimeoutSeconds: 30,
      loopDetectionEnabled: true,
      maxToolCallRounds: 8,
      rateLimiting: { enabled: false, maxRequestsPerMinute: 30, agentRequestDelayMs: 0 },
      cerebras: { model: 'gpt-oss-120k' },
      ollama: { model: 'llama3', host: 'http://localhost:11434', temperature: 0.5, topP: 0.8 },
    }

    const sections = buildInfoPanelSections({
      context: null,
      settings,
      messageCount: 0,
      commandStats: { total: 0, success: 0, error: 0, skipped: 0 },
      tokenUsage: null,
      activeAgent: null,
      enabledTools: [],
    })

    const modelSection = sections.find(s => s.id === 'model')!
    expect(modelSection).toBeDefined()
    expect(modelSection.items.some(item => item.label === 'Host' && item.value === 'http://localhost:11434')).toBe(true)
    expect(modelSection.items.some(item => item.label === 'Temperatur' && item.value === '0.5')).toBe(true)
    expect(modelSection.items.some(item => item.label === 'top_p' && item.value === '0.8')).toBe(true)
    expect(modelSection.items.some(item => item.label === 'Reasoning')).toBe(false)
  })

  it('shows context window usage percentage when token usage and context window are known', () => {
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
      inputTokens: 1000,
      outputTokens: 200,
      totalTokens: 1200,
      cachedInputTokens: 0,
    }

    const sections = buildInfoPanelSections({
      context: null,
      settings,
      messageCount: 5,
      commandStats: { total: 0, success: 0, error: 0, skipped: 0 },
      tokenUsage,
      activeAgent: null,
      enabledTools: [],
    })

    const modelSection = sections.find(s => s.id === 'model')!
    const contextItem = modelSection.items.find(item => item.label === 'Kontextfenster')!
    // 120k = 122880 tokens; 1200/122880 ≈ 1%
    expect(contextItem.value).toContain('1,200')
    expect(contextItem.value).toContain('122,880')
    expect(contextItem.value).toContain('%')
  })

  it('omits runtime section when context is null', () => {
    const sections = buildInfoPanelSections({
      context: null,
      settings: null,
      messageCount: 0,
      commandStats: { total: 0, success: 0, error: 0, skipped: 0 },
      tokenUsage: null,
      activeAgent: null,
      enabledTools: [],
    })

    expect(sections.some(s => s.id === 'runtime')).toBe(false)
  })

  it('omits git items when context has no git info', () => {
    const context: FullShellContext = {
      user: 'alice',
      host: 'laptop',
      cwd: '/home/alice/project',
      os: 'Linux',
      shell: 'bash',
      git: null,
    }

    const sections = buildInfoPanelSections({
      context,
      settings: null,
      messageCount: 0,
      commandStats: { total: 0, success: 0, error: 0, skipped: 0 },
      tokenUsage: null,
      activeAgent: null,
      enabledTools: [],
    })

    const runtimeSection = sections.find(s => s.id === 'runtime')!
    expect(runtimeSection).toBeDefined()
    expect(runtimeSection.items.some(item => item.label.startsWith('Git'))).toBe(false)
  })

  it('uses error tone for commandStats when errors exist', () => {
    const sections = buildInfoPanelSections({
      context: null,
      settings: null,
      messageCount: 2,
      commandStats: { total: 3, success: 1, error: 2, skipped: 0 },
      tokenUsage: null,
      activeAgent: null,
      enabledTools: [],
    })

    const sessionSection = sections[0]
    const befehleItem = sessionSection.items.find(item => item.label === 'Befehle')!
    expect(befehleItem.tone).toBe('error')
  })

  it('uses success tone for commandStats when no errors', () => {
    const sections = buildInfoPanelSections({
      context: null,
      settings: null,
      messageCount: 2,
      commandStats: { total: 2, success: 2, error: 0, skipped: 0 },
      tokenUsage: null,
      activeAgent: null,
      enabledTools: [],
    })

    const sessionSection = sections[0]
    const befehleItem = sessionSection.items.find(item => item.label === 'Befehle')!
    expect(befehleItem.tone).toBe('success')
  })

  it('shows cached token info when cachedInputTokens > 0', () => {
    const sections = buildInfoPanelSections({
      context: null,
      settings: null,
      messageCount: 3,
      commandStats: { total: 0, success: 0, error: 0, skipped: 0 },
      tokenUsage: { inputTokens: 500, outputTokens: 100, totalTokens: 600, cachedInputTokens: 200 },
      activeAgent: null,
      enabledTools: [],
    })

    const tokensItem = sections[0].items.find(item => item.label === 'Tokens')!
    expect(tokensItem).toBeDefined()
    expect(tokensItem.value).toContain('cached')
  })

  it('omits agent section when no agent is active', () => {
    const sections = buildInfoPanelSections({
      context: null,
      settings: null,
      messageCount: 0,
      commandStats: { total: 0, success: 0, error: 0, skipped: 0 },
      tokenUsage: null,
      activeAgent: null,
      enabledTools: [],
    })

    expect(sections.some(s => s.id === 'agent')).toBe(false)
  })

  it('omits agent prompt item when systemPrompt is empty', () => {
    const agent: Agent = {
      id: 'minimal',
      name: 'Minimal Agent',
      systemPrompt: '',
      enabledTools: [],
    }

    const sections = buildInfoPanelSections({
      context: null,
      settings: null,
      messageCount: 0,
      commandStats: { total: 0, success: 0, error: 0, skipped: 0 },
      tokenUsage: null,
      activeAgent: agent,
      enabledTools: [],
    })

    const agentSection = sections.find(s => s.id === 'agent')!
    expect(agentSection).toBeDefined()
    expect(agentSection.items.some(item => item.label === 'Prompt')).toBe(false)
  })
})
