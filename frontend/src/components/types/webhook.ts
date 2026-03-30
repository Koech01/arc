export type WebhookEventType = 'execution.started' | 'execution.completed' | 'execution.failed';

export interface Webhook {
  id: string;
  url: string;
  events: WebhookEventType[];
  isActive: boolean;
  createdAt: string;
}

export interface CreateWebhookRequest {
  url: string;
  events: WebhookEventType[];
  secret: string;
}

export interface WebhookResponse {
  id: string;
  url: string;
  events: WebhookEventType[];
  isActive: boolean;
  createdAt: string;
}

export interface WebhookListResponse {
  webhooks: Webhook[];
}

export interface WebhookTestPayload {
  executionId: string;
  eventType: WebhookEventType;
  timestamp: string;
  taskCount: number;
  status: 'running' | 'success' | 'failed';
  durationMs: number;
  errorMessage: string | null;
}

export interface WebhookTestResult {
  success: boolean;
  responseCode?: number;
  responseTime?: number;
  error?: string;
}

export const WEBHOOK_EVENTS = [
  {
    id: 'execution.started',
    label: 'Execution Started',
    description: 'Notified when an execution begins',
  },
  {
    id: 'execution.completed',
    label: 'Execution Completed',
    description: 'Notified when an execution finishes successfully',
  },
  {
    id: 'execution.failed',
    label: 'Execution Failed',
    description: 'Notified when an execution encounters an error',
  },
] as const;

export const EVENT_COLORS: Record<WebhookEventType, string> = {
  'execution.started': 'bg-blue-100 text-blue-800',
  'execution.completed': 'bg-green-100 text-green-800',
  'execution.failed': 'bg-red-100 text-red-800',
};