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
  finalStatus?: 'completed' | 'user_cancelled' | 'timeout'
  commands: CommandResult[]
  usage?: TokenUsage | null
}

export interface HistoryMessage {
  role: 'user' | 'assistant'
  content: string
}

export type ProviderName = 'ollama'

export interface Session {
  id: string
  title: string
  createdAt: string
  updatedAt: string
}

export interface OllamaSettings {
  model: string
  host: string
}

export interface Settings {
  provider: ProviderName
  model: string
  contextWindowTokens?: number
  ollamaHost?: string
  ollama: OllamaSettings
}

export interface Agent {
  id: string
  name: string
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

export interface ToolCallEntry {
  toolName: string
  command: string
  output: string
  exitCode: number
  wasExecuted: boolean
  status: 'running' | 'success' | 'error' | 'skipped' | 'timeout' | 'user_cancelled'
}
