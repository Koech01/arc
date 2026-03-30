export interface PerformanceProfile {
  executionId: string;
  totalDuration: number;
  criticalPath: string[];
  criticalPathDuration: number;
  maxConcurrentTasks: number;
  parallelizationEfficiency: number;
  taskMetrics: TaskMetric[];
}

export interface TaskMetric {
  taskId: string;
  taskName: string;
  executionTime: number;
  waitTime: number;
  startTime: string;
  endTime: string;
  isOnCriticalPath: boolean;
}
