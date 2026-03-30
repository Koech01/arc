import { webhookApi } from '@/lib/api';
import { useState, useEffect } from 'react';
import { CheckCircle2 } from 'lucide-react';
import { AlertCircleIcon } from "lucide-react";
import { Button } from '@/components/ui/button';
import type { Webhook } from '@/components/types/webhook';
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';


interface TestWebhookModalProps {
  webhook: Webhook | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function TestWebhookModal({
  webhook,
  open,
  onOpenChange,
}: TestWebhookModalProps) {
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<{
    success: boolean;
    responseCode?: number;
    responseTime?: number;
    error?: string;
  } | null>(null);

  useEffect(() => {
    if (!open) {
      setResult(null);
    }
  }, [open]);

  const handleTest = async () => {
    if (!webhook) return;
    setLoading(true);
    try {
      const testResult = await webhookApi.test(webhook.id);
      setResult(testResult);
    } catch (error) {
      setResult({
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error',
      });
    } finally {
      setLoading(false);
    }
  };

  const testPayload = {
    executionId: 'test-execution-uuid',
    eventType: 'execution.started',
    timestamp: new Date().toISOString(),
    taskCount: 0,
    status: 'running' as const,
    durationMs: 0,
    errorMessage: null,
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="p-3 w-[90vw] max-w-[90vw] rounded-2xl md:w-auto md:max-w-3xl md: max-h-[80vh] overflow-y-auto">
        <DialogHeader className="text-left">
          <DialogTitle>Test Webhook</DialogTitle>
          <DialogDescription>
            Send a test payload to verify webhook configuration
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {!result ? (
            <>
              {/* Payload Preview */}
              <div className="space-y-2">
                <p className="text-sm font-medium">Test Payload</p>
                <div className="bg-gray-50 p-3 rounded text-xs overflow-auto max-h-48 border">
                  <pre>{JSON.stringify(testPayload, null, 2)}</pre>
                </div>
              </div>

              {/* Headers */}
              <div className="space-y-2">
                <p className="text-sm font-medium">Request Headers</p>
                <div className="bg-gray-50 p-3 rounded text-xs space-y-1 border">
                  <div>X-Webhook-Event: execution.started</div>
                  <div>X-Webhook-Signature: sha256=[signature]</div>
                  <div>X-Webhook-Timestamp: {new Date().toISOString()}</div>
                </div>
              </div>
            </>
          ) : result.success ? (
            /* Success State */
            <div className="space-y-3">
              <div className="flex items-start gap-3 p-3 bg-green-50 rounded border border-green-200">
                <CheckCircle2 className="h-5 w-5 text-green-600 flex-shrink-0 mt-0.5" />
                <div>
                  <p className="font-medium text-green-900">
                    Webhook test successful
                  </p>
                  <div className="text-sm text-green-700 mt-1 space-y-0.5">
                    <p>Response Code: {result.responseCode}</p>
                    <p>Response Time: {result.responseTime}ms</p>
                  </div>
                </div>
              </div>
            </div>
          ) : (
            /* Error State */
            <Alert variant="destructive" className="w-fit border-gray-200">
              <div className="flex items-center gap-2">
                <AlertCircleIcon className="h-4 w-4 flex-shrink-0" />
                <AlertDescription>
                    <p>Error: {result.error}</p>
                    {result.responseCode && (
                      <p>Response Code: {result.responseCode}</p>
                    )}
                    {result.responseTime && (
                      <p>Response Time: {result.responseTime}ms</p>
                    )}
                </AlertDescription>
              </div>
            </Alert>
          )}
        </div>

        <DialogFooter className="flex gap-4 md:flex-row md:gap-0">
          {result ? (
            <>
              <Button
                variant="outline"
                onClick={() => setResult(null)}
                disabled={loading}
              >
                Close
              </Button>
              <Button
                onClick={handleTest}
                disabled={loading}
                aria-busy={loading}
              >
                {loading ? 'Testing...' : 'Retry'}
              </Button>
            </>
          ) : (
            <>
              <Button
                variant="outline"
                onClick={() => onOpenChange(false)}
                disabled={loading}
              >
                Cancel
              </Button>
              <Button
                onClick={handleTest}
                disabled={loading}
                aria-busy={loading}
              >
                {loading ? 'Sending...' : 'Send Test Payload'}
              </Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}