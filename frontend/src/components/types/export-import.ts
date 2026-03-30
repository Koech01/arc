export type ExecutedTaskStatus = 'Succeeded' | 'Failed' | 'Skipped';
export type ExecutionStatusType = 'Succeeded' | 'Failed';

export interface ExecutedTask {
  taskId: string;
  taskName: string;
  executionOrder: number;
  status: ExecutedTaskStatus;
  output: string;
}

export interface AuditLogEntry {
  sequence: number;
  timestampUtc: string;
  eventType: string;
  taskId?: string | null;
  message?: string | null;
}

export interface ExecutionExportPayload {
  executionId: string;
  userId: string;
  status: ExecutionStatusType;
  createdAtUtc: string;
  tasks: ExecutedTask[];
  auditLogs?: AuditLogEntry[];
}

export interface ExecutionExportItem {
  executionId: string;
  userId: string;
  status: ExecutionStatusType;
  createdAtUtc: string;
  totalTasks: number;
  completedTasks: number;
  failedTasks: number;
  jsonPayload: string;
}

export interface ExportFilter {
  status?: 'Succeeded' | 'Failed' | null;
  executionIdFilter?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  taskCountMin?: number | null;
  taskCountMax?: number | null;
}

export interface ExportRequest {
  status: 'Succeeded' | 'Failed' | null;
  startDate: string | null;
  endDate: string | null;
  minTaskCount: number | null;
  maxTaskCount: number | null;
  minExecutionTimeMs: number | null;
  maxExecutionTimeMs: number | null;
  limit: number;
  offset: number;
  format: 'json';
}

export interface ExportResponse {
  exports: ExecutionExportItem[];
  totalExported: number;
  exportedAt: string;
}

export interface ImportResult {
  executionId: string;
  importedExecutionId?: string;
  success: boolean;
  errorMessage?: string | null;
  importedAt: string;
}

export interface ImportRequest {
  jsonContent: string;
}

export interface ImportResponse {
  results: ImportResult[];
  totalImported: number;
  totalFailed: number;
  importedAt: string;
}