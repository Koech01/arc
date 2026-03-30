import { toast } from 'sonner';
import { LogsViewer } from './LogsViewer';
import { useState, useEffect } from 'react';
import { OverviewTab } from './OverviewTab';
import { useParams } from 'react-router-dom'; 
import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { OutputsSection } from './OutputsSection';
import { ExecutionGraph } from './ExecutionGraph';
import { ExecutionHeader } from './ExecutionHeader';
import { MetadataSection } from './MetadataSection';
import { Textarea } from '@/components/ui/textarea';
import { TaskDetailsSheet } from './TaskDetailsSheet';
import { ExecutionTimeline } from './ExecutionTimeline';
import { Card, CardContent } from '@/components/ui/card';
import { ScrollArea } from '@/components/ui/scroll-area';
import { ExecutionPageSkeleton } from './ExecutionPageSkeleton';
import type { Notification } from '@/components/types/notification';
import { executionApi, notificationApi, goldenExecutionsApi } from '@/lib/api';
import { CheckCircle2Icon, LoaderIcon, XCircleIcon, Star } from 'lucide-react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { extractExecutionId, isTaskNotification } from '@/lib/notification-utils';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import type { Execution, Task, ExecutionLog, ExecutionOutput, ExecutionMetadata, ExecutionTaskSummary } from '@/components/types';


export function ExecutionPage() {
  const { id } = useParams<{ id: string }>();
  const [execution, setExecution] = useState<Execution | null>(null);
  const [tasks, setTasks] = useState<Task[]>([]);
  const [logs, setLogs] = useState<ExecutionLog[]>([]);
  const [outputs, setOutputs] = useState<ExecutionOutput[]>([]);
  const [metadata, setMetadata] = useState<ExecutionMetadata | null>(null);
  const [taskNotifications, setTaskNotifications] = useState<Notification[]>([]);
  const [selectedTask, setSelectedTask] = useState<Task | null>(null);
  const [sheetOpen, setSheetOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('overview');
  const [isGolden, setIsGolden] = useState(false);
  const [goldenDialogOpen, setGoldenDialogOpen] = useState(false);

  const taskSummaryVariant: Record<ExecutionTaskSummary['status'], 'default' | 'destructive' | 'secondary'> = {
    success: 'default',
    failed: 'destructive',
    running: 'secondary',
  };

  const taskSummaryIcon: Record<ExecutionTaskSummary['status'], React.ReactNode> = {
    success: <CheckCircle2Icon className="h-3.5 w-3.5 text-green-500" />,
    failed: <XCircleIcon className="h-3.5 w-3.5 text-red-500" />,
    running: <LoaderIcon className="h-3.5 w-3.5 text-blue-500" />,
  };

  const formatTaskOutput = (message: string) => {
    const normalized = message
      .replace(/\r\n/g, '\n')
      .replace(/\s\*\s\*\*/g, '\n**')
      .replace(/\s\*\s/g, '\n- ')
      .replace(/\*\*(.*?)\*\*/g, '$1')
      .split('\n')
      .map(line => line.trim())
      .filter(Boolean);

    const blocks: Array<{ type: 'bullet' | 'text'; content: string }> = normalized.map((line) => {
      if (line.startsWith('- ')) {
        return { type: 'bullet', content: line.replace(/^-\s*/, '') };
      }
      return { type: 'text', content: line };
    });

    return blocks;
  };

  useEffect(() => {
    if (!id) return;
    fetchExecutionData(id);
    checkIfGolden(id);
  }, [id]);

  const checkIfGolden = async (executionId: string) => {
    try {
      const result = await goldenExecutionsApi.list();
      setIsGolden(result.goldenExecutions.some(g => g.executionId === executionId));
    } catch {
      setIsGolden(false);
    }
  };

  useEffect(() => {
    if (!id) return;

    fetchTaskNotifications(id);
    const interval = setInterval(() => fetchTaskNotifications(id), 30000);

    return () => clearInterval(interval);
  }, [id]);

  const fetchTaskNotifications = async (executionId: string) => {
    try {
      const notifications = await notificationApi.getAll();
      const timelineNotifications = notifications
        .filter(n => isTaskNotification(n.title) && extractExecutionId(n.message) === executionId)
        .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
      setTaskNotifications(timelineNotifications);
    } catch {
      setTaskNotifications([]);
    }
  };

  const fetchExecutionData = async (executionId: string) => {
    setLoading(true);
    
    try {
      const execData = await executionApi.getExecution(executionId);
      setExecution(execData);

      const [tasksResult, logsResult, outputsResult, metadataResult] = await Promise.allSettled([
        executionApi.getTasks(executionId),
        executionApi.getLogs(executionId),
        executionApi.getOutputs(executionId),
        executionApi.getMetadata(executionId),
      ]);

      setTasks(tasksResult.status === 'fulfilled' ? tasksResult.value : []);
      setLogs(logsResult.status === 'fulfilled' ? logsResult.value : []);
      setOutputs(outputsResult.status === 'fulfilled' ? outputsResult.value : []);
      setMetadata(metadataResult.status === 'fulfilled' ? metadataResult.value : null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load execution data', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  const handleTaskClick = (task: Task) => {
    setSelectedTask(task);
    setSheetOpen(true);
  };

  if (loading) {
    return <ExecutionPageSkeleton />;
  }

  if (!execution) {
    return (
      <div className="flex items-center justify-center h-screen">
        <p className="text-muted-foreground">Execution not found</p>
      </div>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-6 p-6">
        <ExecutionHeader execution={execution} />
        
        <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
          <div className="md:hidden mb-4">
            <Select value={activeTab} onValueChange={setActiveTab}>
              <SelectTrigger className="w-full" aria-label="Select tab">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="overview">Overview</SelectItem>
                <SelectItem value="tasks">Tasks</SelectItem>
                <SelectItem value="timeline">Timeline</SelectItem>
                <SelectItem value="graph">Graph</SelectItem>
                <SelectItem value="logs">Logs</SelectItem>
                <SelectItem value="outputs">Outputs</SelectItem>
                <SelectItem value="metadata">Metadata</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <TabsList className="hidden md:grid w-full grid-cols-7">
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="tasks">Tasks</TabsTrigger>
            <TabsTrigger value="timeline">Timeline</TabsTrigger>
            <TabsTrigger value="graph">Graph</TabsTrigger>
            <TabsTrigger value="logs">Logs</TabsTrigger>
            <TabsTrigger value="outputs">Outputs</TabsTrigger>
            <TabsTrigger value="metadata">Metadata</TabsTrigger>
          </TabsList>

          <TabsContent value="overview" className="mt-6">
            <OverviewTab execution={execution} tasks={tasks} />
            
            <Card className="mt-6">
              <CardContent className="pt-3 pb-3">
                <h3 className="text-sm font-medium mb-3">Regression Testing</h3>
                {isGolden ? (
                  <div className="flex flex-col gap-3">
                    <div className="flex items-center gap-2">
                      <Badge variant="default" className="bg-yellow-500 hover:bg-yellow-600">
                        <Star className="w-3 h-3 mr-1 fill-current" />
                        Golden Baseline
                      </Badge>
                      <span className="text-sm text-muted-foreground">
                        This execution is marked as a reference baseline
                      </span>
                    </div>
                    <Button 
                      variant="outline" 
                      size="sm" 
                      onClick={async () => {
                        try {
                          await goldenExecutionsApi.unmark(execution.id);
                          setIsGolden(false);
                          toast.success('Golden status removed');
                        } catch (err) {
                          toast.error(err instanceof Error ? err.message : 'Failed to remove golden status');
                        }
                      }}
                      className="w-fit"
                    >
                      Remove Golden Status
                    </Button>
                  </div>
                ) : (
                  <div className="flex flex-col gap-3">
                    <p className="text-sm text-muted-foreground">
                      Mark this execution as a golden baseline to use it as a reference for regression testing
                    </p>
                    <MarkGoldenDialog
                      executionId={execution.id}
                      open={goldenDialogOpen}
                      onOpenChange={setGoldenDialogOpen}
                      onSuccess={() => {
                        setIsGolden(true);
                        setGoldenDialogOpen(false);
                      }}
                    />
                  </div>
                )}
              </CardContent>
            </Card>

            {taskNotifications.length > 0 && (
              <Card className="mt-6 pb-4">
                <CardContent className="pt-6">
                  <h3 className="text-sm font-medium mb-2">Task Progress</h3>
                  <ol className="space-y-2">
                    {taskNotifications.map(notification => (
                      <li key={notification.id} className="text-sm text-muted-foreground flex items-start gap-2.5 leading-relaxed">
                        <CheckCircle2Icon className="h-4 w-4 shrink-0 mt-0.5 text-green-600 dark:text-green-400" />
                        <span>{notification.message}</span>
                      </li>
                    ))}
                  </ol>
                </CardContent>
              </Card>
            )}
          </TabsContent>

          <TabsContent value="tasks" className="mt-6">
            <Card className="pb-4">
              <CardContent className="pt-6">
                <ol className="space-y-4">
                  {execution.tasks.map((task) => (
                    <li key={task.taskId} className="rounded-lg border bg-card p-4 shadow-sm">
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0 space-y-1">
                          <p className="font-semibold truncate">{task.executionOrder}. {task.taskName}</p>
                          <p className="text-sm text-muted-foreground">{task.taskId}</p>
                        </div>
                        <Badge variant={taskSummaryVariant[task.status]} className="capitalize flex items-center gap-1">
                          {taskSummaryIcon[task.status]}
                          <span>{task.status}</span>
                        </Badge>
                      </div>
                      <div className="mt-3 rounded-md border bg-muted/30 p-3">
                        {task.output?.trim() ? (
                          <div className="space-y-2 text-sm text-muted-foreground leading-relaxed">
                            {(() => {
                              const blocks = formatTaskOutput(task.output);
                              const grouped: Array<{ type: 'p' | 'ul'; items: string[] }> = [];

                              blocks.forEach((block) => {
                                const prev = grouped[grouped.length - 1];
                                if (block.type === 'bullet') {
                                  if (!prev || prev.type !== 'ul') grouped.push({ type: 'ul', items: [block.content] });
                                  else prev.items.push(block.content);
                                } else {
                                  grouped.push({ type: 'p', items: [block.content] });
                                }
                              });

                              return grouped.map((group, index) => {
                                if (group.type === 'ul') {
                                  return (
                                    <ul key={`ul-${index}`} className="list-disc pl-5 space-y-1">
                                      {group.items.map((item, itemIndex) => (
                                        <li key={`li-${index}-${itemIndex}`} className="break-words">{item}</li>
                                      ))}
                                    </ul>
                                  );
                                }
                                return <p key={`p-${index}`} className="break-words">{group.items[0]}</p>;
                              });
                            })()}
                          </div>
                        ) : (
                          <p className="text-sm text-muted-foreground leading-relaxed">No output</p>
                        )}
                      </div>
                    </li>
                  ))}
                </ol>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="timeline" className="mt-6">
            <ExecutionTimeline tasks={tasks} />
          </TabsContent>

          <TabsContent value="graph" className="mt-6">
            <ExecutionGraph tasks={tasks} onTaskClick={handleTaskClick} />
          </TabsContent>

          <TabsContent value="logs" className="mt-6">
            <LogsViewer logs={logs} />
          </TabsContent>

          <TabsContent value="outputs" className="mt-6">
            <Card className="pb-4">
              <CardContent className="pt-6">
                <OutputsSection outputs={outputs} />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="metadata" className="mt-6">
            {metadata ? (
              <MetadataSection metadata={metadata} />
            ) : (
              <Card>
                <CardContent className="pt-6 text-sm text-muted-foreground">
                  Metadata is unavailable for this execution.
                </CardContent>
              </Card>
            )}
          </TabsContent>
        </Tabs>

        <TaskDetailsSheet
          task={selectedTask}
          open={sheetOpen}
          onOpenChange={setSheetOpen}
        />
      </div>
    </ScrollArea>
  );
}

interface MarkGoldenDialogProps {
  executionId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: () => void;
}

function MarkGoldenDialog({ executionId, open, onOpenChange, onSuccess }: MarkGoldenDialogProps) {
  const [label, setLabel] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      await goldenExecutionsApi.mark(executionId, { label: label.trim() || undefined });
      toast.success('Execution marked as golden baseline');
      setLabel('');
      onSuccess();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to mark as golden');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        <Button variant="default" size="sm" className="w-fit">
          <Star className="w-4 h-4 mr-2" />
          Mark as Golden Baseline
        </Button>
      </DialogTrigger>
      <DialogContent className="p-3 w-[90vw] max-w-[90vw] mx-auto rounded-lg md:w-auto md:max-w-lg">
        <DialogHeader>
          <DialogTitle>Mark as Golden Baseline</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <Label htmlFor="label">Label (Optional)</Label>
            <Textarea
              id="label"
              value={label}
              onChange={(e) => setLabel(e.target.value)}
              placeholder="Baseline for GPT-4 migration"
              rows={3}
              maxLength={500}
            />
            <p className="text-xs text-muted-foreground mt-1">
              Add a note to help identify this baseline later
            </p>
          </div>

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting ? 'Marking...' : 'Mark as Golden'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}