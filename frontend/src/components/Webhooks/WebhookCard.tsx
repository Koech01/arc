import { useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Card, CardFooter, CardHeader } from '@/components/ui/card';
import type { Webhook, WebhookEventType } from '@/components/types/webhook';
import { Copy, Trash2, Zap, Pencil, CheckCircle2, PauseCircle } from 'lucide-react';


interface WebhookCardProps {
  webhook: Webhook;
  onTest: (id: string) => void;
  onDelete: (id: string) => void;
  onToggle: (id: string, isActive: boolean) => void;
  onEdit: (webhook: Webhook) => void;
  testingId?: string;
  deletingId?: string;
  togglingId?: string;
}

export function WebhookCard({
  webhook,
  onTest,
  onDelete,
  onToggle,
  onEdit,
  testingId,
  deletingId,
  togglingId,
}: WebhookCardProps) {
  const [copied, setCopied] = useState(false);

  const handleCopyUrl = () => {
    navigator.clipboard.writeText(webhook.url);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const getEventColor = (event: WebhookEventType): string => {
    if (event === 'execution.started') return 'text-amber-500';
    if (event === 'execution.completed') return 'text-green-500';
    return 'text-red-500';
  };

  return (
    <Card className="flex flex-col bg-card/90 shadow-sm">
      <CardHeader className="space-y-4 p-4">
        <div className="flex items-center justify-between gap-2">
          <Badge variant={webhook.isActive ? 'default' : 'outline'} className="flex items-center gap-1">
            {webhook.isActive ? <CheckCircle2 className="h-3.5 w-3.5" /> : <PauseCircle className="h-3.5 w-3.5" />}
            <span>{webhook.isActive ? 'Active' : 'Inactive'}</span>
          </Badge>
        </div>
        <div className="flex flex-col gap-2 text-sm">
          <div className="flex">
            <span className="w-16 text-muted-foreground">URL</span>
            <div className="flex items-center gap-2 flex-1">
              <a
                href={webhook.url}
                target="_blank"
                rel="noopener noreferrer"
                className="break-all"
              >
                {webhook.url.replace(/^https?:\/\//, '')}
              </a>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                onClick={handleCopyUrl}
                className="h-7 w-7"
                aria-label="Copy webhook URL"
              >
                <Copy className="h-3.5 w-3.5" />
              </Button>
            </div>
          </div>
          {copied && (
            <p className="text-xs text-green-600 ml-16" role="status" aria-live="polite">
              Copied!
            </p>
          )}

          <div className="flex">
            <span className="w-16 text-muted-foreground">Events</span>
            <div className="flex gap-2 flex-wrap">
              {webhook.events.map((event) => (
                <Badge key={event} variant="outline" className={getEventColor(event)}>
                  {event.split('.').pop()?.replace(/_/g, ' ')}
                </Badge>
              ))}
            </div>
          </div>

          <div className="flex">
            <span className="w-16 text-muted-foreground">Created</span>
            <span className="text-muted-foreground">
              {formatDate(webhook.createdAt)}
            </span>
          </div>
        </div>
      </CardHeader>

      <CardFooter className="mt-auto flex items-center justify-between gap-2 border-t px-4 py-3">
        <div className="flex items-center gap-2">
          <Switch
            id={`webhook-${webhook.id}-toggle`}
            checked={webhook.isActive}
            onCheckedChange={(checked) => onToggle(webhook.id, checked)}
            disabled={togglingId === webhook.id}
            aria-label={`${webhook.isActive ? 'Disable' : 'Enable'} webhook`}
          />
          <label htmlFor={`webhook-${webhook.id}-toggle`} className="text-sm text-muted-foreground cursor-pointer">
            Enabled
          </label>
        </div>
        <div className="flex gap-2">
          <Button
            size="sm"
            variant="outline"
            onClick={() => onEdit(webhook)}
            aria-label="Edit webhook"
          >
            <Pencil className="h-4 w-4" />
            Edit
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={() => onTest(webhook.id)}
            disabled={testingId === webhook.id || !webhook.isActive}
            aria-busy={testingId === webhook.id}
            aria-label={`Test webhook`}
          >
            <Zap className="h-4 w-4" />
            {testingId === webhook.id ? 'Testing...' : 'Test'}
          </Button>
          <Button
            size="sm"
            variant="destructive"
            onClick={() => onDelete(webhook.id)}
            disabled={deletingId === webhook.id}
            aria-busy={deletingId === webhook.id}
            aria-label={`Delete webhook`}
          >
            <Trash2 className="h-4 w-4" />
            {deletingId === webhook.id ? 'Deleting...' : 'Delete'}
          </Button>
        </div>
      </CardFooter>
    </Card>
  );
}