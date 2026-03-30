export interface UserPreferences {
  theme: 'light' | 'dark' | 'system';
  notifications: {
    email: boolean;
    push: boolean;
    executionComplete: boolean;
    executionFailed: boolean;
  };
  language: string;
  timezone: string;
}
