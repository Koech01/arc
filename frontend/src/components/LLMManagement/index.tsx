import { toast } from 'sonner';
import { llmApi } from '@/lib/api';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useState, useEffect, useMemo } from 'react';
import { ScrollArea } from "@/components/ui/scroll-area";
import { LLMManagementSkeleton } from './LLMManagementSkeleton';
import { Card, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import type { LLMConfig, CreateLLMConfigRequest } from '@/components/types/llm';
import { DeleteConfirmationDialog } from '@/components/ui/delete-confirmation-dialog';
import { Plus, PlusIcon, CpuIcon, Pencil, CheckCircle2, PauseCircle } from 'lucide-react';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogDescription } from '@/components/ui/dialog';


export default function LLMManagementPage() {
  const [llms, setLlms] = useState<LLMConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const [showDialog, setShowDialog] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [deleteDialog, setDeleteDialog] = useState<{ open: boolean; llm: LLMConfig | null; isDeleting: boolean }>({
    open: false,
    llm: null,
    isDeleting: false,
  });

  const [form, setForm] = useState<CreateLLMConfigRequest>({
    name: '',
    baseUrl: '',
    model: '',
    apiKey: '',
    endpoint: 'chat/completions',
    authType: 'bearer',
  });

  const [editDialog, setEditDialog] = useState<{ open: boolean; llm: LLMConfig | null; isLoading: boolean }>({
    open: false,
    llm: null,
    isLoading: false,
  });
  const [editForm, setEditForm] = useState<CreateLLMConfigRequest>({
    name: '',
    baseUrl: '',
    model: '',
    apiKey: '',
    endpoint: 'chat/completions',
    authType: 'bearer',
  });

  useEffect(() => {
    fetchLLMs();
  }, []);

  const fetchLLMs = async () => {
    setLoading(true);
    try {
      const data = await llmApi.getAll();
      setLlms(data);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load LLMs', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const newLLM = await llmApi.create(form);
      setLlms([...llms, newLLM]);
      setShowDialog(false);
      setForm({ name: '', baseUrl: '', model: '', apiKey: '', endpoint: 'chat/completions', authType: 'bearer' });
      toast.success('LLM created successfully', { position: 'top-center' });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to create LLM', { position: 'top-center' });
    }
  };

  const handleEditClick = (llm: LLMConfig) => {
    setEditForm({ name: llm.name, baseUrl: llm.baseUrl, model: llm.model, apiKey: '', endpoint: llm.endpoint, authType: llm.authType });
    setEditDialog({ open: true, llm, isLoading: false });
  };

  const handleEditSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editDialog.llm) return;
    setEditDialog(prev => ({ ...prev, isLoading: true }));
    try {
      const payload = { ...editForm, ...(editForm.apiKey ? {} : { apiKey: undefined }) };
      const updated = await llmApi.update(editDialog.llm.id, payload);
      setLlms(llms.map(l => l.id === editDialog.llm!.id ? updated : l));
      toast.success('LLM updated successfully', { position: 'top-center' });
      setEditDialog({ open: false, llm: null, isLoading: false });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update LLM', { position: 'top-center' });
      setEditDialog(prev => ({ ...prev, isLoading: false }));
    }
  };

  const handleDeleteClick = (llm: LLMConfig) => {
    setDeleteDialog({ open: true, llm, isDeleting: false });
  };

  const handleDeleteConfirm = async () => {
    if (!deleteDialog.llm) return;
    setDeleteDialog(prev => ({ ...prev, isDeleting: true }));
    try {
      await llmApi.delete(deleteDialog.llm.id);
      setLlms(llms.filter(l => l.id !== deleteDialog.llm!.id));
      toast.success('LLM deleted successfully', { position: 'top-center' });
      setDeleteDialog({ open: false, llm: null, isDeleting: false });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete LLM', { position: 'top-center' });
      setDeleteDialog(prev => ({ ...prev, isDeleting: false }));
    }
  };

  const handleTest = async (id: string) => {
    try {
      const result = await llmApi.test(id);
      if (result.success) {
        toast.success(`Connected successfully (${result.responseTimeMs}ms)`, { position: 'top-center' });
      } else {
        toast.error(result.message, { position: 'top-center' });
      }
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Test failed', { position: 'top-center' });
    }
  };

  const filteredLlms = useMemo(() => {
    if (!searchQuery) return llms;
    const query = searchQuery.toLowerCase();
    return llms.filter(llm => 
      llm.name.toLowerCase().includes(query) ||
      llm.model.toLowerCase().includes(query) ||
      llm.baseUrl.toLowerCase().includes(query)
    );
  }, [llms, searchQuery]);

  if (loading) return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <LLMManagementSkeleton />
    </ScrollArea>
  );

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="container mx-auto px-4 py-8">

        {llms.length === 0 ? (
          <Empty>
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <CpuIcon />
              </EmptyMedia>
              <EmptyTitle>LLM Providers</EmptyTitle>
              <EmptyDescription>
                Connect your first LLM provider to get started.
              </EmptyDescription>
            </EmptyHeader>
            <EmptyContent>
              <Button onClick={() => setShowDialog(true)}>
                <PlusIcon className="h-4 w-4" />Add
              </Button>
            </EmptyContent>
          </Empty>
        ) : (
          <div className="space-y-6">
            <div className="flex justify-between items-center gap-4">
              <h1 className="text-2xl font-bold">LLM Providers</h1>
              <Button onClick={() => setShowDialog(true)}><Plus className="h-4 w-4" />Add LLM</Button>
            </div>

            <div className="flex-1 md:w-1/2"> 
              <Input
                id="search-llm"
                placeholder="Search by Name, Model, or URL"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                aria-label="Search LLM providers"
              />
            </div>

            {filteredLlms.length === 0 ? (
              <div className="text-center py-12 text-muted-foreground">
                No LLM providers found matching "{searchQuery}"
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {filteredLlms.map((llm) => (
                <Card key={llm.id} className="flex flex-col bg-card/90 shadow-sm">
                  <CardHeader className="p-4 gap-3">
                    <div className="flex items-center justify-between">
                      <CardTitle className="text-base md:text-lg">{llm.name}</CardTitle>
                      <Badge variant={llm.isActive ? 'default' : 'outline'} className="flex items-center gap-1">
                        {llm.isActive ? <CheckCircle2 className="h-3.5 w-3.5" /> : <PauseCircle className="h-3.5 w-3.5" />}
                        <span>{llm.isActive ? 'Active' : 'Inactive'}</span>
                      </Badge>
                    </div>

                    <div className="flex flex-col gap-2 text-sm text-muted-foreground">
                      <div className="flex">
                        <span className="w-16 tracking-wide">URL</span>
                        <a
                          href={llm.baseUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="break-all hover:underline"
                        >
                          {llm.baseUrl.replace(/^https?:\/\//, '')}
                        </a>
                      </div>

                      <div className="flex">
                        <span className="w-16 tracking-wide">Model</span>
                        <span>{llm.model}</span>
                      </div>

                      <div className="flex">
                        <span className="w-16 tracking-wide">Auth</span>
                        <span>{llm.authType}</span>
                      </div>

                      <div className="flex">
                        <span className="w-16 tracking-wide">Created</span>
                        <span>
                          {new Date(llm.createdAt).toLocaleDateString('en-US', {
                            month: 'short',
                            day: 'numeric',
                            year: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit',
                          })}
                        </span>
                      </div>
                    </div>
                  </CardHeader>

                  <CardFooter className="mt-auto flex items-center justify-end gap-2 border-t px-4 py-3">
                    <div className="flex gap-2">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleTest(llm.id)}
                        aria-label={`Test ${llm.name}`}
                      >
                        Test
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleEditClick(llm)}
                        aria-label={`Edit ${llm.name}`}
                      >
                        <Pencil className="h-4 w-4" />
                        Edit
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={() => handleDeleteClick(llm)}
                        aria-label={`Delete ${llm.name}`}
                      >
                        Delete
                      </Button>
                    </div>
                  </CardFooter>
                </Card>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      <Dialog open={showDialog} onOpenChange={setShowDialog}>
        <DialogContent className="p-3 w-[90vw] max-w-[90vw] mx-auto rounded-lg md:w-auto md:max-w-lg">
          <DialogHeader className="text-left">
            <DialogTitle>Add LLM Configuration</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleCreate} className="space-y-4">
            <div>
              <Label htmlFor="name">Name *</Label>
              <Input id="name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} placeholder="My Local Ollama" required />
            </div>
            <div>
              <Label htmlFor="baseUrl">Base URL *</Label>
              <Input id="baseUrl" value={form.baseUrl} onChange={(e) => setForm({ ...form, baseUrl: e.target.value })} placeholder="http://localhost:11434/v1/" required />
              <p className="text-xs text-muted-foreground mt-1">Examples: Ollama: http://localhost:11434/v1/, Groq: https://api.groq.com/openai/v1/</p>
            </div>
            <div>
              <Label htmlFor="model">Model *</Label>
              <Input id="model" value={form.model} onChange={(e) => setForm({ ...form, model: e.target.value })} placeholder="llama2" required />
            </div>
            <div>
              <Label htmlFor="apiKey">API Key (optional)</Label>
              <Input id="apiKey" type="password" value={form.apiKey} onChange={(e) => setForm({ ...form, apiKey: e.target.value })} placeholder="Leave empty for local LLMs" />
            </div>
            <div>
              <Label htmlFor="authType">Authentication Type</Label>
              <Select value={form.authType || 'bearer'} onValueChange={(value) => setForm({ ...form, authType: value })}>
                <SelectTrigger id="authType">
                  <SelectValue placeholder="Select auth type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="bearer">Bearer Token (OpenAI, Groq, Anthropic)</SelectItem>
                  <SelectItem value="x-goog-api-key">X-Goog-API-Key (Google Gemini)</SelectItem>
                  <SelectItem value="api-key">API-Key Header (Azure OpenAI)</SelectItem>
                  <SelectItem value="x-api-key">X-API-Key Header</SelectItem>
                  <SelectItem value="url-param">URL Parameter (Legacy APIs)</SelectItem>
                  <SelectItem value="none">No Authentication (Ollama)</SelectItem>
                </SelectContent>
              </Select>
              <p className="text-xs text-muted-foreground mt-1">Choose the authentication method required by your LLM provider</p>
            </div>
            <div>
              <Label htmlFor="endpoint">Endpoint</Label>
              <Input id="endpoint" value={form.endpoint} onChange={(e) => setForm({ ...form, endpoint: e.target.value })} placeholder="chat/completions" />
            </div>
            <DialogFooter className="flex justify-end gap-3 md:gap-0">
              <Button type="button" variant="outline" onClick={() => setShowDialog(false)}>Cancel</Button>
              <Button type="submit">Save</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={editDialog.open} onOpenChange={(open) => !editDialog.isLoading && setEditDialog({ open, llm: null, isLoading: false })}>
        <DialogContent className="p-3 w-[90vw] max-w-[90vw] mx-auto rounded-lg md:w-auto md:max-w-lg">
          <DialogHeader className="text-left">
            <DialogTitle>Edit LLM Configuration</DialogTitle>
            <DialogDescription>Welcome to ArcLeave API key blank to keep the existing key.</DialogDescription>
          </DialogHeader>
          <form onSubmit={handleEditSave} className="space-y-4">
            <div>
              <Label htmlFor="edit-name">Name *</Label>
              <Input id="edit-name" value={editForm.name} onChange={(e) => setEditForm({ ...editForm, name: e.target.value })} placeholder="My Local Ollama" required />
            </div>
            <div>
              <Label htmlFor="edit-baseUrl">Base URL *</Label>
              <Input id="edit-baseUrl" value={editForm.baseUrl} onChange={(e) => setEditForm({ ...editForm, baseUrl: e.target.value })} placeholder="Examples: Groq: https://api.groq.com/openai/v1/" required />
            </div>
            <div>
              <Label htmlFor="edit-model">Model *</Label>
              <Input id="edit-model" value={editForm.model} onChange={(e) => setEditForm({ ...editForm, model: e.target.value })} placeholder="llama2" required />
            </div>
            <div>
              <Label htmlFor="edit-apiKey">API Key</Label>
              <Input id="edit-apiKey" type="password" value={editForm.apiKey} onChange={(e) => setEditForm({ ...editForm, apiKey: e.target.value })} placeholder="Leave blank to keep existing key" />
            </div>
            <div>
              <Label htmlFor="edit-authType">Authentication Type</Label>
              <Select value={editForm.authType || 'bearer'} onValueChange={(value) => setEditForm({ ...editForm, authType: value })}>
                <SelectTrigger id="edit-authType">
                  <SelectValue placeholder="Select auth type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="bearer">Bearer Token (OpenAI, Groq, Anthropic)</SelectItem>
                  <SelectItem value="x-goog-api-key">X-Goog-API-Key (Google Gemini)</SelectItem>
                  <SelectItem value="api-key">API-Key Header (Azure OpenAI)</SelectItem>
                  <SelectItem value="x-api-key">X-API-Key Header</SelectItem>
                  <SelectItem value="url-param">URL Parameter (Legacy APIs)</SelectItem>
                  <SelectItem value="none">No Authentication (Ollama)</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label htmlFor="edit-endpoint">Endpoint</Label>
              <Input id="edit-endpoint" value={editForm.endpoint} onChange={(e) => setEditForm({ ...editForm, endpoint: e.target.value })} placeholder="chat/completions" />
            </div>
            <DialogFooter className="flex justify-end gap-3 md:gap-0">
              <Button type="button" variant="outline" onClick={() => setEditDialog({ open: false, llm: null, isLoading: false })} disabled={editDialog.isLoading}>Cancel</Button>
              <Button type="submit" disabled={editDialog.isLoading} aria-busy={editDialog.isLoading}>
                {editDialog.isLoading ? 'Saving...' : 'Save Changes'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <DeleteConfirmationDialog
        open={deleteDialog.open}
        onOpenChange={(open) => !deleteDialog.isDeleting && setDeleteDialog({ open, llm: null, isDeleting: false })}
        onConfirm={handleDeleteConfirm}
        title={`Delete LLM provider "${deleteDialog.llm?.name}"?`}
        description="This action cannot be undone. Workflows using this LLM provider will fail."
        isLoading={deleteDialog.isDeleting}
      />
    </ScrollArea>
  );
}