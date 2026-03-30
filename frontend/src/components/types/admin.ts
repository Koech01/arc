export interface AdminStats {
  totalUsers: number;
  activeUsers: number;
  newUsersThisWeek: number;
  activeLLMs: number;
  newLLMsThisWeek: number;
  totalExecutions: number;
  executionsToday: number;
}

export interface User {
  id: string;
  email: string;
  role: 'Admin' | 'User';
  status: 'Active' | 'Inactive';
  createdAt: string;
}

export interface SystemHealthComponent {
  name: string;
  status: 'Healthy' | 'Degraded' | 'Down';
  uptime: number;
  responseTime: number;
}

// User Management
export interface AdminUserDetail {
  id: string;
  username: string;
  email: string;
  role: 'Admin' | 'User';
  status: 'Active' | 'Inactive';
  createdAt: string;
  isLockedOut: boolean;
  lockedUntilUtc: string | null;
  failedLoginAttempts: number;
  isDeleted: boolean;
  deletedAt: string | null;
  firstname: string | null;
}

export interface AdminUserPage {
  users: AdminUserDetail[];
  totalCount: number;
  limit: number;
  offset: number;
}

export interface LoginHistoryEntry {
  id: number;
  timestampUtc: string;
  success: boolean;
  failureReason: string | null;
  ipAddress: string | null;
  userAgent: string | null;
}

// System-wide Executions
export interface AdminExecutionRow {
  executionId: string;
  userId: string;
  userEmail: string;
  status: string;
  createdAtUtc: string;
  taskCount: number;
  executionTimeMs: number;
  workflowName: string | null;
}

export interface AdminExecutionPage {
  executions: AdminExecutionRow[];
  totalCount: number;
  limit: number;
  offset: number;
}

// LLM Configurations
export interface AdminLLMConfig {
  id: string;
  name: string;
  model: string;
  baseUrl: string;
  isActive: boolean;
  createdAt: string;
  ownerEmail: string;
}

// Webhooks
export interface AdminWebhook {
  id: string;
  url: string;
  events: string[];
  isActive: boolean;
  createdBy: string;
  createdAt: string;
}

export interface AdminWebhookPage {
  webhooks: AdminWebhook[];
  totalCount: number;
  limit: number;
  offset: number;
}

// Cache Management
export interface AdminCacheStats {
  totalEntries: number;
  expiredEntries: number;
  activeEntries: number;
  oldestEntryUtc: string | null;
  newestEntryUtc: string | null;
}

// Maintenance Mode
export interface MaintenanceModeStatus {
  isEnabled: boolean;
  enabledBy: string | null;
  enabledAtUtc: string | null;
  reason: string | null;
}

// System Configuration
export interface SystemConfig {
  databaseProvider: string;
  lLMDefaultProvider: string;
  lLMDefaultModel: string;
  jwtExpirationMinutes: number;
  rateLimitPermitLimit: number;
  rateLimitWindowSeconds: number;
  maintenanceModeEnabled: boolean;
  environment: string;
  apiVersion: string;
}

// Admin Audit Log
export interface AdminAuditEntry {
  id: number;
  adminUserId: string;
  adminAuditAction: string;
  timestampUtc: string;
  targetUserId: string | null;
  detail: string | null;
  ipAddress: string | null;
  userAgent: string | null;
}