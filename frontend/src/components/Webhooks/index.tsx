import { toast } from 'sonner';
import { webhookApi } from '@/lib/api';
import { WebhookCard } from './WebhookCard';
import { WebhookForm } from './WebhookForm';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Plus, WebhookIcon } from 'lucide-react';
import { useState, useEffect, useMemo } from 'react';
import { TestWebhookModal } from './TestWebhookModal';
import { ScrollArea } from "@/components/ui/scroll-area";
import { WebhooksListSkeleton } from './WebhooksListSkeleton';
import type { Webhook, CreateWebhookRequest } from '@/components/types/webhook';
import { DeleteConfirmationDialog } from '@/components/ui/delete-confirmation-dialog';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty';


export function WebhooksListPage() {
  const [webhooks, setWebhooks] = useState<Webhook[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');

  const [formModalOpen, setFormModalOpen] = useState(false);
  const [formLoading, setFormLoading] = useState(false);

  const [testingId, setTestingId] = useState<string | undefined>();
  const [testWebhook, setTestWebhook] = useState<Webhook | null>(null);
  const [testModalOpen, setTestModalOpen] = useState(false);

  const [deleteWebhook, setDeleteWebhook] = useState<Webhook | null>(null);
  const [deleteModalOpen, setDeleteModalOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const [togglingId, setTogglingId] = useState<string | undefined>();

  const [editWebhook, setEditWebhook] = useState<Webhook | null>(null);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [editLoading, setEditLoading] = useState(false);

  // Fetch webhooks on mount
  useEffect(() => {
    fetchWebhooks();
  }, []);

  const fetchWebhooks = async () => {
    setLoading(true);
    try {
      const data = await webhookApi.getAll();
      setWebhooks(data || []);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to fetch webhooks', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  const handleRegisterWebhook = async (data: CreateWebhookRequest) => {
    setFormLoading(true);
    try {
      const newWebhook = await webhookApi.create(data);
      setWebhooks((prev) => [...prev, newWebhook]);
      setFormModalOpen(false);
      toast.success('Webhook registered successfully', { position: 'top-center' });
    } finally {
      setFormLoading(false);
    }
  };

  const handleTestWebhook = async (id: string) => {
    setTestingId(id);
    const webhook = webhooks.find((w) => w.id === id);
    if (webhook) {
      setTestWebhook(webhook);
      setTestModalOpen(true);
    }
    setTestingId(undefined);
  };

  const filteredWebhooks = useMemo(() => {
    if (!searchQuery) return webhooks;
    const query = searchQuery.toLowerCase();
    return webhooks.filter(webhook => 
      webhook.url.toLowerCase().includes(query) ||
      webhook.events.some(event => event.toLowerCase().includes(query))
    );
  }, [webhooks, searchQuery]);

  const handleDeleteWebhook = (id: string) => {
    const webhook = webhooks.find((w) => w.id === id);
    if (webhook) {
      setDeleteWebhook(webhook);
      setDeleteModalOpen(true);
    }
  };

  const handleToggleWebhook = async (id: string, isActive: boolean) => {
    setTogglingId(id);
    try {
      await webhookApi.toggle(id, isActive);
      setWebhooks((prev) => prev.map((w) => w.id === id ? { ...w, isActive } : w));
      toast.success(`Webhook ${isActive ? 'enabled' : 'disabled'} successfully`, { position: 'top-center' });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to toggle webhook', { position: 'top-center' });
    } finally {
      setTogglingId(undefined);
    }
  };

  const handleEditWebhook = (webhook: Webhook) => {
    setEditWebhook(webhook);
    setEditModalOpen(true);
  };

  const handleUpdateWebhook = async (data: { url: string; events: Parameters<typeof webhookApi.update>[1]['events']; secret: string }) => {
    if (!editWebhook) return;
    setEditLoading(true);
    try {
      const payload = { url: data.url, events: data.events, ...(data.secret ? { secret: data.secret } : {}) };
      const updated = await webhookApi.update(editWebhook.id, payload);
      setWebhooks((prev) => prev.map((w) => w.id === editWebhook.id ? { ...w, ...updated } : w));
      setEditModalOpen(false);
      setEditWebhook(null);
      toast.success('Webhook updated successfully', { position: 'top-center' });
    } finally {
      setEditLoading(false);
    }
  };

  const handleConfirmDelete = async () => {
    if (!deleteWebhook) return;
    setIsDeleting(true);
    try {
      await webhookApi.delete(deleteWebhook.id);
      setWebhooks((prev) => prev.filter((w) => w.id !== deleteWebhook.id));
      setDeleteModalOpen(false);
      setDeleteWebhook(null);
      toast.success('Webhook deleted successfully', { position: 'top-center' });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete webhook', { position: 'top-center' });
    } finally {
      setIsDeleting(false);
    }
  };
 

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="container mx-auto px-4 py-8">
        {webhooks.length === 0 && !loading ? (
          <Empty>
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <WebhookIcon />
              </EmptyMedia>
              <EmptyTitle>Webhooks</EmptyTitle>
              <EmptyDescription>
                Configure webhook endpoints to receive execution notifications.
              </EmptyDescription>
            </EmptyHeader>
            <EmptyContent>
              <Button onClick={() => setFormModalOpen(true)}>
                <Plus className="h-4 w-4" />Register Webhook
              </Button>
            </EmptyContent>
          </Empty>
        ) : null}

        {/* Content */}
        {loading ? (
          <WebhooksListSkeleton />
        ) : webhooks.length > 0 ? (
          <div className="space-y-6">
            <div className="flex justify-between items-center gap-4">
              <div>
                <h1 className="text-2xl font-bold">Webhooks</h1>
                <p className="text-sm text-muted-foreground mt-1">
                  {webhooks.filter(w => w.isActive).length} active, {webhooks.filter(w => !w.isActive).length} inactive
                </p>
              </div>
              <Button onClick={() => setFormModalOpen(true)}>
                <Plus className="h-4 w-4" />Register Webhook
              </Button>
            </div>

            <div className="flex-1 md:w-3/4">
              <Input
                id="search-webhook"
                placeholder="Search by URL or Event"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                aria-label="Search webhooks"
              />
            </div>

            {filteredWebhooks.length === 0 ? (
              <div className="text-center py-12 text-muted-foreground">
                No webhooks found matching "{searchQuery}"
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {filteredWebhooks.map((webhook) => (
                  <WebhookCard
                    key={webhook.id}
                    webhook={webhook}
                    onTest={handleTestWebhook}
                    onDelete={handleDeleteWebhook}
                    onToggle={handleToggleWebhook}
                    onEdit={handleEditWebhook}
                    testingId={testingId}
                    deletingId={isDeleting && deleteWebhook?.id === webhook.id ? webhook.id : undefined}
                    togglingId={togglingId}
                  />
                ))}
              </div>
            )}
          </div>
        ) : null}

        {/* Registration Modal */}
        <Dialog open={formModalOpen} onOpenChange={setFormModalOpen}>
          <DialogContent className="p-3 w-[90vw] max-w-[90vw] mx-auto rounded-lg md:w-auto md:max-w-lg">
            <DialogHeader className="text-left">
              <DialogTitle>Register New Webhook</DialogTitle>
              <DialogDescription>
                Configure a webhook endpoint to receive execution notifications
              </DialogDescription>
            </DialogHeader>
            <WebhookForm
              onSubmit={handleRegisterWebhook}
              isLoading={formLoading}
              onCancel={() => setFormModalOpen(false)}
            />
          </DialogContent>
        </Dialog>

        {/* Edit Webhook Modal */}
        <Dialog open={editModalOpen} onOpenChange={(open) => { if (!editLoading) { setEditModalOpen(open); if (!open) setEditWebhook(null); } }}>
          <DialogContent className="p-3 w-[90vw] max-w-[90vw] mx-auto rounded-lg md:w-auto md:max-w-lg">
            <DialogHeader className="text-left">
              <DialogTitle>Edit Webhook</DialogTitle>
              <DialogDescription>
                Update the webhook URL, events, or rotate the signing secret
              </DialogDescription>
            </DialogHeader>
            {editWebhook && (
              <WebhookForm
                onSubmit={handleUpdateWebhook}
                isLoading={editLoading}
                onCancel={() => { setEditModalOpen(false); setEditWebhook(null); }}
                initialValues={{ url: editWebhook.url, events: editWebhook.events }}
                mode="edit"
              />
            )}
          </DialogContent>
        </Dialog>

        {/* Test Webhook Modal */}
        <TestWebhookModal
          webhook={testWebhook}
          open={testModalOpen}
          onOpenChange={setTestModalOpen}
        />

        {/* Delete Webhook Modal */}
        <DeleteConfirmationDialog
          open={deleteModalOpen}
          onOpenChange={(open) => !isDeleting && setDeleteModalOpen(open)}
          onConfirm={handleConfirmDelete}
          title={`Delete webhook "${deleteWebhook?.url.replace(/^https?:\/\//, '')}"?`}
          description="This action cannot be undone. The webhook will no longer receive execution notifications."
          isLoading={isDeleting}
        />
      </div>
    </ScrollArea>
  );
}

export default WebhooksListPage;