import { Copy } from 'lucide-react';
import { Button } from '@/components/ui/button';
import type { ExecutionMetadata } from '@/components/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';


interface MetadataSectionProps {
  metadata: ExecutionMetadata;
}


const formatExecutionId = (id: string): string => {
  if (id.length <= 16) return id;
  return `${id.slice(0, 12)}…${id.slice(-10)}`;
};


export function MetadataSection({ metadata }: MetadataSectionProps) {
  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
  };

  return (
    <Card className="pb-4">
      <CardHeader className="pl-4">
        <CardTitle>Metadata</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
          <div className="space-y-1">
            <p className="text-muted-foreground">Execution ID</p>
            <div className="flex items-center gap-2">
              <p>{formatExecutionId(metadata.executionId)}</p>
              <Button
                variant="ghost"
                size="icon"
                className="h-6 w-6"
                onClick={() => copyToClipboard(metadata.executionId)}
                aria-label="Copy execution ID"
              >
                <Copy className="h-3 w-3" />
              </Button>
            </div>
          </div>
          <div className="space-y-1">
            <p className="text-muted-foreground">Workflow Version</p>
            <p>{metadata.workflowVersion}</p>
          </div>
          <div className="space-y-1">
            <p className="text-muted-foreground">Triggered By</p>
            <p>{metadata.triggeredBy}</p>
          </div>
          <div className="space-y-1">
            <p className="text-muted-foreground">Environment</p>
            <p>{metadata.environment}</p>
          </div>
          <div className="space-y-1">
            <p className="text-muted-foreground">Total Tasks</p>
            <p>{metadata.totalTasks}</p>
          </div>
          <div className="space-y-1">
            <p className="text-muted-foreground">Successful Tasks</p>
            <p className="text-green-600">{metadata.successfulTasks}</p>
          </div>
          <div className="space-y-1">
            <p className="text-muted-foreground">Failed Tasks</p>
            <p className="text-red-600">{metadata.failedTasks}</p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}