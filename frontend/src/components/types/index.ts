// ============================================
// Execution Types
// ============================================
export type LogLevel = 'info' | 'warning' | 'error' | 'debug';
export type ExecutionStatus = 'success' | 'failed' | 'running' | 'queued';
export type TaskStatus = 'success' | 'failed' | 'running' | 'queued' | 'skipped';


export interface ExecutionTaskSummary {
  taskId: string;
  taskName: string;
  executionOrder: number;
  status: 'success' | 'failed' | 'running';
  output?: string | null;
}

export interface Execution {
  id: string;
  status: ExecutionStatus;
  startedAt: string;
  completedAt?: string | null;
  duration: number;
  triggerType: string;
  workflowId?: string | null;
  workflowName: string;
  workflowDescription: string;
  tasks: ExecutionTaskSummary[];
}

export interface ExecutionListItem {
  id: string;
  status: 'completed' | 'failed' | 'running' | 'queued';
  totalTasks: number;
  duration: string;
  startedAt: string;
  workflowName: string;
  workflowDescription: string;
  tasks: ExecutionTaskSummary[];
  isArchived?: boolean;
}

export interface Task {
  id: string;
  name: string;
  status: TaskStatus;
  startedAt: string;
  completedAt?: string;
  duration: number;
  agentType: string;
  dependencies: string[];
  output?: string;
  error?: string;
}

export interface ExecutionLog {
  id: string;
  timestamp: string;
  level: LogLevel;
  message: string;
  taskId?: string;
}

export interface ExecutionOutput {
  key: string;
  value: string;
  type: string;
}

export interface ExecutionMetadata {
  executionId: string;
  workflowId: string;
  workflowVersion: string;
  triggeredBy: string;
  environment: string;
  totalTasks: number;
  successfulTasks: number;
  failedTasks: number;
}

export interface ArchiveAuditEntry {
  id: number;
  executionId: string;
  action: 'ARCHIVE' | 'UNARCHIVE' | 'PURGE';
  performedBy: string;
  performedAt: string;
  reason?: string;
}