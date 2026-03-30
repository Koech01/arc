export type DivergenceRuleType = 
  | 'SimilarityPercentage'
  | 'MaxTaskDivergence'
  | 'CriticalPathPreservation'
  | 'NoStatusDegradation';

export interface DivergenceRule {
  type: DivergenceRuleType;
  threshold: number;
}

export interface RegressionGate {
  id: string;
  name: string;
  description: string | null;
  goldenExecutionId: string;
  workflowId: string | null;
  rules: DivergenceRule[];
  isActive: boolean;
  ownerId: string;
  createdAt: string;
}

export interface GoldenExecution {
  executionId: string;
  markedAt: string;
  label: string | null;
  ownerId: string;
}

export interface DivergenceSummary {
  similarityPercentage: number;
  identicalTaskCount: number;
  differentTaskCount: number;
  firstDivergenceIndex: number | null;
  criticalPathTaskIds: string[];
}

export interface RuleEvaluationResult {
  ruleType: DivergenceRuleType;
  threshold: number;
  actualValue: number;
  passed: boolean;
  message: string;
}

export interface RegressionTestResult {
  gateId: string;
  gateName: string;
  goldenExecutionId: string;
  candidateExecutionId: string;
  passed: boolean;
  testedAt: string;
  divergenceSummary: DivergenceSummary;
  ruleResults: RuleEvaluationResult[];
}

export interface CreateRegressionGateRequest {
  name: string;
  description?: string;
  goldenExecutionId: string;
  workflowId?: string;
  rules: DivergenceRule[];
}

export interface ListRegressionGatesResponse {
  gates: RegressionGate[];
}

export interface ToggleGateActiveRequest {
  isActive: boolean;
}

export interface ToggleGateActiveResponse {
  id: string;
  isActive: boolean;
}

export interface TestGateRequest {
  candidateExecutionId: string;
}

export interface MarkGoldenRequest {
  label?: string;
}

export interface MarkGoldenResponse {
  executionId: string;
  isGolden: true;
  markedAt: string;
  label: string | null;
}

export interface ListGoldenExecutionsResponse {
  goldenExecutions: GoldenExecution[];
}