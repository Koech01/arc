import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { CheckCircle2, XCircle, Loader2, Clock } from 'lucide-react';
import type { ExecuteWorkflowResponse } from '@/components/types/workflow';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog';


interface ExecutionResultDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  result: ExecuteWorkflowResponse | null;
  onViewDetails: () => void;
}

export function ExecutionResultDialog({ open, onOpenChange, result, onViewDetails }: ExecutionResultDialogProps) {
  if (!result) return null;

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Succeeded':
        return <CheckCircle2 className="h-4 w-4 text-green-500" />;
      case 'Failed':
        return <XCircle className="h-4 w-4 text-red-500" />;
      case 'Running':
        return <Loader2 className="h-4 w-4 text-blue-500 animate-spin" />;
      default:
        return <Clock className="h-4 w-4 text-gray-500" />;
    }
  };

  const getStatusVariant = (status: string): 'default' | 'secondary' | 'destructive' | 'outline' => {
    switch (status) {
      case 'Succeeded':
        return 'default';
      case 'Failed':
        return 'destructive';
      case 'Running':
        return 'secondary';
      default:
        return 'outline';
    }
  };


  const formatExecutionId = (id: string): string => {
    if (id.length <= 16) return id;
    return `${id.slice(0, 8)}…${id.slice(-6)}`;
  };

  
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="p-3 w-[90vw] max-w-[90vw] rounded-2xl md:w-auto md:max-w-3xl max-h-[80vh] overflow-y-auto" aria-describedby="execution-result-description">
        <DialogHeader className="text-left md:text-left">
          <DialogTitle>Workflow Execution Started</DialogTitle>
          <DialogDescription id="execution-result-description">
            Execution details for {result.workflowName}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="min-w-0">
              <p className="text-sm font-medium text-muted-foreground">Execution ID</p>
              <p className="text-sm mt-1 truncate" title={result.executionId}>
                {formatExecutionId(result.executionId)}
              </p>
            </div>

            <div>
              <p className="text-sm font-medium text-muted-foreground">Workflow Name</p>
              <p className="text-sm mt-1">{result.workflowName}</p>
            </div>
          </div>

          <div>
            <h3 className="text-sm font-semibold mb-3">Task Execution Status</h3>
            <div className="border rounded-lg overflow-hidden">
              <Table>
                <TableHeader className="bg-muted">
                  <TableRow>
                    <TableHead className="w-16">Order</TableHead>
                    <TableHead>Task Name</TableHead>
                    <TableHead className="w-32">Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {result.tasks.map((task) => (
                    <TableRow key={task.taskId}>
                      <TableCell className="text-sm">
                        #{task.executionOrder}
                      </TableCell>
                      <TableCell className="font-medium">{task.taskName}</TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          {getStatusIcon(task.status)}
                          <Badge variant={getStatusVariant(task.status)}>
                            {task.status}
                          </Badge>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </div>

          {result.tasks.some(t => t.output) && (
            <div>
              <h3 className="text-sm font-semibold mb-3">Task Outputs</h3>
              <div className="space-y-3">
                {result.tasks.filter(t => t.output).map((task) => (
                  <div key={task.taskId} className="border rounded-lg p-3">
                    <div className="flex items-center gap-2 mb-2">
                      <span className="text-xs text-muted-foreground">#{task.executionOrder}</span>
                      <span className="font-medium text-sm">{task.taskName}</span>
                    </div>
                    <ScrollArea className="max-h-32">
                      <pre className="text-xs bg-muted p-2 rounded whitespace-pre-wrap break-words">
                        {task.output}
                      </pre>
                    </ScrollArea>
                  </div>
                ))}
              </div>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-4">
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              Close
            </Button>
            <Button onClick={onViewDetails}>
              View Execution Details
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}