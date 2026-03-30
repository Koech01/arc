import { toast } from 'sonner';
import { executionApi } from '@/lib/api';
import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { formatSmartDateTime } from '@/lib/dateFormat';
import { ScrollArea } from '@/components/ui/scroll-area';
import { useNavigate, useLocation } from 'react-router-dom';
import { ExecutionListSkeleton } from './ExecutionListSkeleton';
import { useState, useEffect, useMemo, useCallback } from 'react';
import type { Execution, ExecutionListItem } from '@/components/types';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useReactTable, getCoreRowModel, getPaginationRowModel, getFilteredRowModel, flexRender, type ColumnDef } from '@tanstack/react-table';
import { CheckCircle2Icon, LoaderIcon, XCircleIcon, ChevronLeftIcon, ChevronRightIcon, ChevronsLeftIcon, ChevronsRightIcon, FilterIcon, ArchiveIcon, ArchiveRestoreIcon } from 'lucide-react';


const mapDetailStatusToListStatus = (status: Execution['status']): ExecutionListItem['status'] => {
  if (status === 'success') return 'completed';
  if (status === 'failed') return 'failed';
  if (status === 'running') return 'running';
  return 'queued';
};

const formatDurationForList = (durationMs: number): string => {
  const totalSeconds = Math.max(0, Math.floor(durationMs / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  if (minutes > 0) return `${minutes}m ${seconds}s`;
  return `${seconds}s`;
};

const mapExecutionDetailToListItem = (execution: Execution): ExecutionListItem => ({
  id: execution.id,
  status: mapDetailStatusToListStatus(execution.status),
  totalTasks: execution.tasks.length,
  duration: formatDurationForList(execution.duration),
  startedAt: execution.startedAt,
  workflowName: execution.workflowName,
  workflowDescription: execution.workflowDescription,
  tasks: execution.tasks,
});


export function ExecutionListPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [executions, setExecutions] = useState<ExecutionListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [pagination, setPagination] = useState({ pageIndex: 0, pageSize: 10 });
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [searchId, setSearchId] = useState('');
  const [showArchived, setShowArchived] = useState(false);

  const loadExecutions = useCallback(async () => {
    try {
      setLoading(true);
      const data = await executionApi.getAll(showArchived);
      
      // Sort by startedAt descending (newest first) to ensure duplicates appear at top
      const sorted = (Array.isArray(data) ? data : []).sort((a, b) => 
        new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime()
      );
      setExecutions(sorted);
      // Reset to first page when data reloads
      setPagination(prev => ({ ...prev, pageIndex: 0 }));
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load executions', { position: 'top-center' });
      setExecutions([]);
    } finally {
      setLoading(false);
    }
  }, [showArchived]);

  useEffect(() => {
    loadExecutions();
  }, [loadExecutions, location.search]); // Re-fetch when query params change

  useEffect(() => {
    const hydrateImportedExecutions = async (importedExecutionIds: string[]) => {
      if (!importedExecutionIds.length) return;

      const uniqueIds = Array.from(new Set(importedExecutionIds));
      const results = await Promise.allSettled(uniqueIds.map((id) => executionApi.getExecution(id)));

      const hydrated = results
        .filter((result): result is PromiseFulfilledResult<Execution> => result.status === 'fulfilled')
        .map((result) => mapExecutionDetailToListItem(result.value));

      if (!hydrated.length) return;

      setExecutions((current) => {
        const currentById = new Map(current.map((execution) => [execution.id, execution]));
        hydrated.forEach((execution) => {
          currentById.set(execution.id, execution);
        });
        return Array.from(currentById.values()).sort((a, b) =>
          new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime()
        );
      });
    };

    const restoreRecentImportedExecutions = () => {
      const raw = sessionStorage.getItem('recentImportedExecutionIds');
      if (!raw) return;

      try {
        const ids = JSON.parse(raw) as string[];
        void hydrateImportedExecutions(Array.isArray(ids) ? ids : []);
      } catch {
        sessionStorage.removeItem('recentImportedExecutionIds');
        return;
      }

      sessionStorage.removeItem('recentImportedExecutionIds');
    };

    const onImported = (event: Event) => {
      const customEvent = event as CustomEvent<{ importedExecutionIds?: string[] }>;
      const importedExecutionIds = customEvent.detail?.importedExecutionIds ?? [];

      void loadExecutions().then(() => hydrateImportedExecutions(importedExecutionIds));
    };

    restoreRecentImportedExecutions();
    window.addEventListener('executions-imported', onImported);
    return () => {
      window.removeEventListener('executions-imported', onImported);
    };
  }, [loadExecutions]);

  const formatExecutionId = (id: string): string => {
    if (id.length <= 16) return id;
    return `${id.slice(0, 8)}…${id.slice(-6)}`;
  };

  const filteredExecutions = useMemo(() => {
    const hasArchiveStateInResponse = executions.some((execution) => typeof execution.isArchived === 'boolean');

    return executions.filter(exec => {
      const isArchived = exec.isArchived === true;
      const matchesArchiveState = hasArchiveStateInResponse
        ? (showArchived ? isArchived : !isArchived)
        : true;
      const matchesStatus = statusFilter === 'all' || exec.status === statusFilter;
      const matchesSearch = !searchId || exec.id.toLowerCase().includes(searchId.toLowerCase());
      return matchesArchiveState && matchesStatus && matchesSearch;
    });
  }, [executions, showArchived, statusFilter, searchId]);

  const analytics = useMemo(() => {
    const total = executions.length;
    const completed = executions.filter(e => e.status === 'completed').length;
    const failed = executions.filter(e => e.status === 'failed').length;
    const running = executions.filter(e => e.status === 'running').length;
    const successRate = total > 0 ? Math.round((completed / total) * 100) : 0;
    const avgTasks = total > 0 ? Math.round(executions.reduce((sum, e) => sum + e.totalTasks, 0) / total) : 0;
    return { total, completed, failed, running, successRate, avgTasks };
  }, [executions]);

  const handleArchive = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await executionApi.archive(id);
      toast.success('Execution archived', { position: 'top-center' });
      loadExecutions();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to archive execution';
      if (message.includes('401') || message.includes('Unauthorized')) {
        toast.error('Archive feature not yet available on backend', { position: 'top-center' });
      } else {
        toast.error(message, { position: 'top-center' });
      }
    }
  };

  const handleUnarchive = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await executionApi.unarchive(id);
      toast.success('Execution restored', { position: 'top-center' });
      loadExecutions();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to restore execution', { position: 'top-center' });
    }
  };

  const columns: ColumnDef<ExecutionListItem>[] = [
    {
      accessorKey: 'workflowName',
      header: 'Workflow',
      cell: ({ row }) => (
        <div className="min-w-0">
          <p className="font-medium truncate">{row.original.workflowName}</p>
        </div>
      ),
    },
    {
      accessorKey: 'id',
      header: () => <span className="hidden md:table-cell">ID</span>,
      cell: ({ row }) => (
        <span className="hidden md:table-cell text-sm">{formatExecutionId(row.original.id)}</span>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => (
        <Badge variant="outline" className="flex w-fit gap-1 px-2">
          {row.original.status === 'completed' && <CheckCircle2Icon className="h-3 w-3 text-green-500" />}
          {row.original.status === 'failed' && <XCircleIcon className="h-3 w-3 text-red-500" />}
          {row.original.status === 'running' && <LoaderIcon className="h-3 w-3 text-blue-500" />}
          {row.original.status}
        </Badge>
      ),
    },
    {
      accessorKey: 'totalTasks',
      header: () => <span className="hidden md:table-cell">Tasks</span>,
      cell: ({ row }) => <span className="hidden md:table-cell">{row.original.totalTasks}</span>,
    },
    {
      accessorKey: 'duration',
      header: () => <span className="hidden md:table-cell">Duration</span>,
      cell: ({ row }) => <span className="hidden md:table-cell text-sm">{row.original.duration}</span>,
    },
    {
      accessorKey: 'startedAt',
      header: () => <span className="hidden lg:table-cell">Started At</span>,
      cell: ({ row }) => <span className="hidden lg:table-cell text-sm">{formatSmartDateTime(row.original.startedAt)}</span>,
    },
    {
      id: 'actions',
      header: () => <span className="hidden md:table-cell">Actions</span>,
      cell: ({ row }) => (
        <Button
          variant="ghost"
          size="sm"
          className="hidden md:inline-flex"
          onClick={(e) => row.original.isArchived ? handleUnarchive(row.original.id, e) : handleArchive(row.original.id, e)}
        >
          {row.original.isArchived ? (
            <><ArchiveRestoreIcon className="h-4 w-4 mr-1" /> Restore</>
          ) : (
            <><ArchiveIcon className="h-4 w-4 mr-1" /> Archive</>
          )}
        </Button>
      ),
    },
  ];

  const table = useReactTable({
    data: filteredExecutions,
    columns,
    state: { pagination },
    onPaginationChange: setPagination,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
  });

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <ExecutionListSkeleton />
      </ScrollArea>
    );
  }


  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-6 p-6">
        <div>
          <h1 className="text-2xl font-semibold">Executions</h1>
          <p className="text-sm text-muted-foreground">View all workflow executions</p>
        </div>

        <div className="grid gap-1 grid-cols-1 md:grid-cols-4 *:text-center">
          <div className="rounded-lg border py-6 space-y-2">
            <div className="text-3xl font-bold">{analytics.total}</div>
            <p className="text-sm text-muted-foreground">Total</p>
          </div>
          <div className="rounded-lg border py-6 space-y-2">
            <div className="text-3xl font-bold text-green-600">{analytics.successRate}%</div>
            <p className="text-sm text-muted-foreground">Success Rate</p>
          </div>
          <div className="rounded-lg border py-6 space-y-2">
            <div className="text-3xl font-bold text-red-600">{analytics.failed}</div>
            <p className="text-sm text-muted-foreground">Failed</p>
          </div>
          <div className="rounded-lg border py-6 space-y-2">
            <div className="text-3xl font-bold">{analytics.avgTasks}</div>
            <p className="text-sm text-muted-foreground">Avg Tasks</p>
          </div>
        </div>

        <div className="flex flex-col md:flex-row gap-4 items-end mt-4">
          <div className="flex-1 w-full">
            <Label htmlFor="search-id">Search by ID</Label>
            <Input
              id="search-id"
              placeholder="Enter execution ID..."
              value={searchId}
              onChange={(e) => setSearchId(e.target.value)}
              aria-label="Search executions by ID"
            />
          </div>
          <div className="w-full md:w-48">
            <Label htmlFor="status-filter">Status</Label>
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger id="status-filter" aria-label="Filter by status">
                <FilterIcon className="h-4 w-4 mr-2" />
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="completed">Completed</SelectItem>
                <SelectItem value="failed">Failed</SelectItem>
                <SelectItem value="running">Running</SelectItem>
                <SelectItem value="queued">Queued</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <Button
            variant={showArchived ? 'default' : 'outline'}
            onClick={() => setShowArchived(!showArchived)}
            className="w-full md:w-auto"
          >
            {showArchived ? <ArchiveRestoreIcon className="h-4 w-4 mr-2" /> : <ArchiveIcon className="h-4 w-4 mr-2" />}
            {showArchived ? 'Show Active' : 'Show Archived'}
          </Button>
        </div>

        <div className="overflow-hidden rounded-lg border">
          <Table>
            <TableHeader className="bg-muted">
              {table.getHeaderGroups().map((headerGroup) => (
                <TableRow key={headerGroup.id}>
                  {headerGroup.headers.map((header) => (
                    <TableHead key={header.id}>
                      {header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}
                    </TableHead>
                  ))}
                </TableRow>
              ))}
            </TableHeader>
            <TableBody>
              {table.getRowModel().rows?.length ? (
                table.getRowModel().rows.map((row) => (
                  <TableRow
                    key={row.id}
                    className="cursor-pointer hover:bg-muted/50"
                    onClick={() => navigate(`/executions/${row.original.id}`)}
                  >
                    {row.getVisibleCells().map((cell) => (
                      <TableCell key={cell.id}>
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={columns.length} className="h-24 text-center">
                    <p className="text-muted-foreground">No executions found.</p>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>

        <div className="flex items-center justify-between px-4">
          <div className="hidden flex-1 text-sm text-muted-foreground lg:flex">
            {table.getFilteredRowModel().rows.length} row(s) total.
          </div>
          <div className="flex w-full items-center gap-8 lg:w-fit">
            <div className="hidden items-center gap-2 lg:flex">
              <Label htmlFor="rows-per-page" className="text-sm font-medium">Rows per page</Label>
              <Select
                value={`${table.getState().pagination.pageSize}`}
                onValueChange={(value) => table.setPageSize(Number(value))}
              >
                <SelectTrigger className="w-20" id="rows-per-page">
                  <SelectValue placeholder={table.getState().pagination.pageSize} />
                </SelectTrigger>
                <SelectContent side="top">
                  {[10, 20, 30, 40, 50].map((pageSize) => (
                    <SelectItem key={pageSize} value={`${pageSize}`}>{pageSize}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex w-fit items-center justify-center text-sm font-medium">
              Page {table.getState().pagination.pageIndex + 1} of {table.getPageCount()}
            </div>
            <div className="ml-auto flex items-center gap-2 lg:ml-0">
              <Button
                variant="outline"
                className="hidden h-8 w-8 p-0 lg:flex"
                onClick={() => table.setPageIndex(0)}
                disabled={!table.getCanPreviousPage()}
              >
                <span className="sr-only">Go to first page</span>
                <ChevronsLeftIcon />
              </Button>
              <Button
                variant="outline"
                className="size-8"
                size="icon"
                onClick={() => table.previousPage()}
                disabled={!table.getCanPreviousPage()}
              >
                <span className="sr-only">Go to previous page</span>
                <ChevronLeftIcon />
              </Button>
              <Button
                variant="outline"
                className="size-8"
                size="icon"
                onClick={() => table.nextPage()}
                disabled={!table.getCanNextPage()}
              >
                <span className="sr-only">Go to next page</span>
                <ChevronRightIcon />
              </Button>
              <Button
                variant="outline"
                className="hidden size-8 lg:flex"
                size="icon"
                onClick={() => table.setPageIndex(table.getPageCount() - 1)}
                disabled={!table.getCanNextPage()}
              >
                <span className="sr-only">Go to last page</span>
                <ChevronsRightIcon />
              </Button>
            </div>
          </div>
        </div>
      </div>
    </ScrollArea>
  );
}