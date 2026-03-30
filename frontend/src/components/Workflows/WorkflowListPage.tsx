import { toast } from 'sonner';
import { workflowApi } from '@/lib/api';
import { useState, useEffect } from 'react';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { formatSmartDateTime } from '@/lib/dateFormat';
import { ScrollArea } from "@/components/ui/scroll-area";
import { WorkflowListSkeleton } from './WorkflowListSkeleton';
import { ExecutionResultDialog } from './ExecutionResultDialog';
import { Plus, Play, Trash2, Eye, Clock, Webhook, Copy } from 'lucide-react';
import type { Workflow, ExecuteWorkflowResponse } from '@/components/types/workflow';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { ChevronLeftIcon, ChevronRightIcon, ChevronsLeftIcon, ChevronsRightIcon } from "lucide-react";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useReactTable, getCoreRowModel, getPaginationRowModel, flexRender, type ColumnDef } from '@tanstack/react-table';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '@/components/ui/alert-dialog';


export function WorkflowListPage() {
  const navigate = useNavigate();
  const [workflows, setWorkflows] = useState<Workflow[]>([]);
  const [loading, setLoading] = useState(true);
  const [executionResult, setExecutionResult] = useState<ExecuteWorkflowResponse | null>(null);
  const [showResultDialog, setShowResultDialog] = useState(false);
  const [executing, setExecuting] = useState<string | null>(null);
  const [deleteWorkflowId, setDeleteWorkflowId] = useState<string | null>(null);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [duplicating, setDuplicating] = useState<string | null>(null);

  useEffect(() => {
    loadWorkflows();
  }, []);

  const loadWorkflows = async () => {
    try {
      setLoading(true);
      const data = await workflowApi.getAll();
      setWorkflows(data);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load workflows', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setDeleteWorkflowId(id);
    setShowDeleteDialog(true);
  };

  const confirmDelete = async () => {
    if (!deleteWorkflowId) return;

    try {
      await workflowApi.delete(deleteWorkflowId);
      setWorkflows(workflows.filter(w => w.id !== deleteWorkflowId));
      toast.success('Workflow deleted successfully', { position: 'top-center' });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete workflow', { position: 'top-center' });
    } finally {
      setShowDeleteDialog(false);
      setDeleteWorkflowId(null);
    }
  };

  const handleExecute = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setExecuting(id);
    try {
      const result = await workflowApi.execute(id);
      setExecutionResult(result);
      setShowResultDialog(true);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to execute workflow', { position: 'top-center' });
    } finally {
      setExecuting(null);
    }
  };

  const handleViewExecutionDetails = () => {
    if (executionResult) {
      setShowResultDialog(false);
      navigate(`/executions/${executionResult.executionId}`);
    }
  };

  const handleDuplicate = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setDuplicating(id);
    try {
      const result = await workflowApi.duplicate(id);
      toast.success(`Workflow duplicated: ${result.name}`, { position: 'top-center' });
      await loadWorkflows();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to duplicate workflow', { position: 'top-center' });
    } finally {
      setDuplicating(null);
    }
  };

  const columns: ColumnDef<Workflow>[] = [
    {
      accessorKey: 'name',
      header: 'Name',
      cell: ({ row }) => <span className="font-medium">{row.original.name}</span>,
    },
    {
      accessorKey: 'description',
      header: () => <span className="hidden md:table-cell">Description</span>,
      cell: ({ row }) => (
        <span className="hidden md:table-cell text-muted-foreground max-w-md break-words">
          {row.original.description || '-'}
        </span>
      ),
    },
    {
      accessorKey: 'triggerType',
      header: () => <span className="hidden sm:table-cell">Trigger Type</span>,
      cell: ({ row }) => {
        const type = row.original.triggerType;
        return (
          <Badge variant="outline" className="hidden sm:flex w-fit gap-1 px-2 text-muted-foreground [&_svg]:size-3">
            {type === 'manual' && <Play className="text-blue-500 dark:text-blue-400" />}
            {type === 'scheduled' && <Clock className="text-purple-500 dark:text-purple-400" />}
            {type === 'webhook' && <Webhook className="text-green-500 dark:text-green-400" />}
            {type.charAt(0).toUpperCase() + type.slice(1)}
          </Badge>
        );
      },
    },
    {
      accessorKey: 'createdAt',
      header: () => <span className="hidden lg:table-cell">Created At</span>,
      cell: ({ row }) => <span className="hidden lg:table-cell text-sm">{formatSmartDateTime(row.original.createdAt)}</span>,
    },
    {
      id: 'actions',
      header: () => <div className="text-right">Actions</div>,
      cell: ({ row }) => (
        <div className="flex items-center justify-end gap-2">
          <Button
            size="icon"
            variant="ghost"
            onClick={(e) => {
              e.stopPropagation();
              navigate(`/workflows/${row.original.id}`);
            }}
            aria-label={`View ${row.original.name}`}
          >
            <Eye className="h-4 w-4" />
          </Button>
          <Button
            size="icon"
            variant="ghost"
            onClick={(e) => handleExecute(row.original.id, e)}
            disabled={executing === row.original.id}
            aria-label={`Execute ${row.original.name}`}
            aria-busy={executing === row.original.id}
          >
            <Play className="h-4 w-4" />
          </Button>
          <Button
            size="icon"
            variant="ghost"
            onClick={(e) => handleDuplicate(row.original.id, e)}
            disabled={duplicating === row.original.id}
            aria-label={`Duplicate ${row.original.name}`}
            aria-busy={duplicating === row.original.id}
          >
            <Copy className="h-4 w-4" />
          </Button>
          <Button
            size="icon"
            variant="ghost"
            onClick={(e) => handleDelete(row.original.id, e)}
            aria-label={`Delete ${row.original.name}`}
          >
            <Trash2 className="h-4 w-4 text-red-500" />
          </Button>
        </div>
      ),
    },
  ];

  const [pagination, setPagination] = useState({ pageIndex: 0, pageSize: 10 });

  const table = useReactTable({
    data: workflows,
    columns,
    state: { pagination },
    onPaginationChange: setPagination,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
  });

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <WorkflowListSkeleton />
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-6 p-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">Workflows</h1>
            <p className="text-sm text-muted-foreground">Manage workflow templates</p>
          </div>
          <Button onClick={() => navigate('/workflows/create')} aria-label="Create new workflow">
            <Plus className="h-4 w-4" />
            Create Workflow
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
                    onClick={() => navigate(`/workflows/${row.original.id}`)}
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
                    <p className="text-muted-foreground">No workflows found.</p>
                    <Button variant="link" onClick={() => navigate('/workflows/create')} className="mt-2">
                      Create your first workflow
                    </Button>
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

        <ExecutionResultDialog
          open={showResultDialog}
          onOpenChange={setShowResultDialog}
          result={executionResult}
          onViewDetails={handleViewExecutionDetails}
        />

        <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
          <AlertDialogContent className="p-0 w-[90vw] max-w-[90vw] mx-auto rounded-lg md:w-auto md:max-w-lg">
            <AlertDialogHeader className="pt-4 pl-4 pr-4">
              <AlertDialogTitle>Delete workflow?</AlertDialogTitle>
              <AlertDialogDescription>
                This action cannot be undone. This will permanently delete the workflow and all its configurations.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter className="bg-muted/50 rounded-b-xl border-t pt-3 pl-3 pr-3 pb-3">
              <AlertDialogCancel>Cancel</AlertDialogCancel>
              <AlertDialogAction onClick={confirmDelete} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
                Delete
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    </ScrollArea>
  );
}