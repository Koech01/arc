import { toast } from 'sonner';
import { adminApi } from '@/lib/api';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { useState, useEffect, useCallback } from 'react';
import { ScrollArea } from "@/components/ui/scroll-area";
import type { AdminExecutionRow } from '@/components/types/admin';
import { SystemExecutionsSkeleton } from './SystemExecutionsSkeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { ChevronLeftIcon, ChevronRightIcon, ChevronsLeftIcon, ChevronsRightIcon } from 'lucide-react';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useReactTable, getCoreRowModel, getPaginationRowModel, flexRender, type ColumnDef } from '@tanstack/react-table';


export default function SystemExecutions() {
  const navigate = useNavigate();
  const [executions, setExecutions] = useState<AdminExecutionRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState('all');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [pagination, setPagination] = useState({ pageIndex: 0, pageSize: 10 });

  const fetchExecutions = useCallback(async () => {
    setLoading(true);
    try {
      const data = await adminApi.getExecutions({
        status: statusFilter && statusFilter !== 'all' ? statusFilter : undefined,
        from: fromDate || undefined,
        to: toDate || undefined,
        limit: 100,
        offset: 0,
      });
      setExecutions(data.executions);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load executions', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  }, [statusFilter, fromDate, toDate]);

  useEffect(() => {
    fetchExecutions();
  }, [fetchExecutions]);

  const formatDuration = (ms: number) => {
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(2)}s`;
  };

  const columns: ColumnDef<AdminExecutionRow>[] = [
    {
      accessorKey: 'executionId',
      header: 'Execution ID',
      cell: ({ row }) => <span className="text-sm">{row.original.executionId.slice(0, 8)}...</span>,
    },
    {
      accessorKey: 'userEmail',
      header: 'User',
      cell: ({ row }) => <span className="text-sm">{row.original.userEmail}</span>,
    },
    {
      accessorKey: 'status',
      header: () => <span className="hidden md:table-cell">Status</span>,
      cell: ({ row }) => (
        <Badge variant="outline" className="hidden md:inline-flex">{row.original.status}</Badge>
      ),
    },
    {
      accessorKey: 'workflowName',
      header: 'Workflow',
      cell: ({ row }) => <span className="text-sm">{row.original.workflowName || '-'}</span>,
    },
    {
      accessorKey: 'taskCount',
      header: () => <span className="hidden md:table-cell">Tasks</span>,
      cell: ({ row }) => <span className="hidden md:table-cell text-sm">{row.original.taskCount}</span>,
    },
    {
      accessorKey: 'executionTimeMs',
      header: () => <span className="hidden md:table-cell">Duration</span>,
      cell: ({ row }) => <span className="hidden md:table-cell text-sm">{formatDuration(row.original.executionTimeMs)}</span>,
    },
    {
      accessorKey: 'createdAtUtc',
      header: () => <span className="hidden md:table-cell">Created</span>,
      cell: ({ row }) => <span className="hidden md:table-cell text-sm">{new Date(row.original.createdAtUtc).toLocaleDateString()}</span>,
    },
  ];

  const table = useReactTable({
    data: executions,
    columns,
    state: { pagination },
    onPaginationChange: setPagination,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
  });

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <SystemExecutionsSkeleton />
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-6 py-8 px-4 lg:px-6">
        <div>
            <h1 className="text-1xl font-semibold">System-wide Executions</h1>
            <p className="text-sm text-muted-foreground">View all execution activity across all users</p>
        </div>

        {/* Filters */}
        <div className="grid w-full sm:w-fit gap-8 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <Label htmlFor="status-filter">Status</Label>
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger id="status-filter" className="w-[90%] sm:w-auto sm:max-w-[200px]">
                <SelectValue placeholder="All statuses" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All statuses</SelectItem>
                <SelectItem value="Completed">Completed</SelectItem>
                <SelectItem value="Failed">Failed</SelectItem>
                <SelectItem value="Running">Running</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label htmlFor="from-date">From Date</Label>
            <Input
              id="from-date"
              type="date"
              value={fromDate}
              className="w-[85%] sm:w-auto sm:max-w-[150px]"
              onChange={(e) => setFromDate(e.target.value)}
            />
          </div>
          <div>
            <Label htmlFor="to-date">To Date</Label>
            <Input
              id="to-date"
              type="date"
              value={toDate}
              className="w-[85%] sm:w-auto sm:max-w-[150px]"
              onChange={(e) => setToDate(e.target.value)}
            />
          </div>
          <div className="flex items-end">
            <Button onClick={() => { setStatusFilter('all'); setFromDate(''); setToDate(''); }}>
              Clear Filters
            </Button>
          </div>
        </div>

        {/* Executions Table */}
        <div className="overflow-x-auto rounded-lg border">
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
                    onClick={() => navigate(`/executions/${row.original.executionId}`)}
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
                    No executions found.
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