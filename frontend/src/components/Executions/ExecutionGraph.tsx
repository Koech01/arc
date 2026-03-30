import { Badge } from '@/components/ui/badge';
import type { Task } from '@/components/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle2Icon, ClockIcon, LoaderIcon, MinusCircleIcon, XCircleIcon } from 'lucide-react';


interface ExecutionGraphProps {
  tasks: Task[];
  onTaskClick: (task: Task) => void;
}

export function ExecutionGraph({ tasks, onTaskClick }: ExecutionGraphProps) {
  const statusBorder = {
    success: 'border-green-500',
    failed: 'border-red-500',
    running: 'border-blue-500',
    queued: 'border-slate-400',
    skipped: 'border-slate-300',
  };

  const statusVariant = {
    success: 'default' as const,
    failed: 'destructive' as const,
    running: 'secondary' as const,
    queued: 'outline' as const,
    skipped: 'outline' as const,
  };

  const statusIcon = {
    success: <CheckCircle2Icon className="h-3.5 w-3.5 text-green-500" />,
    failed: <XCircleIcon className="h-3.5 w-3.5 text-red-500" />,
    running: <LoaderIcon className="h-3.5 w-3.5 text-blue-500" />,
    queued: <ClockIcon className="h-3.5 w-3.5 text-gray-500" />,
    skipped: <MinusCircleIcon className="h-3.5 w-3.5 text-gray-400" />,
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Execution Graph (DAG)</CardTitle>
      </CardHeader>
      <CardContent> 
        <div className="flex flex-wrap gap-4 pt-0 pl-0 pr-4 pb-4">
          {tasks.map((task) => (
            <Card
              key={task.id}
              className={`w-[220px] cursor-pointer border-2 ${statusBorder[task.status]} bg-card/80 hover:shadow-md transition-all hover:-translate-y-0.5`}
              onClick={() => onTaskClick(task)}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  onTaskClick(task);
                }
              }}
              aria-label={`Task: ${task.name}`}
            >
              <CardHeader className="p-4 pb-2">
                <div className="space-y-3">
                  <p className="w-full font-semibold text-sm leading-tight break-words">{task.name}</p>
                  <Badge variant={statusVariant[task.status]} className="text-xs capitalize w-fit flex items-center gap-1">
                    {statusIcon[task.status]}
                    <span>{task.status}</span>
                  </Badge>
                </div>
              </CardHeader>
              <CardContent className="p-4 pt-0">
                <div className="text-sm text-muted-foreground space-y-1.5">
                  <p><span className="font-medium text-foreground">Agent:</span> {task.agentType}</p>
                  <p><span className="font-medium text-foreground">Duration:</span> {Math.floor(task.duration / 1000)}s</p>
                  {task.dependencies.length > 0 && (
                    <p><span className="font-medium text-foreground">Dependencies:</span> {task.dependencies.length}</p>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div> 
      </CardContent>
    </Card>
  );
}