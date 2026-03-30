import { toast } from 'sonner';
import { useState, useEffect } from 'react';
import { Plus, Trash2 } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useNavigate } from 'react-router-dom';
import { workflowApi, llmApi } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Textarea } from "@/components/ui/textarea";
import type { LLMConfig } from '@/components/types/llm';
import type { WorkflowTask } from '@/components/types/workflow';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';


export function CreateWorkflowForm() {
  const navigate = useNavigate();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [triggerType, setTriggerType] = useState<'manual' | 'scheduled' | 'webhook'>('manual');
  const [llmConfigId, setLlmConfigId] = useState('');
  const [llms, setLlms] = useState<LLMConfig[]>([]);
  const [tasks, setTasks] = useState<WorkflowTask[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const fetchLLMs = async () => {
      try {
        const data = await llmApi.getAll();
        setLlms(data);
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'Failed to load LLM providers', { position: 'top-center' });
      }
    };
    fetchLLMs();
  }, []);

  const addTask = () => {
    const nextTaskNumber = tasks.length + 1;
    setTasks([...tasks, {
      id: `task-${nextTaskNumber}`,
      name: '',
      agentType: '',
      prompt: '',
      config: {},
      dependencies: [],
    }]);
  };

  const removeTask = (id: string) => {
    setTasks(tasks.filter(t => t.id !== id));
  };

  const updateTask = (id: string, field: keyof WorkflowTask, value: string | string[]) => {
    setTasks(tasks.map(t => t.id === id ? { ...t, [field]: value } : t));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      const workflow = await workflowApi.create({
        name,
        description,
        tasks,
        triggerType,
        llmConfigId: llmConfigId || undefined,
      });
      navigate(`/workflows/${workflow.id}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to create workflow', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6 px-4 md:px-6">
      <Card className="pt-4 pb-4">
        <CardHeader className="pb-2">
          <CardTitle className="text-lg">Workflow Details</CardTitle>
          <CardDescription>Set the workflow name, description, trigger, and optional default LLM provider.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4 pb-0">
          <div className="space-y-2">
            <Label htmlFor="name">Workflow Name</Label>
            <Input
              id="name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="My Workflow"
              required
              aria-required="true"
              aria-label="Workflow name"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>

            <Textarea
              id="description"
              placeholder="Workflow description"
              className="min-h-[100px]"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>
          
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="trigger">Trigger Type</Label>
              <Select value={triggerType} onValueChange={(v) => setTriggerType(v as typeof triggerType)}>
                <SelectTrigger id="trigger" aria-label="Trigger type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="manual">Manual</SelectItem>
                  <SelectItem value="scheduled">Scheduled</SelectItem>
                  <SelectItem value="webhook">Webhook</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="llm">LLM Provider</Label>
              <Select value={llmConfigId} onValueChange={setLlmConfigId}>
                <SelectTrigger id="llm" aria-label="LLM provider">
                  <SelectValue placeholder="Select LLM (optional)" />
                </SelectTrigger>
                <SelectContent>
                  {llms.map((llm) => (
                    <SelectItem key={llm.id} value={llm.id}>{llm.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {llms.length === 0 && (
                <p className="text-xs text-muted-foreground">No LLMs configured. <a href="/llms" className="underline">Add one</a></p>
              )}
            </div>
          </div>
        </CardContent>
      </Card>
  
      <Card className="pb-5">
        <CardHeader>
          <div className="flex items-center justify-between gap-4 flex-wrap"> 
            <div>
              <CardTitle className="text-lg">Tasks</CardTitle>
              <CardDescription>Add and configure tasks in execution order.</CardDescription>
            </div>
            <Button type="button" onClick={addTask} aria-label="Add task">
              <Plus className="h-4 w-4"/>
              Add Task
            </Button>
          </div>
        </CardHeader>

        <CardContent className="space-y-2">
          {tasks.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-6">
              No tasks added. Click "Add Task" to get started.
            </p>
          ) : (
            tasks.map((task, idx) => (
              <Card className="pb-3" key={task.id}>
                <CardContent className="pt-4 space-y-4">
                  <div className="flex items-center justify-between">
                    <h4 className="font-medium">Task {idx + 1}</h4>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => removeTask(task.id)}
                      aria-label={`Remove task ${idx + 1}`}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor={`task-id-${task.id}`}>Task ID <span className="text-red-500">*</span></Label>
                    <Input
                      id={`task-id-${task.id}`}
                      value={task.id}
                      onChange={(e) => updateTask(task.id, 'id', e.target.value.toLowerCase().replace(/[^a-z0-9_-]/g, '-'))}
                      placeholder="e.g., fetch-data, task-1"
                      required
                      pattern="[a-z0-9_-]+"
                      aria-label={`Task ${idx + 1} ID`}
                    />
                    <p className="text-xs text-muted-foreground">Unique identifier for this task. Use in prompts as {'{{'}{task.id}{'}}'}</p>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor={`task-name-${task.id}`}>Task Name</Label>
                    <Input
                      id={`task-name-${task.id}`}
                      value={task.name}
                      onChange={(e) => updateTask(task.id, 'name', e.target.value)}
                      placeholder="Task name"
                      required
                      aria-required="true"
                      aria-label={`Task ${idx + 1} name`}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor={`task-agent-${task.id}`}>Agent Type</Label>
                    <Select
                      value={task.agentType}
                      onValueChange={(v) => updateTask(task.id, 'agentType', v)}
                    >
                      <SelectTrigger id={`task-agent-${task.id}`} aria-label={`Task ${idx + 1} agent type`}   className="max-w-[200px]">
                        <SelectValue placeholder="Select agent type" />
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

                  <div className="space-y-2">
                    <Label htmlFor={`task-prompt-${task.id}`}>
                      Task Prompt {task.agentType === 'llm' && <span className="text-red-500">*</span>}
                    </Label>
                    <Textarea
                      id={`task-prompt-${task.id}`}
                      value={task.prompt || ''}
                      onChange={(e) => updateTask(task.id, 'prompt', e.target.value)}
                      placeholder={task.agentType === 'llm' ? "e.g., Write a summary of: {{task-1}}" : "Provide specific instructions for this task. Use {{task-id}} for dynamic values from previous tasks."}
                      rows={4}
                      maxLength={5000}
                      className="text-sm"
                      aria-label={`Task ${idx + 1} prompt`}
                      required={task.agentType === 'llm'}
                    />
                    <p className="text-xs text-muted-foreground">
                      {task.agentType === 'llm' && 'Required for LLM tasks. '}
                      Use {'{{'} task-id {'}}' } to reference previous task outputs. {task.prompt?.length || 0} / 5000 characters
                      {task.prompt && task.prompt.length > 4500 && (
                        <span className="text-yellow-600 ml-2">⚠️ Approaching limit</span>
                      )}
                    </p>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor={`task-deps-${task.id}`}>Dependencies</Label>
                    <Input
                      id={`task-deps-${task.id}`}
                      value={task.dependencies.join(', ')}
                      onChange={(e) => updateTask(task.id, 'dependencies', e.target.value.split(',').map(s => s.trim()).filter(Boolean))}
                      placeholder="e.g., task-1, task-2"
                      className="text-sm"
                      aria-label={`Task ${idx + 1} dependencies`}
                    />
                    <p className="text-xs text-muted-foreground">Comma-separated task IDs that must complete before this task</p>
                  </div>
                </CardContent>
              </Card>
            ))
          )}
        </CardContent>
      </Card>
    

      <div className="flex flex-col sm:flex-row gap-3">
        <Button
          type="submit"
          disabled={loading || !name || tasks.length === 0}
          aria-busy={loading}
          className="sm:w-auto"
        >
          {loading ? 'Creating...' : 'Create Workflow'}
        </Button>
        <Button
          type="button"
          variant="outline"
          onClick={() => navigate('/dashboard')}
          className="sm:w-auto"
        >
          Cancel
        </Button>
      </div>
    </form>
  );
}