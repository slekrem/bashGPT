export interface CommandResult {
  command: string
  exitCode: number
  output: string
  wasExecuted: boolean
}

export interface TokenUsage {
  inputTokens: number
  outputTokens: number
  totalTokens?: number
  cachedInputTokens?: number
}

export interface ChatResponse {
  response: string
  usedToolCalls: boolean
  logs: string[]
  shellContext?: ShellContext
  commands: CommandResult[]
  usage?: TokenUsage | null
}

export interface ShellContext {
  user: string
  host: string
  cwd: string
}

export interface HistoryMessage {
  role: 'user' | 'assistant'
  content: string
}

export type ExecMode = 'ask' | 'dry-run' | 'auto-exec' | 'no-exec'
export type ProviderName = 'cerebras' | 'ollama'

export interface Session {
  id: string
  title: string
  createdAt: string
  updatedAt: string
  enabledTools?: string[]
}

export interface CerebrasSettings {
  model: string
  apiKey?: string
  hasApiKey?: boolean
  baseUrl?: string
  temperature?: number
  topP?: number
  maxCompletionTokens?: number
  seed?: number
  reasoningEffort?: string
}

export interface OllamaSettings {
  model: string
  host: string
  temperature?: number
  topP?: number
  seed?: number
}

export interface RateLimitingSettings {
  enabled: boolean
  maxRequestsPerMinute: number
  agentRequestDelayMs: number
}

export interface Settings {
  provider: ProviderName
  model: string
  contextWindowTokens?: number
  hasApiKey?: boolean
  apiKey?: string
  ollamaHost?: string
  execMode: ExecMode
  forceTools: boolean
  commandTimeoutSeconds: number
  loopDetectionEnabled: boolean
  maxToolCallRounds: number
  rateLimiting: RateLimitingSettings
  cerebras: CerebrasSettings
  ollama: OllamaSettings
}

export interface GitContext {
  branch: string
  lastCommit: string | null
  changedFilesCount: number
}

export interface FullShellContext {
  user: string
  host: string
  cwd: string
  os: string
  shell: string
  git?: GitContext | null
}

export interface Agent {
  id: string
  name: string
  systemPrompt?: string
  enabledTools: string[]
}

export interface ToolInfo {
  name: string
  description: string
  parameters: Array<{
    name: string
    type: string
    description: string
    required: boolean
  }>
}

export type AppView = 'dashboard' | 'chat' | 'settings' | 'agents' | 'tools'

export type ChatStatus = 'idle' | 'loading' | 'error'

export interface TerminalEntry {
  command: string
  output: string
  exitCode: number
  wasExecuted: boolean
  status: 'running' | 'success' | 'error' | 'skipped'
}
