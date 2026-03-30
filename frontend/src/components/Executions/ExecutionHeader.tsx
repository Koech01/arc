import { toast } from 'sonner';
import { useState } from 'react';
import { executionApi } from '@/lib/api';
import { formatDateTime } from '@/lib/date';
import { Badge } from '@/components/ui/badge';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { CompareDialog } from './CompareDialog';
import type { Execution } from '@/components/types';
import { Separator } from '@/components/ui/separator';
import { Card, CardHeader } from '@/components/ui/card';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { MoreVertical, Play, GitCompare, Download, Archive, CalendarClock, Timer, BellRing, Fingerprint } from 'lucide-react';


interface ExecutionHeaderProps {
  execution: Execution;
}

export function ExecutionHeader({ execution }: ExecutionHeaderProps) {
  const navigate = useNavigate();
  const [compareOpen, setCompareOpen] = useState(false);
  
  const handleArchive = async () => {
    try {
      await executionApi.archive(execution.id);
      toast.success('Execution archived', { position: 'top-center' });
      // Add timestamp to force refresh of executions list
      navigate('/executions?refresh=' + Date.now());
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to archive execution', { position: 'top-center' });
    }
  };

  const handleExportJSON = async () => {
    try {
      // Fetch full execution details including tasks, logs, outputs, metadata
      const [tasks, logs, outputs, metadata] = await Promise.all([
        executionApi.getTasks(execution.id),
        executionApi.getLogs(execution.id),
        executionApi.getOutputs(execution.id),
        executionApi.getMetadata(execution.id).catch(() => null),
      ]);

      const exportData = {
        execution,
        tasks,
        logs,
        outputs,
        metadata,
        exportedAt: new Date().toISOString(),
      };

      const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `execution-${execution.id.slice(0, 8)}-${new Date().toISOString().split('T')[0]}.json`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      toast.success('Execution exported successfully', { position: 'top-center' });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to export execution', { position: 'top-center' });
    }
  };
  
  const statusVariant = {
    success: 'default' as const,
    failed: 'destructive' as const,
    running: 'secondary' as const,
    queued: 'outline' as const,
  };

  const formatDuration = (ms: number) => {
    const seconds = Math.floor(ms / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    if (hours > 0) return `${hours}h ${minutes % 60}m`;
    if (minutes > 0) return `${minutes}m ${seconds % 60}s`;
    return `${seconds}s`;
  };


  const formatExecutionId = (id: string, isMobile: boolean = false): string => {
    if (id.length <= 16) return id;
    if (isMobile) return `${id.slice(0, 8)}…${id.slice(-8)}`;
    return `${id.slice(0, 12)}…${id.slice(-10)}`;
  };


  return (
    <Card>
      <CardHeader>
        <div className="flex flex-col md:flex-row md:items-start justify-between gap-4">
          <div className="space-y-2 flex-1 min-w-0">
            <div className="flex flex-col lg:flex-row lg:items-center gap-3 w-full">
              <h1 className="text-2xl font-bold break-words lg:truncate lg:flex-1">{execution.workflowName}</h1>
              <div className="flex items-center justify-between lg:gap-3">
                <Badge variant={statusVariant[execution.status]} aria-label={`Status: ${execution.status}`} className="flex-shrink-0 w-fit">
                  {execution.status}
                </Badge>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" aria-label="More actions" className="flex-shrink-0">
                      <MoreVertical className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    {execution.workflowId ? (
                      <DropdownMenuItem className="sm:hidden" onClick={() => navigate(`/executions/${execution.id}/replay`)}>
                        <Play className="h-4 w-4 mr-2" />
                        Replay
                      </DropdownMenuItem>
                    ) : null}
                    <DropdownMenuItem className="sm:hidden" onClick={() => setCompareOpen(true)}>
                      <GitCompare className="h-4 w-4 mr-2" />
                      Compare
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={handleExportJSON}>
                      <Download className="h-4 w-4 mr-2" />
                      Export JSON
                    </DropdownMenuItem>
                    <DropdownMenuItem className="text-orange-600" onClick={handleArchive}>
                      <Archive className="h-4 w-4 mr-2" />
                      Archive
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>
            <div className="space-y-3">
              {execution.workflowDescription && (
                <p className="text-sm text-muted-foreground leading-relaxed">{execution.workflowDescription}</p>
              )}
              <div className="flex justify-start">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-x-4 gap-y-3 w-full md:w-4/5 text-sm text-muted-foreground">
                  <div className="flex w-full items-center gap-1 rounded-md border bg-muted/30 px-2 py-2">
                    <Fingerprint className="h-3.5 w-3.5 shrink-0" />
                    <span className="text-xs uppercase tracking-wide">ID</span>
                    <Separator orientation="vertical" className="h-3" />
                    <span>{formatExecutionId(execution.id, true)}</span>
                  </div>

                  <div className="flex w-full items-center gap-1 rounded-md border bg-muted/30 px-2 py-2">
                    <CalendarClock className="h-3.5 w-3.5 shrink-0" />
                    <span className="text-xs uppercase tracking-wide">Started</span>
                    <Separator orientation="vertical" className="h-3" />
                    <span className="text-sm whitespace-nowrap overflow-hidden">
                      {execution?.startedAt ? formatDateTime(execution.startedAt) : '-'}
                    </span>
                  </div>

                  <div className="flex w-full items-center gap-1 rounded-md border bg-muted/30 px-2 py-2">
                    <Timer className="h-3.5 w-3.5 shrink-0" />
                    <span className="text-xs uppercase tracking-wide">Duration</span>
                    <Separator orientation="vertical" className="h-3" />
                    <span className="truncate">{formatDuration(execution.duration)}</span>
                  </div>

                  <div className="flex w-full items-center gap-1 rounded-md border bg-muted/30 px-2 py-2">
                    <BellRing className="h-3.5 w-3.5 shrink-0" />
                    <span className="text-xs uppercase tracking-wide">Trigger</span>
                    <Separator orientation="vertical" className="h-3" />
                    <span className="capitalize truncate">{execution.triggerType}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div className="hidden sm:flex items-center gap-2 flex-shrink-0">
            {execution.workflowId ? (
              <Button onClick={() => navigate(`/executions/${execution.id}/replay`)} aria-label="Replay execution">
                <Play className="h-4 w-4 mr-2" />
                Replay
              </Button>
            ) : null}
            <Button variant="outline" onClick={() => setCompareOpen(true)} aria-label="Compare execution">
              <GitCompare className="h-4 w-4 mr-2" />
              Compare
            </Button>
          </div>
        </div>
      </CardHeader>
      <CompareDialog open={compareOpen} onOpenChange={setCompareOpen} currentExecutionId={execution.id} />
    </Card>
  );
}