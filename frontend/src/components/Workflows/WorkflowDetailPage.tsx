import { toast } from 'sonner';
import { workflowApi } from '@/lib/api';
import { formatDateTime } from '@/lib/date';
import { useState, useEffect } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ScrollArea } from "@/components/ui/scroll-area";
import { useParams, useNavigate } from 'react-router-dom';
import { ExecutionResultDialog } from './ExecutionResultDialog';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import type { WorkflowDetail, ExecuteWorkflowResponse } from '@/components/types/workflow';
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion';
import { Play, Trash2, ArrowLeft, CalendarClock, ListChecks, BellRing, ClockIcon, Copy } from 'lucide-react';
import { AlertDialog, AlertDialogContent, AlertDialogHeader, AlertDialogTitle, AlertDialogDescription, AlertDialogFooter, AlertDialogCancel, AlertDialogAction } from '@/components/ui/alert-dialog';


export function WorkflowDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [workflow, setWorkflow] = useState<WorkflowDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [executionResult, setExecutionResult] = useState<ExecuteWorkflowResponse | null>(null);
  const [showResultDialog, setShowResultDialog] = useState(false);
  const [executing, setExecuting] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [duplicating, setDuplicating] = useState(false);


  useEffect(() => {
    if (id) loadWorkflow(id);
  }, [id]);

  const loadWorkflow = async (workflowId: string) => {
    try {
      setLoading(true);
      const data = await workflowApi.getById(workflowId);
      setWorkflow(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load workflow');
    } finally {
      setLoading(false);
    }
  };

  const handleExecute = async () => {
    if (!id) return;
    setError('');
    setExecuting(true);
    try {
      const result = await workflowApi.execute(id);
      setExecutionResult(result);
      setShowResultDialog(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to execute workflow');
    } finally {
      setExecuting(false);
    }
  };

  const handleViewExecutionDetails = () => {
    if (executionResult) {
      setShowResultDialog(false);
      navigate(`/executions/${executionResult.executionId}`);
    }
  };

  const handleDelete = () => {
    setShowDeleteDialog(true);
  };

  const handleDuplicate = async () => {
    if (!id) return;

    setDuplicating(true);
    try {
      const result = await workflowApi.duplicate(id);
      toast.success(`Workflow duplicated: ${result.name}`, { position: 'top-center' });
      // Navigate to the newly created workflow
      navigate(`/workflows/${result.id}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to duplicate workflow', { position: 'top-center' });
    } finally {
      setDuplicating(false);
    }
  };

  const confirmDelete = async () => {
    if (!id) return;

    setDeleting(true);
    try {
      await workflowApi.delete(id);
      toast.success('Workflow deleted successfully', { position: 'top-center' });
      navigate('/workflows');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete workflow', { position: 'top-center' });
    } finally {
      setDeleting(false);
      setShowDeleteDialog(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-muted-foreground">Loading workflow...</p>
      </div>
    );
  }

  if (error || !workflow) {
    return (
      <div className="flex flex-col items-center justify-center h-64 gap-4">
        <p className="text-red-500">{error || 'Workflow not found'}</p>
        <Button onClick={() => navigate('/workflows')} variant="outline">
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Workflows
        </Button>
      </div>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
    <div className="flex flex-col gap-6 p-6">
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4 md:gap-0">
        <div className="flex items-center gap-4">
          <Button
            variant="ghost"
            size="icon"
            onClick={() => navigate('/workflows')}
            aria-label="Back to workflows"
            className="border border-input md:border-0"
          >
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <h1 className="text-2xl font-semibold">{workflow.name}</h1>
            <p className="text-sm text-muted-foreground">{workflow.description || 'No description'}</p>
          </div>
        </div>

        <div className="flex gap-2">
          <Button onClick={handleExecute} disabled={executing} aria-label="Execute workflow" aria-busy={executing}>
            <Play className="h-4 w-4 mr-2" />
            {executing ? 'Executing...' : 'Execute'}
          </Button>
          <Button
            variant="outline"
            onClick={handleDuplicate}
            disabled={duplicating}
            aria-label="Duplicate workflow"
            aria-busy={duplicating}
          >
            <Copy className="h-4 w-4 mr-2" />
            {duplicating ? 'Duplicating...' : 'Duplicate'}
          </Button>
          <Button
            variant="destructive"
            onClick={handleDelete}
            disabled={deleting}
            aria-label="Delete workflow"
          >
            <Trash2 className="h-4 w-4 mr-2" />
            Delete
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card className="p-3">
          <CardContent className="p-0 flex items-center gap-3">
            <div className="rounded-md border bg-muted/30 p-2">
              <BellRing className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="min-w-0">
              <p className="text-xs uppercase tracking-wide text-muted-foreground">Trigger Type</p>
              <Badge variant="outline" className="mt-1 capitalize">{workflow.triggerType}</Badge>
            </div>
          </CardContent>
        </Card>
        <Card className="p-3">
          <CardContent className="p-0 flex items-center gap-3">
            <div className="rounded-md border bg-muted/30 p-2">
              <ListChecks className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="min-w-0">
              <p className="text-xs uppercase tracking-wide text-muted-foreground">Total Tasks</p>
              <p className="text-lg font-semibold mt-0.5">{workflow.tasks.length}</p>
            </div>
          </CardContent>
        </Card>
        <Card className="p-3">
          <CardContent className="p-0 flex items-center gap-3">
            <div className="rounded-md border bg-muted/30 p-2">
              <CalendarClock className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="min-w-0">
              <p className="text-xs uppercase tracking-wide text-muted-foreground">Created At</p>
              <p className="text-sm mt-0.5 truncate">{formatDateTime(workflow.createdAt)}</p>
            </div>
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="tasks" className="w-full">
        <TabsList className="grid w-full grid-cols-2 md:w-fit md:max-w-md">
          <TabsTrigger value="tasks">Tasks</TabsTrigger>
          <TabsTrigger value="graph">Dependency Graph</TabsTrigger>
        </TabsList>

        <TabsContent value="tasks" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Task List</CardTitle>
            </CardHeader>
            <CardContent>
              {workflow.tasks.length === 0 ? (
                <p className="text-center text-muted-foreground py-8">No tasks defined</p>
              ) : (
                <Accordion type="single" collapsible className="w-full">
                  {workflow.tasks.map((task, idx) => (
                    <AccordionItem key={task.id} value={task.id} className="rounded-xl border bg-card/80 shadow-sm px-4 mb-3">
                      <AccordionTrigger className="py-4 hover:no-underline">
                        <div className="flex flex-col sm:flex-row w-full items-start sm:items-center justify-between gap-3 pr-3 min-w-0">
                          <div className="flex items-center gap-3 min-w-0">
                            <span className="text-xs text-muted-foreground rounded-md border bg-muted/40 px-2 py-1">#{idx + 1}</span>
                            <span className="font-medium break-words md:truncate">{task.name}</span>
                          </div>
                          <div className="flex items-center gap-2 shrink-0">
                            <Badge variant="secondary" className="capitalize">{task.agentType}</Badge>
                            {task.dependencies.length > 0 && (
                              <Badge variant="outline" className="text-xs">
                                {task.dependencies.length} dep{task.dependencies.length > 1 ? 's' : ''}
                              </Badge>
                            )}
                          </div>
                        </div>
                      </AccordionTrigger>
                      <AccordionContent className="pb-4">
                        <div className="space-y-4 pl-0">
                          <div>
                            <p className="text-sm font-medium">Agent Type</p>
                            <p className="text-sm text-muted-foreground capitalize">{task.agentType}</p>
                          </div>
                          {task.prompt && (
                            <div>
                              <p className="text-sm font-medium">Task Prompt</p>
                              <div className="p-3 bg-muted/40 border rounded-md mt-1">
                                <pre className="text-xs whitespace-pre-wrap leading-relaxed">{task.prompt}</pre>
                              </div>
                            </div>
                          )}
                          {task.dependencies.length > 0 && (
                            <div>
                              <p className="text-sm font-medium">Dependencies</p>
                              <div className="flex flex-wrap gap-2 mt-1">
                                {task.dependencies.map((depId) => {
                                  const depTask = workflow.tasks.find(t => t.id === depId);
                                  return (
                                    <Badge key={depId} variant="outline">
                                      {depTask?.name || depId}
                                    </Badge>
                                  );
                                })}
                              </div>
                            </div>
                          )}
                          {Object.keys(task.config).length > 0 && (
                            <div>
                              <p className="text-sm font-medium">Configuration</p>
                              <pre className="text-xs bg-muted p-2 rounded mt-1 overflow-x-auto">
                                {JSON.stringify(task.config, null, 2)}
                              </pre>
                            </div>
                          )}
                        </div>
                      </AccordionContent>
                    </AccordionItem>
                  ))}
                </Accordion>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="graph">
          <Card>
            <CardHeader>
              <CardTitle>Dependency Graph (DAG)</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex flex-wrap gap-4 pt-0 pl-0 pr-4 pb-4">
                {workflow.tasks.map((task) => (
                  <Card
                    key={task.id}
                    className="w-[220px] border-2 border-green-500 bg-card/80 shadow-sm transition-all hover:-translate-y-0.5 hover:shadow-md"
                  >
                    <CardHeader className="p-4 pb-2">
                      <div className="space-y-3">
                        <p className="w-full font-semibold text-sm leading-tight break-words">{task.name}</p>
                        <Badge variant="secondary" className="text-xs capitalize w-fit">
                          {task.agentType}
                        </Badge>
                        <Badge variant="outline" className="w-fit text-xs flex items-center gap-1">
                          <ClockIcon className="h-3.5 w-3.5 text-muted-foreground" />
                          <span>Pending</span>
                        </Badge>
                      </div>
                    </CardHeader>
                    <CardContent className="p-4 pt-0">
                      <div className="text-sm text-muted-foreground space-y-1.5">
                        <p><span className="font-medium text-foreground">Task ID:</span> {task.id}</p>
                        <p><span className="font-medium text-foreground">Dependencies:</span> {task.dependencies.length}</p>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      <ExecutionResultDialog
        open={showResultDialog}
        onOpenChange={setShowResultDialog}
        result={executionResult}
        onViewDetails={handleViewExecutionDetails}
      />

      <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <AlertDialogContent className="p-0 w-[90vw] max-w-[90vw] mx-auto rounded-lg md:w-auto md:max-w-lg">
          <AlertDialogHeader className="pt-4 pl-4 pr-4">
            <AlertDialogTitle>Delete workflow?</AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone. This will permanently delete the workflow and all its configurations.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter className="bg-muted/50 rounded-b-xl border-t pt-3 pl-3 pr-3 pb-3">
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={confirmDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
    </ScrollArea>
  );
}