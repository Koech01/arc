import { toast } from 'sonner';
import { useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { formatSmartDateTime } from '@/lib/dateFormat';
import type { ExecutionExportItem } from '@/components/types/export-import';
import { CheckCircle2Icon, CopyIcon, DownloadIcon, ViewIcon, XCircleIcon } from 'lucide-react';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';


interface ExportResultsProps {
  exports: ExecutionExportItem[];
}

function formatExecutionId(id: string): string {
  if (id.length <= 16) return id;
  return `${id.slice(0, 8)}…${id.slice(-6)}`;
}

export function ExportResults({ exports }: ExportResultsProps) {
  const [previewId, setPreviewId] = useState<string | null>(null);

  const downloadJson = (executionId: string, jsonPayload: string) => {
    try {
      const blob = new Blob([jsonPayload], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `execution-${executionId}.json`;
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Failed to download export';
      toast.error(message, { position: 'top-center' });
    }
  };

  const copyToClipboard = async (jsonPayload: string) => {
    try {
      await navigator.clipboard.writeText(jsonPayload);
      toast.success('JSON copied to clipboard', { position: 'top-center' });
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Failed to copy JSON';
      toast.error(message, { position: 'top-center' });
    }
  };

  if (exports.length === 0) {
    return (
      <div className="text-sm text-muted-foreground">
        No exports returned.
      </div>
    );
  }

  const activePreview = previewId
    ? exports.find((x) => x.executionId === previewId)
    : undefined;

  return (
    <div className="space-y-4">
      <p className="text-muted-foreground text-sm">Export Results ({exports.length}).</p>

      <div className="overflow-hidden rounded-lg border">
        <Table>
          <TableHeader className="bg-muted">
            <TableRow>
              <TableHead>Execution ID</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Tasks</TableHead>
              <TableHead>Created</TableHead>
              <TableHead className="w-[260px]">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {exports.map((e) => (
              <TableRow key={e.executionId}>
                <TableCell className="text-sm">
                  <span title={e.executionId} aria-label={e.executionId}>
                    {formatExecutionId(e.executionId)}
                  </span>
                </TableCell>
                <TableCell>
                  <Badge
                    variant="outline"
                    className="flex w-fit gap-1 px-2 text-muted-foreground [&_svg]:size-3"
                  >
                    {e.status === 'Succeeded' && (
                      <CheckCircle2Icon className="text-green-500 dark:text-green-400" />
                    )}
                    {e.status === 'Failed' && (
                      <XCircleIcon className="text-red-500 dark:text-red-400" />
                    )}
                    {e.status}
                  </Badge>
                </TableCell>
                <TableCell>
                  {e.completedTasks}/{e.totalTasks}
                </TableCell>
                <TableCell className="text-sm">
                  {formatSmartDateTime(e.createdAtUtc)}
                </TableCell>
                <TableCell>
                  <div className="flex flex-col items-start gap-2">
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() =>
                        downloadJson(e.executionId, e.jsonPayload)
                      }
                      aria-label={`Download export for execution ${e.executionId}`}
                    >
                      <DownloadIcon className="mr-1 h-4 w-4" />
                      Download
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => copyToClipboard(e.jsonPayload)}
                      aria-label={`Copy JSON for execution ${e.executionId}`}
                    >
                      <CopyIcon className="mr-1 h-4 w-4" />
                      Copy
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() =>
                        setPreviewId(
                          previewId === e.executionId ? null : e.executionId,
                        )
                      }
                      aria-pressed={previewId === e.executionId}
                      aria-label={`${
                        previewId === e.executionId ? 'Hide' : 'Show'
                      } JSON preview for execution ${e.executionId}`}
                    >
                      <ViewIcon className="mr-1 h-4 w-4" />
                      {previewId === e.executionId ? 'Hide' : 'Preview'}
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {activePreview && (
        <pre
          className="max-h-[320px] overflow-auto rounded-lg border bg-muted/40 p-4 text-xs"
          aria-live="polite"
        >
          {JSON.stringify(JSON.parse(activePreview.jsonPayload), null, 2)}
        </pre>
      )}
    </div>
  );
}