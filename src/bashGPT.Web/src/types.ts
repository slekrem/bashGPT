export interface CommandResult {
  command: string
  exitCode: number
  output: string
  wasExecuted: boolean
}

export interface ChatResponse {
  response: string
  usedToolCalls: boolean
  logs: string[]
  commands: CommandResult[]
}

export interface HistoryMessage {
  role: 'user' | 'assistant'
  content: string
}

export type ExecMode = 'ask' | 'dry-run' | 'auto-exec' | 'no-exec'
