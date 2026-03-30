export interface BatchExecutionPlan {
  tasks: Array<{
    id: string;
    name: string;
    agentType: string;
    config: Record<string, string>;
    dependencies: string[];
  }>;
}

export interface BatchExecutionRequest {
  plans: BatchExecutionPlan[];
}

export interface BatchExecutionResult {
  success: boolean;
  executionId?: string;
  error?: string;
}

export interface BatchExecutionResponse {
  results: BatchExecutionResult[];
}
