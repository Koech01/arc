import { toast } from 'sonner';
import { exportImportApi } from '@/lib/api';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useState, useCallback } from 'react';
import { Button } from '@/components/ui/button';
import type { ExportRequest, ExecutionExportItem } from '@/components/types/export-import';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';


interface ExportFormProps {
  onExportComplete?: (results: ExecutionExportItem[]) => void;
  onError?: (error: Error) => void;
}

export function ExportForm({ onExportComplete, onError }: ExportFormProps) {
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState<'Succeeded' | 'Failed' | 'all'>('all');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  const validateForm = useCallback((): boolean => {
    if (startDate && endDate) {
      const start = new Date(startDate);
      const end = new Date(endDate);
      if (start > end) {
        toast.error('Start date must be before end date', { position: 'top-center' });
        return false;
      }
    }
    return true;
  }, [startDate, endDate]);

  const handleExport = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setIsLoading(true);
    try {
      const request: ExportRequest = {
        status: status === 'all' ? null : status,
        startDate: startDate ? new Date(startDate).toISOString() : null,
        endDate: endDate ? new Date(endDate).toISOString() : null,
        minTaskCount: null,
        maxTaskCount: null,
        minExecutionTimeMs: null,
        maxExecutionTimeMs: null,
        limit: 100,
        offset: 0,
        format: 'json',
      };

      const response = await exportImportApi.export(request);
      onExportComplete?.(response.exports);
      toast.success('Export completed successfully', { position: 'top-center' });
    } catch (error) {
      const err = error instanceof Error ? error : new Error('Export failed');
      onError?.(err);
      toast.error(err.message, { position: 'top-center' });
    } finally {
      setIsLoading(false);
    }
  }, [status, startDate, endDate, validateForm, onExportComplete, onError]);

  return (
    <form onSubmit={handleExport} className="space-y-6">
      <div className="space-y-4">
        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="status">Execution Status</Label>
            <Select value={status} onValueChange={(value) => setStatus(value as 'Succeeded' | 'Failed' | 'all')}>
              <SelectTrigger id="status" className="w-[100%] md:max-w-[200px]">
                <SelectValue placeholder="All Statuses" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Statuses</SelectItem>
                <SelectItem value="Succeeded">Succeeded</SelectItem>
                <SelectItem value="Failed">Failed</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>

        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="startDate">Start Date</Label>
            <Input
              id="startDate"
              type="date"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              disabled={isLoading}
              aria-required="false"
              className="w-[92%] md:max-w-[150px]"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="endDate">End Date</Label>
            <Input
              id="endDate"
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              disabled={isLoading}
              aria-required="false"
              className="w-[92%] md:max-w-[150px]"
            />
          </div>
        </div>
      </div>

      <Button
        type="submit"
        disabled={isLoading}
        aria-busy={isLoading}
        className="w-fit"
      >
        {isLoading ? 'Exporting...' : 'Export Executions'}
      </Button>
    </form>
  );
}