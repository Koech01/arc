export interface LLMConfig {
  id: string;
  name: string;
  baseUrl: string;
  model: string;
  apiKey?: string;
  endpoint: string;
  authType: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateLLMConfigRequest {
  name: string;
  baseUrl: string;
  model: string;
  apiKey?: string;
  endpoint?: string;
  authType?: string;
  headers?: Record<string, string>;
}

export interface TestLLMConnectionResponse {
  success: boolean;
  responseTimeMs: number;
  message: string;
}