import { executionApi } from '@/lib/api';
import { Badge } from '@/components/ui/badge';
import { useSearchParams } from 'react-router-dom';
import { useState, useEffect, useMemo } from 'react';
import type { Execution, Task } from '@/components/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { useReactTable, getCoreRowModel, getPaginationRowModel, flexRender, type ColumnDef } from '@tanstack/react-table';


function CompareTable({ exec, taskList }: { exec: Execution; taskList: Task[] }) {


  const formatExecutionId = (id: string): string => {
    if (id.length <= 16) return id;
    return `${id.slice(0, 15)}…${id.slice(-10)}`;
  };

  const columns: ColumnDef<Task>[] = useMemo(() => [
    {
      accessorKey: 'name',
      header: 'Task',
      cell: ({ row }) => <span>{row.original.name}</span>,
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => <Badge variant="outline">{row.original.status}</Badge>,
    },
    {
      accessorKey: 'duration',
      header: 'Duration',
      cell: ({ row }) => <span className="font-mono text-sm">{row.original.duration}ms</span>,
    },
  ], []);

  const table = useReactTable({
    data: taskList,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm">{formatExecutionId(exec.id)}</CardTitle>
        <Badge variant={exec.status === 'success' ? 'default' : 'destructive'} className="w-fit">
          {exec.status}
        </Badge>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="overflow-hidden rounded-lg border mb-4">
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
                  <TableRow key={row.id}>
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
                    No tasks found.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      </CardContent>
    </Card>
  );
}

export function CompareExecutions() {
  const [searchParams] = useSearchParams();
  const [executions, setExecutions] = useState<Execution[]>([]);
  const [tasks, setTasks] = useState<{ [key: string]: Task[] }>({});
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const ids = searchParams.get('ids')?.split(',') || [];
    if (ids.length === 2) {
      Promise.all(ids.map(id => Promise.all([
        executionApi.getExecution(id),
        executionApi.getTasks(id)
      ]))).then(results => {
        setExecutions(results.map(r => r[0]));
        setTasks(Object.fromEntries(results.map((r, i) => [ids[i], r[1]])));
      }).finally(() => setLoading(false));
    }
  }, [searchParams]);

  if (loading) return <div className="p-6">Loading...</div>;

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-1xl font-bold">Compare Executions</h1>
      <div className="grid grid-cols-2 gap-4">
        {executions.map(exec => (
          <CompareTable key={exec.id} exec={exec} taskList={tasks[exec.id] || []} />
        ))}
      </div>
    </div>
  );
}