import { AlertTriangle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import type { Webhook } from '@/components/types/webhook';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';


interface DeleteWebhookModalProps {
  webhook: Webhook | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: () => Promise<void>;
  isLoading: boolean;
}

export function DeleteWebhookModal({
  webhook,
  open,
  onOpenChange,
  onConfirm,
  isLoading,
}: DeleteWebhookModalProps) {
  if (!webhook) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="p-3 max-w-md mx-4">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <AlertTriangle className="h-6 w-6 text-red-600 flex-shrink-0" />
            <div>
              <DialogTitle>Delete Webhook?</DialogTitle>
              <DialogDescription className="text-sm">
                This action cannot be undone. The webhook will no longer receive
                execution notifications.
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <div className="space-y-3 py-4">
          <div className="bg-gray-50 p-3 rounded border">
            <p className="text-sm font-medium text-gray-600">URL</p>
            <p className="text-sm text-gray-900 break-all">{webhook.url}</p>
          </div>

          <div className="bg-gray-50 p-3 rounded border">
            <p className="text-sm font-medium text-gray-600">Events</p>
            <p className="text-sm text-gray-900">{webhook.events.join(', ')}</p>
          </div>
        </div>

        <DialogFooter className="flex justify-end gap-3 md:gap-0">
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isLoading}
          >
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={onConfirm}
            disabled={isLoading}
            aria-busy={isLoading}
          >
            {isLoading ? 'Deleting...' : 'Delete Webhook'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}