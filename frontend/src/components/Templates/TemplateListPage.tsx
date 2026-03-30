import { toast } from 'sonner';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { templateApi, llmApi } from '@/lib/api';
import { Plus, CopyCheckIcon } from 'lucide-react';
import { useEffect, useState, useMemo } from 'react';
import { formatSmartDateTime } from '@/lib/dateFormat';
import type { LLMConfig } from '@/components/types/llm';
import { ScrollArea } from '@/components/ui/scroll-area';
import { TemplateListSkeleton } from './TemplateListSkeleton';
import type { ExecutionTemplate } from '@/components/types/template';
import { DeleteConfirmationDialog } from '@/components/ui/delete-confirmation-dialog';
import { Card, CardFooter, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty';


export function TemplateListPage() {
  const navigate = useNavigate();
  const [templates, setTemplates] = useState<ExecutionTemplate[]>([]);
  const [llms, setLlms] = useState<LLMConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [deleteDialog, setDeleteDialog] = useState<{ open: boolean; template: ExecutionTemplate | null; isDeleting: boolean }>({
    open: false,
    template: null,
    isDeleting: false,
  });

  useEffect(() => {
    fetchTemplates();
  }, []);

  const fetchTemplates = async () => {
    setLoading(true);
    try {
      const [data, llmData] = await Promise.all([templateApi.getAll(), llmApi.getAll()]);
      setTemplates(data);
      setLlms(llmData);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load templates', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  const getLlmName = (llmConfigId?: string) =>
    llmConfigId ? (llms.find(l => l.id === llmConfigId)?.name ?? null) : null;

  const handleDeleteClick = (template: ExecutionTemplate) => {
    setDeleteDialog({ open: true, template, isDeleting: false });
  };

  const handleDeleteConfirm = async () => {
    if (!deleteDialog.template) return;
    setDeleteDialog(prev => ({ ...prev, isDeleting: true }));
    try {
      await templateApi.delete(deleteDialog.template.name);
      setTemplates(templates.filter(t => t.name !== deleteDialog.template!.name));
      toast.success('Template deleted successfully', { position: 'top-center' });
      setDeleteDialog({ open: false, template: null, isDeleting: false });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete template', { position: 'top-center' });
      setDeleteDialog(prev => ({ ...prev, isDeleting: false }));
    }
  };

  const filteredTemplates = useMemo(() => {
    if (!searchQuery) return templates;
    const query = searchQuery.toLowerCase();
    return templates.filter(template => 
      template.name.toLowerCase().includes(query) ||
      template.description.toLowerCase().includes(query)
    );
  }, [templates, searchQuery]);

  if (loading) return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <TemplateListSkeleton />
    </ScrollArea>
  );

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="container mx-auto px-4 py-8"> 
        {templates.length === 0 ? (
          <Empty>
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <CopyCheckIcon />
              </EmptyMedia>
              <EmptyTitle>Execution Templates</EmptyTitle>
              <EmptyDescription>
                Create your first execution template to get started.
              </EmptyDescription>
            </EmptyHeader>
            <EmptyContent>
              <Button onClick={() => navigate('/templates/new')}>
                <Plus className="h-4 w-4" />Create
              </Button>
            </EmptyContent>
          </Empty>
        ) : (
          <div className="space-y-6">
            <div className="flex justify-between items-center gap-4">
              <h1 className="text-2xl font-bold">Execution Templates</h1>
              <Button onClick={() => navigate('/templates/new')}><Plus className="h-4 w-4" />Create Template</Button>
            </div>

            <div className="flex-1 md:w-3/4"> 
              <Input
                id="search-template"
                placeholder="Search by Name or Description"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                aria-label="Search templates"
              />
            </div>

            {filteredTemplates.length === 0 ? (
              <div className="text-center py-12 text-muted-foreground">
                No templates found matching "{searchQuery}"
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {filteredTemplates.map((template) => (
                  <Card key={template.name} className="flex flex-col">
                    <CardHeader className="p-4 gap-2">
                      <div className="flex items-start justify-between gap-2">
                        <CardTitle className="flex-1">{template.name}</CardTitle>
                        <Badge variant="outline" className="text-xs">
                          Used {template.useCount}x
                        </Badge>
                      </div>
                      <CardDescription>{template.description}</CardDescription> 
                      <CardDescription>
                        LLM: <span className="font-medium text-foreground">{getLlmName(template.llmConfigId)}</span>
                      </CardDescription> 
                      <CardDescription>
                        Created <span className="font-medium text-foreground">{formatSmartDateTime(template.createdAtUtc)}</span>
                      </CardDescription> 
                    </CardHeader>
                    <CardFooter className="flex justify-end items-center gap-2 py-3 px-3 mt-auto border-0 border-t border-border">
                      <div className="flex gap-2">
                        <Button size="sm" variant="outline" onClick={() => navigate(`/templates/${template.name}/use`)}>Instantiate</Button>
                        <Button size="sm" variant="outline" onClick={() => navigate(`/templates/${template.name}/edit`)}>Edit</Button>
                        <Button size="sm" variant="destructive" onClick={() => handleDeleteClick(template)}>Delete</Button>
                      </div>
                    </CardFooter>
                  </Card>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      <DeleteConfirmationDialog
        open={deleteDialog.open}
        onOpenChange={(open) => !deleteDialog.isDeleting && setDeleteDialog({ open, template: null, isDeleting: false })}
        onConfirm={handleDeleteConfirm}
        title={`Delete template "${deleteDialog.template?.name}"?`}
        description="This action cannot be undone. This will not affect workflows already created from this template."
        isLoading={deleteDialog.isDeleting}
      />
    </ScrollArea>
  );
}