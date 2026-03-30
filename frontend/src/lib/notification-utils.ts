export function extractExecutionId(message: string): string | null {
  const match = message.match(/in execution ([a-f0-9]{64})/);
  return match ? match[1] : null;
}

export function isTaskNotification(title: string): boolean {
  return title === 'Task Completed';
}

export function isWebhookFailureNotification(title: string): boolean {
  return title === 'Webhook Delivery Failed';
}