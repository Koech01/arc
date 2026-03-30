import { toast } from 'sonner';
import { adminApi } from '@/lib/api';
import { useState, useEffect } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ScrollArea } from "@/components/ui/scroll-area";
import type { AdminWebhook } from '@/components/types/admin';
import { WebhooksAdminSkeleton } from './WebhooksAdminSkeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';


export default function WebhooksAdmin() {
  const [webhooks, setWebhooks] = useState<AdminWebhook[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchWebhooks();
  }, []);

  const fetchWebhooks = async () => {
    setLoading(true);
    try {
      const data = await adminApi.getAllWebhooks(100, 0);
      setWebhooks(data.webhooks);
      setTotalCount(data.totalCount);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load webhooks', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  const handleDeactivateUserWebhooks = async (userId: string) => {
    try {
      const result = await adminApi.deactivateUserWebhooks(userId);
      toast.success(`Deactivated ${result.deactivatedCount} webhook(s)`);
      fetchWebhooks();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to deactivate webhooks');
    }
  };

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <WebhooksAdminSkeleton />
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-6 py-8 px-4 lg:px-6">
        <div>
          <h1 className="text-2xl font-bold">Webhooks Management</h1>
          <p className="text-muted-foreground">View and manage all webhooks across all users</p>
        </div>

        {/* Webhooks Table */}
        <div className="overflow-hidden rounded-lg border">
          <Table>
            <TableHeader className="bg-muted">
              <TableRow>
                <TableHead>URL</TableHead>
                <TableHead>Events</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Owner</TableHead>
                <TableHead>Created</TableHead>
                <TableHead>Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {webhooks.length > 0 ? (
                webhooks.map((webhook) => (
                  <TableRow key={webhook.id}>
                    <TableCell><span className="text-sm truncate max-w-xs">{webhook.url}</span></TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        {webhook.events.slice(0, 2).map((event) => (
                          <Badge key={event} variant="outline" className="text-xs">
                            {event}
                          </Badge>
                        ))}
                        {webhook.events.length > 2 && (
                          <Badge variant="outline" className="text-xs">
                            +{webhook.events.length - 2}
                          </Badge>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant={webhook.isActive ? 'default' : 'secondary'}>
                        {webhook.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                    </TableCell>
                    <TableCell><span className="text-sm">{webhook.createdBy.slice(0, 8)}...</span></TableCell>
                    <TableCell><span className="text-sm">{new Date(webhook.createdAt).toLocaleDateString()}</span></TableCell>
                    <TableCell>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => handleDeactivateUserWebhooks(webhook.createdBy)}
                        disabled={!webhook.isActive}
                      >
                        Deactivate
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center">
                    No webhooks found.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>

        {/* Summary */}
        <div className="border rounded-lg p-4">
          <div className="flex justify-between text-sm">
            <span className="text-muted-foreground">Total Webhooks:</span>
            <span className="font-medium">{totalCount}</span>
          </div>
        </div>
      </div>
    </ScrollArea>
  );
}