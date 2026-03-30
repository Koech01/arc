export interface WorkflowTask {
  id: string;
  name: string;
  agentType: string;
  prompt?: string;
  config: Record<string, string>;
  dependencies: string[];
}

export interface CreateWorkflowRequest {
  name: string;
  description: string;
  tasks: WorkflowTask[];
  triggerType: 'manual' | 'scheduled' | 'webhook';
  llmConfigId?: string;
}

export interface WorkflowResponse {
  id: string;
  name: string;
  description: string;
  createdAt: string;
}

export interface Workflow {
  id: string;
  name: string;
  description: string;
  triggerType: 'manual' | 'scheduled' | 'webhook';
  createdAt: string;
}

export interface WorkflowDetail extends Workflow {
  tasks: WorkflowTask[];
}

export interface ExecuteWorkflowResponse {
  executionId: string;
  workflowId: string;
  workflowName: string;
  tasks: ExecutedTask[];
}

export interface ExecutedTask {
  taskId: string;
  taskName: string;
  executionOrder: number;
  status: 'Succeeded' | 'Failed' | 'Running' | 'Pending';
  output?: string;
}

export interface DuplicateWorkflowResponse {
  id: string;
  name: string;
  description: string;
  createdAt: string;
}