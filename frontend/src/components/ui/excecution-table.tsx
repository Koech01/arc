"use client";
"use no memo";
import * as React from "react";
import { Badge } from "@/components/ui/badge";
import { Label } from "@/components/ui/label";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { formatSmartDateTime } from "@/lib/dateFormat";
import type { ExecutionListItem } from "@/components/types";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { type ColumnDef, flexRender, getCoreRowModel, getPaginationRowModel, useReactTable } from "@tanstack/react-table";
import { CheckCircle2Icon, ChevronLeftIcon, ChevronRightIcon, ChevronsLeftIcon, ChevronsRightIcon, LoaderIcon, XCircleIcon, ClockIcon } from "lucide-react";


const columns: ColumnDef<ExecutionListItem>[] = [
  {
    accessorKey: "workflowName",
    header: "Workflow",
    cell: ({ row }) => (
      <p className="truncate font-medium">{row.original.workflowName}</p>
    ),
  },
  {
    accessorKey: "id",
    header: () => <span className="hidden md:table-cell">ID</span>,
    cell: ({ row }) => (
      <span className="hidden md:table-cell font-mono text-sm">{row.original.id.slice(0, 8)}</span>
    ),
  },
  {
    accessorKey: "status",
    header: "Status",
    cell: ({ row }) => {
      const status = row.original.status;
      return (
        <Badge variant="outline" className="flex w-fit gap-1 px-2 text-muted-foreground [&_svg]:size-3">
          {status === "completed" && <CheckCircle2Icon className="text-green-500 dark:text-green-400" />}
          {status === "failed" && <XCircleIcon className="text-red-500 dark:text-red-400" />}
          {status === "running" && <LoaderIcon className="text-blue-500 dark:text-blue-400" />}
          {status === "queued" && <ClockIcon className="text-yellow-500 dark:text-yellow-400" />}
          {status.charAt(0).toUpperCase() + status.slice(1)}
        </Badge>
      );
    },
  },
  {
    accessorKey: "totalTasks",
    header: "Tasks",
    cell: ({ row }) => <span>{row.original.totalTasks || 0}</span>,
  },
  {
    accessorKey: "duration",
    header: () => <span className="hidden md:table-cell">Duration</span>,
    cell: ({ row }) => {
      const duration = row.original.duration;
      if (!duration || duration === "0" || duration === "0ms") return <span className="hidden md:table-cell text-sm">N/A</span>;
      return <span className="hidden md:table-cell text-sm">{duration}</span>;
    },
  },
  {
    accessorKey: "startedAt",
    header: () => <span className="hidden lg:table-cell">Started At</span>,
    cell: ({ row }) => (
      <span className="hidden lg:table-cell text-sm">{formatSmartDateTime(row.original.startedAt)}</span>
    ),
  },
]

export function ExecutionTable({ data: initialData }: { data: ExecutionListItem[] }) {
  const navigate = useNavigate();
  const [data] = React.useState(() => initialData)
  const [pagination, setPagination] = React.useState({
    pageIndex: 0,
    pageSize: 10,
  })

  const table = useReactTable({
    data,
    columns,
    state: { pagination },
    onPaginationChange: setPagination,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
  })

  return (
    <div className="flex flex-col gap-4 w-[92%] mx-auto lg:w-full lg:px-6">
      <h2 className="text-lg font-semibold">Recent Executions</h2>
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
                <TableRow key={row.id} className="cursor-pointer" onClick={() => navigate(`/executions/${row.original.id}`)}>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id} className="py-2 align-middle">
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
            <Select value={`${table.getState().pagination.pageSize}`} onValueChange={(value) => table.setPageSize(Number(value))}>
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
            <Button variant="outline" className="hidden h-8 w-8 p-0 lg:flex" onClick={() => table.setPageIndex(0)} disabled={!table.getCanPreviousPage()}>
              <span className="sr-only">Go to first page</span>
              <ChevronsLeftIcon />
            </Button>
            <Button variant="outline" className="size-8" size="icon" onClick={() => table.previousPage()} disabled={!table.getCanPreviousPage()}>
              <span className="sr-only">Go to previous page</span>
              <ChevronLeftIcon />
            </Button>
            <Button variant="outline" className="size-8" size="icon" onClick={() => table.nextPage()} disabled={!table.getCanNextPage()}>
              <span className="sr-only">Go to next page</span>
              <ChevronRightIcon />
            </Button>
            <Button variant="outline" className="hidden size-8 lg:flex" size="icon" onClick={() => table.setPageIndex(table.getPageCount() - 1)} disabled={!table.getCanNextPage()}>
              <span className="sr-only">Go to last page</span>
              <ChevronsRightIcon />
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}