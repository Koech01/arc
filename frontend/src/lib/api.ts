const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';


export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  role: 'User' | 'Admin';
}

export interface AuthResponse {
  userId: string;
  username: string;
  email: string;
  role: string;
  firstname?: string;
}

export interface UpdateProfileRequest {
  username: string;
  email: string;
  firstname?: string;
}

class ApiClient {
  private getHeaders(): HeadersInit {
    return {
      'Content-Type': 'application/json',
    };
  }

  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      headers: this.getHeaders(),
      credentials: 'include',
      body: JSON.stringify(data),
    });
    
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Login failed');
    }
    
    return response.json();
  }

  async register(data: RegisterRequest): Promise<AuthResponse> {
    const response = await fetch(`${API_BASE_URL}/auth/register`, {
      method: 'POST',
      headers: this.getHeaders(),
      credentials: 'include',
      body: JSON.stringify(data),
    });
    
    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: `HTTP ${response.status}: ${response.statusText}` }));
      throw new Error(error.message || 'Registration failed');
    }
    
    return response.json();
  }

  async logout(): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/auth/logout`, {
      method: 'POST',
      headers: this.getHeaders(),
      credentials: 'include',
    });
    
    if (!response.ok) {
      throw new Error('Logout failed');
    }
  }

  async checkAuth(): Promise<AuthResponse> {
    const response = await fetch(`${API_BASE_URL}/auth/me`, {
      method: 'GET',
      headers: this.getHeaders(),
      credentials: 'include',
    });
    
    if (!response.ok) {
      throw new Error('Not authenticated');
    }
    
    return response.json();
  }
}

export const api = new ApiClient();

export const auth = {
  logout: async () => {
    try {
      await api.logout();
    } catch {
      return;
    }
  },
  checkAuth: () => api.checkAuth(),
  updateProfile: async (data: UpdateProfileRequest): Promise<AuthResponse> => {
    const response = await fetch(`${API_BASE_URL}/auth/profile`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to update profile' }));
      throw new Error(error.message);
    }
    return response.json();
  },
};

// Execution API
import type { Execution, Task, ExecutionLog, ExecutionOutput, ExecutionMetadata, ExecutionListItem, ArchiveAuditEntry } from '@/components/types';

const getErrorMessage = (status: number, defaultMessage: string): string => {
  switch (status) {
    case 401:
      return 'Authentication required. Please log in again.';
    case 403:
      return 'You do not have permission to access this resource.';
    case 404:
      return 'The requested resource was not found.';
    case 500:
      return 'Server error. Please try again later.';
    case 503:
      return 'Service temporarily unavailable. Please try again later.';
    default:
      return defaultMessage;
  }
};

export const executionApi = {
  getAll: async (includeArchived: boolean = false): Promise<ExecutionListItem[]> => {
    const params = new URLSearchParams({
      simple: 'true',
      includeArchived: includeArchived.toString(),
    });
    const response = await fetch(`${API_BASE_URL}/executions?${params}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch executions') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  getExecution: async (id: string): Promise<Execution> => {
    const response = await fetch(`${API_BASE_URL}/executions/${id}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch execution details') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  archive: async (id: string, reason?: string, retentionDays?: number): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/executions/${id}/archive`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ reason, retentionDays }),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to archive execution') 
      }));
      throw new Error(error.message);
    }
  },

  unarchive: async (id: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/executions/${id}/unarchive`, {
      method: 'POST',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to unarchive execution') 
      }));
      throw new Error(error.message);
    }
  },

  purge: async (id: string, reason: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/executions/${id}`, {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ reason }),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to delete execution') 
      }));
      throw new Error(error.message);
    }
  },

  getArchiveAudit: async (id: string): Promise<ArchiveAuditEntry[]> => {
    const response = await fetch(`${API_BASE_URL}/executions/${id}/archive-audit`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch archive audit') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  getTasks: async (executionId: string): Promise<Task[]> => {
    const response = await fetch(`${API_BASE_URL}/executions/${executionId}/tasks`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch execution tasks') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  getLogs: async (executionId: string): Promise<ExecutionLog[]> => {
    const response = await fetch(`${API_BASE_URL}/executions/${executionId}/logs`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch execution logs') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  getOutputs: async (executionId: string): Promise<ExecutionOutput[]> => {
    const response = await fetch(`${API_BASE_URL}/executions/${executionId}/outputs`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch execution outputs') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  getMetadata: async (executionId: string): Promise<ExecutionMetadata> => {
    const response = await fetch(`${API_BASE_URL}/executions/${executionId}/metadata`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch execution metadata') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  replay: async (executionId: string): Promise<{ executionId: string }> => {
    const response = await fetch(`${API_BASE_URL}/executions/${executionId}/replay`, {
      method: 'POST',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to replay execution') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },
};

// Workflow API
import type { CreateWorkflowRequest, WorkflowResponse, Workflow, WorkflowDetail, ExecuteWorkflowResponse, DuplicateWorkflowResponse } from '@/components/types/workflow';

export const workflowApi = {
  create: async (data: CreateWorkflowRequest): Promise<WorkflowResponse> => {
    const response = await fetch(`${API_BASE_URL}/workflows`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to create workflow') 
      }));
      // If backend returns validation errors, format them nicely
      if (error.errors) {
        const errorMessages = Object.entries(error.errors)
          .map(([field, messages]) => `${field}: ${Array.isArray(messages) ? messages.join(', ') : messages}`)
          .join('\n');
        throw new Error(errorMessages || error.message);
      }
      throw new Error(error.message);
    }
    return response.json();
  },

  getAll: async (): Promise<Workflow[]> => {
    const response = await fetch(`${API_BASE_URL}/workflows`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch workflows') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  getById: async (id: string): Promise<WorkflowDetail> => {
    const response = await fetch(`${API_BASE_URL}/workflows/${id}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch workflow details') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  delete: async (id: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/workflows/${id}`, {
      method: 'DELETE',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to delete workflow') 
      }));
      throw new Error(error.message);
    }
  },

  execute: async (id: string): Promise<ExecuteWorkflowResponse> => {
    const response = await fetch(`${API_BASE_URL}/workflows/${id}/execute`, {
      method: 'POST',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to execute workflow') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  duplicate: async (id: string): Promise<DuplicateWorkflowResponse> => {
    const response = await fetch(`${API_BASE_URL}/workflows/${id}/duplicate`, {
      method: 'POST',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to duplicate workflow') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },
};

// Webhook API
import type { Webhook, CreateWebhookRequest, WebhookResponse, WebhookTestResult, WebhookEventType } from '@/components/types/webhook';

export const webhookApi = {
  create: async (data: CreateWebhookRequest): Promise<WebhookResponse> => {
    const response = await fetch(`${API_BASE_URL}/webhooks`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to register webhook') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  getAll: async (): Promise<Webhook[]> => {
    const response = await fetch(`${API_BASE_URL}/webhooks`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch webhooks') 
      }));
      throw new Error(error.message);
    }

    return response.json();
  },

  getById: async (id: string): Promise<Webhook> => {
    const response = await fetch(`${API_BASE_URL}/webhooks/${id}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch webhook details') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  delete: async (id: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/webhooks/${id}`, {
      method: 'DELETE',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to delete webhook') 
      }));
      throw new Error(error.message);
    }
  },

  update: async (id: string, data: { url: string; events: WebhookEventType[]; secret?: string }): Promise<WebhookResponse> => {
    const response = await fetch(`${API_BASE_URL}/webhooks/${id}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({
        message: getErrorMessage(response.status, 'Failed to update webhook')
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  toggle: async (id: string, isActive: boolean): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/webhooks/${id}/toggle`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ isActive }),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to toggle webhook') 
      }));
      throw new Error(error.message);
    }
  },

  test: async (id: string): Promise<WebhookTestResult> => {
    const response = await fetch(`${API_BASE_URL}/webhooks/${id}/test`, {
      method: 'POST',
      credentials: 'include',
    });
    
    const startTime = Date.now();
    try {
      if (!response.ok) {
        const error = await response.json().catch(() => ({ 
          message: getErrorMessage(response.status, 'Failed to test webhook') 
        }));
        return {
          success: false,
          responseCode: response.status,
          responseTime: Date.now() - startTime,
          error: error.message,
        };
      }
      return {
        success: true,
        responseCode: response.status,
        responseTime: Date.now() - startTime,
      };
    } catch (err) {
      return {
        success: false,
        responseTime: Date.now() - startTime,
        error: err instanceof Error ? err.message : 'Unknown error',
      };
    }
  },
};

// Export & Import API
import type {
  ExportRequest,
  ExportResponse,
  ImportRequest,
  ImportResponse,
  ExecutionExportPayload,
  ExecutionExportItem,
} from '@/components/types/export-import';

export const exportImportApi = {
  export: async (request: ExportRequest): Promise<ExportResponse> => {
    const response = await fetch(`${API_BASE_URL}/executions/export-bulk`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(request),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({
        message: getErrorMessage(response.status, 'Failed to export executions'),
      }));
      throw new Error(error.message);
    }

    const text = await response.text();

    let parsed: unknown;
    try {
      parsed = text ? JSON.parse(text) : [];
    } catch {
      throw new Error('Failed to parse export payload from server');
    }

    const payloads: ExecutionExportPayload[] = Array.isArray(parsed)
      ? (parsed as ExecutionExportPayload[])
      : [parsed as ExecutionExportPayload];

    const exports: ExecutionExportItem[] = payloads.map((payload) => {
      const tasks = payload.tasks;
      const totalTasks = tasks.length;
      const completedTasks = tasks.filter((t) => t.status === 'Succeeded').length;
      const failedTasks = tasks.filter((t) => t.status === 'Failed').length;

      const createdAtUtc = payload.createdAtUtc || new Date().toISOString();

      return {
        executionId: payload.executionId,
        userId: payload.userId,
        status: payload.status,
        createdAtUtc,
        totalTasks,
        completedTasks,
        failedTasks,
        jsonPayload: JSON.stringify(payload),
      };
    });

    return {
      exports,
      totalExported: exports.length,
      exportedAt: new Date().toISOString(),
    };
  },

  import: async (request: ImportRequest): Promise<ImportResponse> => {
    const payload = {
      jsonContent: request.jsonContent,
    };

    const response = await fetch(`${API_BASE_URL}/executions/import`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(payload),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to import executions') 
      }));
      throw new Error(error.message);
    }
    const rawResponse = await response.json();
    const results = Array.isArray(rawResponse?.results) ? rawResponse.results : [];

    return {
      ...rawResponse,
      results: results.map((result: { executionId?: string; importedExecutionId?: string }) => ({
        ...result,
        executionId: result.executionId ?? result.importedExecutionId,
      })),
    };
  },
};

// Cache API
export const cacheApi = {
  clear: async (): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/cache`, {
      method: 'DELETE',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to clear cache') 
      }));
      throw new Error(error.message);
    }
  },
};

// Batch Execution API
import type { BatchExecutionRequest, BatchExecutionResponse } from '@/components/types/batch';

export const batchApi = {
  execute: async (request: BatchExecutionRequest): Promise<BatchExecutionResponse> => {
    const response = await fetch(`${API_BASE_URL}/batch`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(request),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to execute batch') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },
};

// Performance API
import type { PerformanceProfile } from '@/components/types/performance';

export const performanceApi = {
  getProfile: async (executionId: string): Promise<PerformanceProfile> => {
    const response = await fetch(`${API_BASE_URL}/executions/${executionId}/profile`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch performance profile') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },
};

// Template API
import type { ExecutionTemplate, TemplateDetail, CreateTemplateRequest, InstantiateTemplateRequest, InstantiateTemplateResponse } from '@/components/types/template';

export const templateApi = {
  create: async (data: CreateTemplateRequest): Promise<ExecutionTemplate> => {
    const response = await fetch(`${API_BASE_URL}/execution-templates`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to create template') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  getAll: async (): Promise<ExecutionTemplate[]> => {
    const response = await fetch(`${API_BASE_URL}/execution-templates`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch templates') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  getByName: async (name: string): Promise<TemplateDetail> => {
    const response = await fetch(`${API_BASE_URL}/execution-templates/${name}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch template details') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  delete: async (name: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/execution-templates/${name}`, {
      method: 'DELETE',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to delete template') 
      }));
      throw new Error(error.message);
    }
  },

  update: async (name: string, data: CreateTemplateRequest): Promise<ExecutionTemplate> => {
    const response = await fetch(`${API_BASE_URL}/execution-templates/${name}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({
        message: getErrorMessage(response.status, 'Failed to update template')
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  instantiate: async (name: string, data: InstantiateTemplateRequest): Promise<InstantiateTemplateResponse> => {
    const response = await fetch(`${API_BASE_URL}/execution-templates/${name}/instantiate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to instantiate template') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },
};

// Notification API
import type { Notification } from '@/components/types/notification';

export const notificationApi = {
  getAll: async (): Promise<Notification[]> => {
    const response = await fetch(`${API_BASE_URL}/notifications`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch notifications') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  markAsRead: async (id: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/notifications/${id}/read`, {
      method: 'PUT',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to mark as read') 
      }));
      throw new Error(error.message);
    }
  },

  markAllAsRead: async (): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/notifications/read-all`, {
      method: 'PUT',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to mark all as read') 
      }));
      throw new Error(error.message);
    }
  },

  delete: async (id: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/notifications/${id}`, {
      method: 'DELETE',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to delete notification') 
      }));
      throw new Error(error.message);
    }
  },
};

// Settings API
import type { UserPreferences } from '@/components/types/preferences';

export const settingsApi = {
  getPreferences: async (): Promise<UserPreferences> => {
    const response = await fetch(`${API_BASE_URL}/settings/preferences`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch preferences') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  updatePreferences: async (preferences: UserPreferences): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/settings/preferences`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(preferences),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to update preferences') 
      }));
      throw new Error(error.message);
    }
  },
};

// LLM API
import type { LLMConfig, CreateLLMConfigRequest, TestLLMConnectionResponse } from '@/components/types/llm';

export const llmApi = {
  getAll: async (): Promise<LLMConfig[]> => {
    const response = await fetch(`${API_BASE_URL}/llm-configs`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch LLM configs') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  create: async (data: CreateLLMConfigRequest): Promise<LLMConfig> => {
    const response = await fetch(`${API_BASE_URL}/llm-configs`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to create LLM config') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  update: async (id: string, data: CreateLLMConfigRequest): Promise<LLMConfig> => {
    const response = await fetch(`${API_BASE_URL}/llm-configs/${id}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({
        message: getErrorMessage(response.status, 'Failed to update LLM config')
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  delete: async (id: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/llm-configs/${id}`, {
      method: 'DELETE',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to delete LLM config') 
      }));
      throw new Error(error.message);
    }
  },

  test: async (id: string): Promise<TestLLMConnectionResponse> => {
    const response = await fetch(`${API_BASE_URL}/llm-configs/${id}/test`, {
      method: 'POST',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to test LLM connection') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },
};

// Admin API
import type { 
  AdminStats, 
  User, 
  SystemHealthComponent, 
  AdminUserDetail, 
  AdminUserPage, 
  LoginHistoryEntry,
  AdminExecutionPage,
  AdminLLMConfig,
  AdminWebhookPage,
  AdminCacheStats,
  MaintenanceModeStatus,
  SystemConfig,
  AdminAuditEntry
} from '@/components/types/admin';

export const adminApi = {
  // Dashboard stats
  getStats: async (): Promise<AdminStats> => {
    const response = await fetch(`${API_BASE_URL}/admin/stats`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch admin stats') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  getUsers: async (): Promise<User[]> => {
    const response = await fetch(`${API_BASE_URL}/admin/users`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch users') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  getHealth: async (): Promise<SystemHealthComponent[]> => {
    const response = await fetch(`${API_BASE_URL}/health`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch system health') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  // User Management
  searchUsers: async (params: {
    email?: string;
    username?: string;
    role?: string;
    isActive?: boolean;
    includeDeleted?: boolean;
    limit?: number;
    offset?: number;
  }): Promise<AdminUserPage> => {
    const qs = new URLSearchParams(
      Object.entries(params)
        .filter(([, v]) => v !== undefined)
        .map(([k, v]) => [k, String(v)])
    ).toString();
    const response = await fetch(`${API_BASE_URL}/admin/users/search?${qs}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to search users') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  getUserById: async (id: string): Promise<AdminUserDetail> => {
    const response = await fetch(`${API_BASE_URL}/admin/users/${id}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch user details') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  toggleUserStatus: async (id: string, isActive: boolean): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/admin/users/${id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ isActive }),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to toggle user status') 
      }));
      throw new Error(error.message);
    }
  },

  changeUserRole: async (id: string, role: 'Admin' | 'User'): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/admin/users/${id}/role`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ role }),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to change user role') 
      }));
      throw new Error(error.message);
    }
  },

  resetUserPassword: async (id: string, newPassword: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/admin/users/${id}/reset-password`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ newPassword }),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to reset password') 
      }));
      throw new Error(error.message);
    }
  },

  deleteUser: async (id: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/admin/users/${id}`, {
      method: 'DELETE',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to delete user') 
      }));
      throw new Error(error.message);
    }
  },

  getLoginHistory: async (id: string, limit: number = 50): Promise<LoginHistoryEntry[]> => {
    const response = await fetch(`${API_BASE_URL}/admin/users/${id}/login-history?limit=${limit}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch login history') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  // System-wide Executions
  getExecutions: async (params: {
    status?: string;
    from?: string;
    to?: string;
    limit?: number;
    offset?: number;
  }): Promise<AdminExecutionPage> => {
    const qs = new URLSearchParams(
      Object.entries(params)
        .filter(([, v]) => v !== undefined)
        .map(([k, v]) => [k, String(v)])
    ).toString();
    const response = await fetch(`${API_BASE_URL}/admin/executions?${qs}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch executions') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  // LLM Configurations
  getLLMConfigs: async (limit: number = 50, offset: number = 0): Promise<AdminLLMConfig[]> => {
    const response = await fetch(`${API_BASE_URL}/admin/llm-configs?limit=${limit}&offset=${offset}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch LLM configs') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  // Webhooks
  getAllWebhooks: async (limit: number = 50, offset: number = 0): Promise<AdminWebhookPage> => {
    const response = await fetch(`${API_BASE_URL}/admin/webhooks?limit=${limit}&offset=${offset}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch webhooks') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  deactivateUserWebhooks: async (userId: string): Promise<{ deactivatedCount: number }> => {
    const response = await fetch(`${API_BASE_URL}/admin/webhooks/user/${userId}/deactivate`, {
      method: 'PATCH',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to deactivate webhooks') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  // Cache Management
  getCacheStats: async (): Promise<AdminCacheStats> => {
    const response = await fetch(`${API_BASE_URL}/admin/cache/stats`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch cache stats') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  clearCache: async (): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/admin/cache`, {
      method: 'DELETE',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to clear cache') 
      }));
      throw new Error(error.message);
    }
  },

  // Maintenance Mode
  getMaintenanceStatus: async (): Promise<MaintenanceModeStatus> => {
    const response = await fetch(`${API_BASE_URL}/admin/maintenance`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch maintenance status') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  enableMaintenance: async (reason?: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/admin/maintenance/enable`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ reason }),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to enable maintenance mode') 
      }));
      throw new Error(error.message);
    }
  },

  disableMaintenance: async (): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/admin/maintenance/disable`, {
      method: 'DELETE',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to disable maintenance mode') 
      }));
      throw new Error(error.message);
    }
  },

  // System Configuration
  getSystemConfig: async (): Promise<SystemConfig> => {
    const response = await fetch(`${API_BASE_URL}/admin/system`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch system configuration') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  // Admin Audit Log
  getAuditLog: async (params: {
    adminUserId?: string;
    action?: string;
    from?: string;
    to?: string;
    limit?: number;
    offset?: number;
  }): Promise<AdminAuditEntry[]> => {
    const qs = new URLSearchParams(
      Object.entries(params)
        .filter(([, v]) => v !== undefined)
        .map(([k, v]) => [k, String(v)])
    ).toString();
    const response = await fetch(`${API_BASE_URL}/admin/audit-log?${qs}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch audit log') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },
};

// Regression Gates API
import type {
  RegressionGate,
  RegressionTestResult,
  CreateRegressionGateRequest,
  ListRegressionGatesResponse,
  ToggleGateActiveRequest,
  ToggleGateActiveResponse,
  TestGateRequest,
  MarkGoldenRequest,
  MarkGoldenResponse,
  ListGoldenExecutionsResponse,
} from '@/components/types/regression-gates';

export const regressionGatesApi = {
  list: async (workflowId?: string): Promise<ListRegressionGatesResponse> => {
    const params = workflowId ? `?workflowId=${workflowId}` : '';
    const response = await fetch(`${API_BASE_URL}/regression-gates${params}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch regression gates') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  get: async (id: string): Promise<RegressionGate> => {
    const response = await fetch(`${API_BASE_URL}/regression-gates/${id}`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch gate') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  create: async (data: CreateRegressionGateRequest): Promise<RegressionGate> => {
    const response = await fetch(`${API_BASE_URL}/regression-gates`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to create gate') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  delete: async (id: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/regression-gates/${id}`, {
      method: 'DELETE',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to delete gate') 
      }));
      throw new Error(error.message);
    }
  },

  toggle: async (id: string, data: ToggleGateActiveRequest): Promise<ToggleGateActiveResponse> => {
    const response = await fetch(`${API_BASE_URL}/regression-gates/${id}/toggle`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to toggle gate status') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  test: async (id: string, data: TestGateRequest): Promise<RegressionTestResult> => {
    const response = await fetch(`${API_BASE_URL}/regression-gates/${id}/test`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to test gate') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },
};

export const goldenExecutionsApi = {
  list: async (): Promise<ListGoldenExecutionsResponse> => {
    const response = await fetch(`${API_BASE_URL}/executions/golden`, {
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to fetch golden executions') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  mark: async (executionId: string, data: MarkGoldenRequest): Promise<MarkGoldenResponse> => {
    const response = await fetch(`${API_BASE_URL}/executions/${executionId}/mark-golden`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to mark as golden') 
      }));
      throw new Error(error.message);
    }
    return response.json();
  },

  unmark: async (executionId: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/executions/${executionId}/mark-golden`, {
      method: 'DELETE',
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ 
        message: getErrorMessage(response.status, 'Failed to unmark golden') 
      }));
      throw new Error(error.message);
    }
  },
};