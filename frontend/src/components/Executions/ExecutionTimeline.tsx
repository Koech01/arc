import { Badge } from '@/components/ui/badge';
import type { Task } from '@/components/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { CheckCircle2Icon, ClockIcon, LoaderIcon, MinusCircleIcon, XCircleIcon } from 'lucide-react';


interface ExecutionTimelineProps {
  tasks: Task[];
}

export function ExecutionTimeline({ tasks }: ExecutionTimelineProps) {
  if (tasks.length === 0) {
    return (
      <Card className="pb-2">
        <CardHeader>
          <CardTitle>Execution Timeline</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">No timeline data available.</p>
        </CardContent>
      </Card>
    );
  }

  const sortedTasks = [...tasks].sort((a, b) => 
    new Date(a.startedAt).getTime() - new Date(b.startedAt).getTime()
  );

  const minTime = Math.min(...sortedTasks.map(t => new Date(t.startedAt).getTime()));
  const maxTime = Math.max(...sortedTasks.map(t => 
    new Date(t.completedAt || t.startedAt).getTime()
  ));
  const totalDuration = maxTime - minTime;

  const getBarStyle = (task: Task) => {
    const start = new Date(task.startedAt).getTime() - minTime;
    const duration = Math.max(task.duration, 1);
    const safeTotal = Math.max(totalDuration, duration);
    const left = (start / safeTotal) * 100;
    const width = (duration / safeTotal) * 100;
    return { left: `${left}%`, width: `${Math.max(width, 1)}%` };
  };

  const statusColor = {
    success: 'bg-green-500',
    failed: 'bg-red-500',
    running: 'bg-blue-500',
    queued: 'bg-gray-400',
    skipped: 'bg-gray-300',
  };

  const statusIcon = {
    success: <CheckCircle2Icon className="h-3.5 w-3.5 text-green-500" />,
    failed: <XCircleIcon className="h-3.5 w-3.5 text-red-500" />,
    running: <LoaderIcon className="h-3.5 w-3.5 text-blue-500" />,
    queued: <ClockIcon className="h-3.5 w-3.5 text-gray-500" />,
    skipped: <MinusCircleIcon className="h-3.5 w-3.5 text-gray-400" />,
  };

  return (
    <Card className="pb-2">
      <CardHeader>
        <CardTitle>Execution Timeline</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {sortedTasks.map((task) => (
            <div key={task.id} className="rounded-md border bg-card p-3 space-y-2">
              <div className="flex items-center justify-between gap-3">
                <div className="flex items-center gap-2 min-w-0">
                  {statusIcon[task.status]}
                  <span className="font-medium text-sm truncate">{task.name}</span>
                </div>
                <Badge variant="outline" className="text-xs capitalize shrink-0">
                  {task.status}
                </Badge>
              </div>
              <div className="flex items-center justify-between text-xs text-muted-foreground">
                <span>{new Date(task.startedAt).toLocaleTimeString()}</span>
                <span>{Math.floor(task.duration / 1000)}s</span>
              </div>
              <div className="relative h-2 rounded-full bg-muted overflow-hidden">
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <div
                        className={`absolute h-full rounded-full ${statusColor[task.status]} transition-all`}
                        style={getBarStyle(task)}
                        role="progressbar"
                        aria-label={`${task.name} duration`}
                        aria-valuenow={task.duration}
                      />
                    </TooltipTrigger>
                    <TooltipContent>
                      <p className="text-xs">
                        Started: {new Date(task.startedAt).toLocaleTimeString()}
                        <br />
                        Duration: {Math.floor(task.duration / 1000)}s
                      </p>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              </div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}