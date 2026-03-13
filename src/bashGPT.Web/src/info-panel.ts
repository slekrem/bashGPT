import type {
  Agent,
  FullShellContext,
  InfoPanelItem,
  InfoPanelSection,
  Settings,
  TokenUsage,
  ToolInfo,
} from './types'

interface CommandStats {
  total: number
  success: number
  error: number
  skipped: number
}

export interface InfoPanelSnapshot {
  context: FullShellContext | null
  settings: Settings | null
  messageCount: number
  commandStats: CommandStats
  tokenUsage: TokenUsage | null
  activeAgent: Agent | null
  enabledTools: ToolInfo[]
}

export function buildInfoPanelSections(snapshot: InfoPanelSnapshot): InfoPanelSection[] {
  return [
    buildSessionSection(snapshot),
    buildRuntimeSection(snapshot.context),
    buildModelSection(snapshot.settings, snapshot.tokenUsage),
    buildAgentSection(snapshot.activeAgent),
    buildToolsSection(snapshot.enabledTools),
  ].filter((section): section is InfoPanelSection => section !== null)
}

function buildSessionSection(snapshot: InfoPanelSnapshot): InfoPanelSection {
  const inputTokens = snapshot.tokenUsage?.inputTokens ?? 0
  const outputTokens = snapshot.tokenUsage?.outputTokens ?? 0
  const cachedInputTokens = snapshot.tokenUsage?.cachedInputTokens ?? 0
  const totalTokens = snapshot.tokenUsage?.totalTokens ?? (inputTokens + outputTokens)

  const items: InfoPanelItem[] = [
    { label: 'Nachrichten', value: String(snapshot.messageCount), source: 'session' },
    {
      label: 'Agent',
      value: snapshot.activeAgent?.name ?? 'Kein Agent aktiv',
      tone: snapshot.activeAgent ? 'accent' : 'muted',
      source: snapshot.activeAgent ? `agent:${snapshot.activeAgent.id}` : 'session',
    },
    {
      label: 'Tools',
      value: snapshot.enabledTools.length > 0
        ? snapshot.enabledTools.map(tool => tool.name).join(', ')
        : 'Keine aktiv',
      tone: snapshot.enabledTools.length > 0 ? 'accent' : 'muted',
      source: 'session',
    },
    {
      label: 'Befehle',
      value: snapshot.commandStats.total > 0
        ? `${snapshot.commandStats.success} ok, ${snapshot.commandStats.error} Fehler, ${snapshot.commandStats.skipped} ausgelassen`
        : 'Keine',
      tone: snapshot.commandStats.error > 0 ? 'error' : snapshot.commandStats.success > 0 ? 'success' : 'muted',
      source: 'session',
    },
  ]

  if (totalTokens > 0) {
    items.push({
      label: 'Tokens',
      value: cachedInputTokens > 0
        ? `${totalTokens.toLocaleString()} gesamt (${inputTokens.toLocaleString()} in, ${outputTokens.toLocaleString()} out, ${cachedInputTokens.toLocaleString()} cached)`
        : `${totalTokens.toLocaleString()} gesamt (${inputTokens.toLocaleString()} in, ${outputTokens.toLocaleString()} out)`,
      source: 'session',
    })
  }

  return {
    id: 'session',
    title: 'Session',
    source: 'session',
    items,
  }
}

function buildRuntimeSection(context: FullShellContext | null): InfoPanelSection | null {
  if (!context) return null

  const items: InfoPanelItem[] = [
    { label: 'Arbeitsordner', value: context.cwd, tone: 'accent', source: 'system' },
    { label: 'OS', value: context.os, source: 'system' },
    { label: 'Shell', value: context.shell, tone: 'accent', source: 'system' },
  ]

  if (context.git) {
    items.push(
      { label: 'Git Branch', value: context.git.branch, tone: 'accent', source: 'system' },
      {
        label: 'Git Änderungen',
        value: context.git.changedFilesCount === 0
          ? 'Keine'
          : `${context.git.changedFilesCount} Datei${context.git.changedFilesCount === 1 ? '' : 'en'}`,
        tone: context.git.changedFilesCount > 0 ? 'default' : 'muted',
        source: 'system',
      },
    )

    if (context.git.lastCommit) {
      items.push({
        label: 'Letzter Commit',
        value: context.git.lastCommit,
        source: 'system',
      })
    }
  }

  return {
    id: 'runtime',
    title: 'Laufzeit',
    source: 'system',
    items,
  }
}

function buildModelSection(settings: Settings | null, tokenUsage: TokenUsage | null): InfoPanelSection | null {
  if (!settings) return null

  const contextWindow = settings.contextWindowTokens ?? resolveContextWindow(settings.model)
  const usedTokens = tokenUsage?.totalTokens ?? ((tokenUsage?.inputTokens ?? 0) + (tokenUsage?.outputTokens ?? 0))
  const usageValue = contextWindow && usedTokens > 0
    ? `${usedTokens.toLocaleString()} / ${contextWindow.toLocaleString()} (${Math.min(100, Math.round((usedTokens / contextWindow) * 100))}%)`
    : contextWindow
      ? `${contextWindow.toLocaleString()} Tokens`
      : 'Unbekannt'

  const items: InfoPanelItem[] = [
    { label: 'Provider', value: settings.provider, tone: 'accent', source: 'system' },
    { label: 'Modell', value: settings.model, tone: 'accent', source: 'system' },
    { label: 'Kontextfenster', value: usageValue, source: 'system' },
  ]

  if (settings.provider === 'ollama') {
    items.push(
      { label: 'Host', value: settings.ollama.host, source: 'system' },
      { label: 'Temperatur', value: formatNumber(settings.ollama.temperature ?? 0.2), source: 'system' },
      { label: 'top_p', value: formatNumber(settings.ollama.topP ?? 0.9), source: 'system' },
    )
  } else {
    items.push(
      { label: 'Base URL', value: settings.cerebras.baseUrl ?? 'https://api.cerebras.ai/v1', source: 'system' },
      { label: 'Temperatur', value: formatNumber(settings.cerebras.temperature ?? 0.2), source: 'system' },
      { label: 'top_p', value: formatNumber(settings.cerebras.topP ?? 0.9), source: 'system' },
      { label: 'Reasoning', value: settings.cerebras.reasoningEffort ?? 'medium', source: 'system' },
    )
  }

  return {
    id: 'model',
    title: 'Modell',
    source: 'system',
    items,
  }
}

function buildAgentSection(agent: Agent | null): InfoPanelSection | null {
  if (!agent) return null

  const items: InfoPanelItem[] = [
    { label: 'Name', value: agent.name, tone: 'accent', source: `agent:${agent.id}` },
    { label: 'ID', value: agent.id, tone: 'muted', source: `agent:${agent.id}` },
    {
      label: 'Tools',
      value: agent.enabledTools.length > 0 ? agent.enabledTools.join(', ') : 'Keine Vorgabe',
      source: `agent:${agent.id}`,
    },
  ]

  if (agent.systemPrompt?.trim()) {
    items.push({
      label: 'Prompt',
      value: summarize(agent.systemPrompt.trim(), 160),
      source: `agent:${agent.id}`,
    })
  }

  return {
    id: 'agent',
    title: 'Agent',
    source: `agent:${agent.id}`,
    items,
  }
}

function buildToolsSection(enabledTools: ToolInfo[]): InfoPanelSection | null {
  if (enabledTools.length === 0) return null

  return {
    id: 'tools',
    title: 'Aktive Tools',
    source: 'session',
    items: enabledTools.map(tool => ({
      label: tool.name,
      value: summarize(tool.description || 'Ohne Beschreibung', 120),
      tone: 'accent',
      source: `tool:${tool.name}`,
    })),
  }
}

function resolveContextWindow(model: string | undefined): number | null {
  if (!model) return null
  const normalized = model.trim().toLowerCase()
  if (!normalized) return null

  const kMatch = normalized.match(/(?:^|[-_:])(\d{1,4})k(?:$|[-_:])/)
  if (!kMatch) return null

  const k = Number(kMatch[1])
  return Number.isFinite(k) && k > 0 ? k * 1024 : null
}

function formatNumber(value: number | undefined, fractionDigits = 2): string {
  if (value == null) return '-'
  return Number.isInteger(value)
    ? value.toString()
    : value.toFixed(fractionDigits).replace(/\.?0+$/, '')
}

function summarize(value: string, maxLength: number): string {
  return value.length > maxLength
    ? `${value.slice(0, maxLength - 1).trimEnd()}…`
    : value
}
