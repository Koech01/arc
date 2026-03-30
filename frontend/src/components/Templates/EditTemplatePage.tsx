import { toast } from 'sonner';
import { Plus, Trash2 } from 'lucide-react';
import { useState, useEffect } from 'react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { templateApi, llmApi } from '@/lib/api';
import { Skeleton } from '@/components/ui/skeleton';
import { Textarea } from '@/components/ui/textarea';
import { extractVariables } from '@/lib/templateUtils';
import type { LLMConfig } from '@/components/types/llm';
import { ScrollArea } from '@/components/ui/scroll-area';
import { useNavigate, useParams } from 'react-router-dom';
import type { TemplateTask } from '@/components/types/template';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';


export function EditTemplatePage() {
  const { name } = useParams<{ name: string }>();
  const navigate = useNavigate();

  const [description, setDescription] = useState('');
  const [triggerType, setTriggerType] = useState<'manual' | 'scheduled' | 'webhook'>('manual');
  const [llmConfigId, setLlmConfigId] = useState('');
  const [llms, setLlms] = useState<LLMConfig[]>([]);
  const [tasks, setTasks] = useState<TemplateTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      if (!name) return;
      try {
        const [template, llmData] = await Promise.all([
          templateApi.getByName(name),
          llmApi.getAll(),
        ]);
        setDescription(template.description ?? '');
        setTriggerType(template.triggerType);
        setLlmConfigId(template.llmConfigId ?? '');
        setTasks(template.tasks);
        setLlms(llmData);
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'Failed to load template', { position: 'top-center' });
        navigate('/templates');
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [name, navigate]);

  const addTask = () => {
    const nextTaskNumber = tasks.length + 1;
    setTasks([...tasks, { id: `task-${nextTaskNumber}`, name: '', agentType: 'llm', prompt: '', config: {}, dependencies: [] }]);
  };

  const removeTask = (index: number) => {
    if (tasks.length === 1) {
      toast.error('At least one task is required', { position: 'top-center' });
      return;
    }
    setTasks(tasks.filter((_, i) => i !== index));
  };

  const updateTask = (index: number, field: keyof TemplateTask, value: string | string[]) => {
    const updated = [...tasks];
    const task = updated[index];
    
    if (field === 'dependencies') {
      task.dependencies = value as string[];
    } else if (field === 'config') {
      task.config = value as Record<string, string>;
    } else if (field === 'id') {
      task.id = value as string;
    } else if (field === 'name') {
      task.name = value as string;
    } else if (field === 'agentType') {
      task.agentType = value as 'llm' | 'http' | 'python' | 'sql' | 'email';
    } else if (field === 'prompt') {
      task.prompt = value as string;
    } else if (field === 'llmConfigId') {
      task.llmConfigId = value as string;
    }
    
    setTasks(updated);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name) return;

    if (tasks.some(t => !t.name || !t.id)) {
      toast.error('All tasks must have an ID and name', { position: 'top-center' });
      return;
    }

    setSaving(true);
    try {
      await templateApi.update(name, { name, description, triggerType, llmConfigId: llmConfigId || undefined, tasks });
      toast.success('Template updated successfully', { position: 'top-center' });
      navigate('/templates');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update template', { position: 'top-center' });
    } finally {
      setSaving(false);
    }
  };

  const detectedVariables = extractVariables(tasks);

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <div className="p-6 w-full">
          <Card className="pb-4 max-w-4xl">
            <CardHeader className="pl-4">
              <Skeleton className="h-7 w-48" />
              <Skeleton className="h-4 w-72 mt-1" />
            </CardHeader>
            <CardContent className="space-y-4">
              {[1, 2, 3].map(i => <Skeleton key={i} className="h-10 w-full" />)}
            </CardContent>
          </Card>
        </div>
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="p-6 w-full">
        <Card className="pb-4 max-w-4xl">
          <CardHeader className="pl-4">
            <CardTitle>Edit Template</CardTitle>
            <CardDescription>
              Editing <span>{name}</span> - template name is fixed as it is the identifier
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-6">
              <div className="space-y-4">
                <div>
                  <Label htmlFor="name">Template Name</Label>
                  <Input id="name" value={name ?? ''} disabled className="bg-muted" />
                  <p className="text-sm text-muted-foreground mt-1">Name cannot be changed. Create a new template to use a different name.</p>
                </div>

                <div>
                  <Label htmlFor="description">Description</Label>
                  <Input
                    id="description"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    placeholder="Brief description of the template"
                  />
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <Label htmlFor="triggerType">
                      Trigger Type <span className="text-red-500">*</span>
                    </Label>
                    <Select value={triggerType} onValueChange={(v) => setTriggerType(v as typeof triggerType)}>
                      <SelectTrigger id="triggerType">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="manual">Manual</SelectItem>
                        <SelectItem value="scheduled">Scheduled</SelectItem>
                        <SelectItem value="webhook">Webhook</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>

                  <div>
                    <Label htmlFor="llmConfigId">LLM Provider</Label>
                    <Select value={llmConfigId} onValueChange={setLlmConfigId}>
                      <SelectTrigger id="llmConfigId">
                        <SelectValue placeholder="Select LLM (optional)" />
                      </SelectTrigger>
                      <SelectContent>
                        {llms.map((llm) => (
                          <SelectItem key={llm.id} value={llm.id}>{llm.name}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              </div>

              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <Label className="text-base">
                    Tasks <span className="text-red-500">*</span>
                  </Label>
                  <Button type="button" size="sm" variant="outline" onClick={addTask}>
                    <Plus className="h-4 w-4" />Add Task
                  </Button>
                </div>

                {tasks.map((task, index) => (
                  <Card key={index} className="p-4">
                    <div className="space-y-3">
                      <div className="flex items-center justify-between">
                        <h4 className="font-medium">Task {index + 1}</h4>
                        {tasks.length > 1 && (
                          <Button type="button" size="sm" variant="ghost" onClick={() => removeTask(index)}>
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        )}
                      </div>

                      <div className="grid grid-cols-2 gap-3">
                        <div>
                          <Label htmlFor={`task-${index}-id`}>Task ID</Label>
                          <Input
                            id={`task-${index}-id`}
                            value={task.id}
                            onChange={(e) => updateTask(index, 'id', e.target.value)}
                            placeholder="task-1"
                            required
                          />
                        </div>
                        <div>
                          <Label htmlFor={`task-${index}-agentType`}>Agent Type</Label>
                          <Select value={task.agentType} onValueChange={(v) => updateTask(index, 'agentType', v)}>
                            <SelectTrigger id={`task-${index}-agentType`}>
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="llm">LLM / AI Agent</SelectItem>
                              <SelectItem value="http">HTTP Agent</SelectItem>
                              <SelectItem value="python">Python Agent</SelectItem>
                              <SelectItem value="sql">SQL Agent</SelectItem>
                              <SelectItem value="email">Email Agent</SelectItem>
                            </SelectContent>
                          </Select>
                        </div>
                      </div>

                      <div>
                        <Label htmlFor={`task-${index}-name`}>Task Name (supports {'{{variable}}'})</Label>
                        <Input
                          id={`task-${index}-name`}
                          value={task.name}
                          onChange={(e) => updateTask(index, 'name', e.target.value)}
                          placeholder="e.g., Process {{dataset}} data"
                          required
                        />
                      </div>

                      <div>
                        <Label htmlFor={`task-${index}-prompt`}>Prompt (supports {'{{variable}}'})</Label>
                        <Textarea
                          id={`task-${index}-prompt`}
                          value={task.prompt}
                          onChange={(e) => updateTask(index, 'prompt', e.target.value)}
                          placeholder="Instructions for the agent"
                          className="min-h-[80px]"
                        />
                      </div>

                      <div>
                        <Label htmlFor={`task-${index}-dependencies`}>Dependencies (comma-separated task IDs)</Label>
                        <Input
                          id={`task-${index}-dependencies`}
                          value={task.dependencies?.join(', ') || ''}
                          onChange={(e) => updateTask(index, 'dependencies', e.target.value.split(',').map(s => s.trim()).filter(Boolean))}
                          placeholder="e.g., task-1, task-2"
                        />
                      </div>
                    </div>
                  </Card>
                ))}
              </div>

              {detectedVariables.length > 0 && (
                <Card className="p-4 bg-muted/50">
                  <h4 className="font-medium mb-2">Detected Variables</h4>
                  <div className="flex flex-wrap gap-2">
                    {detectedVariables.map(v => (
                      <code key={v} className="px-2 py-1 bg-background rounded text-sm">
                        {`{{${v}}}`}
                      </code>
                    ))}
                  </div>
                  <p className="text-sm text-muted-foreground mt-2">
                    Users will be prompted to provide values for these variables when instantiating the template.
                  </p>
                </Card>
              )}

              <div className="flex gap-2">
                <Button type="submit" disabled={saving} aria-busy={saving}>
                  {saving ? 'Saving...' : 'Save Changes'}
                </Button>
                <Button type="button" variant="outline" onClick={() => navigate('/templates')}>
                  Cancel
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </ScrollArea>
  );
}