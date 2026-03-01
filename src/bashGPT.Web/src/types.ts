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
  shellContext?: ShellContext
  commands: CommandResult[]
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

export interface Session {
  id: string
  title: string
  createdAt: string
  updatedAt: string
}

export interface Settings {
  provider: string
  model: string
  apiKey?: string
  ollamaHost?: string
  execMode: ExecMode
  forceTools: boolean
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

export type AppView = 'dashboard' | 'chat' | 'settings'

export type ChatStatus = 'idle' | 'loading' | 'error'

export interface TerminalEntry {
  command: string
  output: string
  exitCode: number
  wasExecuted: boolean
  status: 'running' | 'success' | 'error' | 'skipped'
}
