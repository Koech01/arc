import { toast } from 'sonner';
import { executionApi } from '@/lib/api';
import { useState, useEffect } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { useParams, useNavigate } from 'react-router-dom';
import type { Execution, Task } from '@/components/types';
import { ReplayExecutionSkeleton } from './ReplayExecutionSkeleton';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { ArrowLeft, Play, CheckCircle2Icon, XCircleIcon, LoaderIcon, AlertCircle } from 'lucide-react';


export function ReplayExecution() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [execution, setExecution] = useState<Execution | null>(null);
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(true);
  const [replaying, setReplaying] = useState(false);

  const formatExecutionId = (id: string): string => {
    if (id.length <= 16) return id;
    return `${id.slice(0, 8)}…${id.slice(-6)}`;
  };

  const canReplay = !!execution?.workflowId;

  useEffect(() => {
    if (id) {
      Promise.all([
        executionApi.getExecution(id),
        executionApi.getTasks(id)
      ]).then(([exec, taskList]) => {
        setExecution(exec);
        setTasks(taskList);
      }).catch((err) => {
        toast.error(err instanceof Error ? err.message : 'Failed to load execution', { position: 'top-center' });
      }).finally(() => setLoading(false));
    }
  }, [id]);

  const handleReplay = async () => {
    setReplaying(true);
    try {
      const { executionId } = await executionApi.replay(id!);
      toast.success('Execution replayed successfully', { position: 'top-center' });
      navigate(`/executions/${executionId}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to replay', { position: 'top-center' });
    } finally {
      setReplaying(false);
    }
  };

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <ReplayExecutionSkeleton />
      </ScrollArea>
    );
  }

  if (!execution) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <div className="flex flex-col items-center justify-center h-64 gap-4">
          <p className="text-red-500">Execution not found</p>
          <Button onClick={() => navigate('/executions')} variant="outline">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Executions
          </Button>
        </div>
      </ScrollArea>
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
              onClick={() => navigate('/executions')}
              aria-label="Back to executions"
              className="border border-input md:border-0"
            >
              <ArrowLeft className="h-4 w-4" />
            </Button>
            <div>
              <h1 className="text-2xl font-semibold">Replay Execution</h1>
              <p className="text-sm text-muted-foreground">Review and replay this execution</p>
            </div>
          </div>

          <Button 
            onClick={handleReplay} 
            disabled={replaying || !canReplay}
            aria-label="Replay execution"
            aria-busy={replaying}
          >
            <Play className="h-4 w-4 mr-2" />
            {replaying ? 'Replaying...' : 'Replay Now'}
          </Button>
        </div>

        {!canReplay && (
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertTitle>Cannot Replay Execution</AlertTitle>
            <AlertDescription className="space-y-2">
              <p>
                This execution cannot be replayed because it wasn't created from a workflow.
              </p>
              <p className="text-sm">
                <strong>Why this happened:</strong> Only executions created by running workflows can be replayed. 
                Ad-hoc executions (created via the "Execute" endpoint) don't store the workflow definition needed for replay.
              </p>
              <div className="text-sm space-y-1">
                <p><strong>How to fix this:</strong></p>
                <ol className="list-decimal list-inside space-y-1 ml-2">
                  <li>Create a workflow from your tasks</li>
                  <li>Execute that workflow (not the ad-hoc execute endpoint)</li>
                  <li>The resulting execution will be replayable</li>
                </ol>
              </div>
              <p className="text-sm">
                <strong>To create replayable executions:</strong><br />
                • Use: <code className="bg-muted px-1 rounded">POST /api/workflows/&#123;workflowId&#125;/execute</code><br />
                • Avoid: <code className="bg-muted px-1 rounded">POST /api/execute</code> (ad-hoc, not replayable)
              </p>
            </AlertDescription>
          </Alert>
        )}

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <Card className="p-3 flex flex-col gap-3">
            <CardHeader className="p-0">
              <CardTitle className="text-sm font-medium">Execution ID</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <p className="text-sm break-all">{formatExecutionId(execution.id)}</p>
            </CardContent>
          </Card>

          <Card className="p-3 flex flex-col gap-3">
            <CardHeader className="p-0">
              <CardTitle className="text-sm font-medium">Execution Type</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              {execution.workflowId ? (
                <Badge variant="default" className="w-fit">
                  Workflow
                </Badge>
              ) : (
                <Badge variant="secondary" className="w-fit">
                  Ad-hoc
                </Badge>
              )}
            </CardContent>
          </Card>

          <Card className="p-3 flex flex-col gap-3">
            <CardHeader className="p-0">
              <CardTitle className="text-sm font-medium">Status</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <Badge variant="outline" className="flex w-fit gap-1 px-2">
                {execution.status === 'success' && <CheckCircle2Icon className="h-3 w-3 text-green-500" />}
                {execution.status === 'failed' && <XCircleIcon className="h-3 w-3 text-red-500" />}
                {execution.status === 'running' && <LoaderIcon className="h-3 w-3 text-blue-500" />}
                {execution.status}
              </Badge>
            </CardContent>
          </Card>

          <Card className="p-3 flex flex-col gap-3">
            <CardHeader className="p-0">
              <CardTitle className="text-sm font-medium">Total Tasks</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <p className="text-xl font-bold">{tasks.length}</p>
            </CardContent>
          </Card>

          <Card className="p-3 flex flex-col gap-3">
            <CardHeader className="p-0">
              <CardTitle className="text-sm font-medium">Duration</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <p className="text-sm">{execution.duration}ms</p>
            </CardContent>
          </Card>
        </div>

        {tasks.length > 0 && (
          <Card className="pb-3">
            <CardHeader>
              <CardTitle>Tasks to Replay</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="overflow-hidden rounded-lg border">
                <Table>
                  <TableHeader className="bg-muted">
                    <TableRow>
                      <TableHead className="w-16">#</TableHead>
                      <TableHead>Task Name</TableHead>
                      <TableHead className="hidden md:table-cell">Agent Type</TableHead>
                      <TableHead className="hidden lg:table-cell">Status</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {tasks.map((task, idx) => (
                      <TableRow key={task.id} className="hover:bg-muted/50">
                        <TableCell className="text-sm text-muted-foreground">
                          {idx + 1}
                        </TableCell>
                        <TableCell className="font-medium">{task.name}</TableCell>
                        <TableCell className="hidden md:table-cell">
                          <Badge variant="secondary">{task.agentType}</Badge>
                        </TableCell>
                        <TableCell className="hidden lg:table-cell">
                          <Badge variant="outline" className="flex w-fit gap-1 px-2">
                            {task.status === 'success' && <CheckCircle2Icon className="h-3 w-3 text-green-500" />}
                            {task.status === 'failed' && <XCircleIcon className="h-3 w-3 text-red-500" />}
                            {task.status === 'running' && <LoaderIcon className="h-3 w-3 text-blue-500" />}
                            {task.status}
                          </Badge>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </ScrollArea>
  );
}