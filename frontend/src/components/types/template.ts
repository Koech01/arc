export interface TemplateTask {
  id: string;
  name: string;
  agentType: 'llm' | 'http' | 'python' | 'sql' | 'email';
  prompt?: string;
  llmConfigId?: string;
  config?: Record<string, string>;
  dependencies?: string[];
}

export interface ExecutionTemplate {
  name: string;
  description: string;
  createdAtUtc: string;
  useCount: number;
  llmConfigId?: string;
}

export interface TemplateDetail extends ExecutionTemplate {
  triggerType: 'manual' | 'scheduled' | 'webhook';
  llmConfigId?: string;
  tasks: TemplateTask[];
}

export interface CreateTemplateRequest {
  name: string;
  description?: string;
  triggerType: 'manual' | 'scheduled' | 'webhook';
  llmConfigId?: string;
  tasks: TemplateTask[];
}

export interface InstantiateTemplateRequest {
  variables?: Record<string, string>;
  llmConfigId?: string;
}

export interface InstantiateTemplateResponse {
  workflowId: string;
  workflowName: string;
}