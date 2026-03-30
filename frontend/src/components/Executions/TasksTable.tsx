import { useState } from 'react';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { formatSmartDateTime } from '@/lib/dateFormat';
import type { Task, TaskStatus } from '@/components/types';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';


interface TasksTableProps {
  tasks: Task[];
  onTaskClick: (task: Task) => void;
}

export function TasksTable({ tasks, onTaskClick }: TasksTableProps) {
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<TaskStatus | 'all'>('all');
  const [selected, setSelected] = useState<Set<string>>(new Set());

  const filteredTasks = tasks.filter((task) => {
    const matchesSearch = task.name.toLowerCase().includes(search.toLowerCase());
    const matchesStatus = statusFilter === 'all' || task.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  const toggleSelect = (id: string) => {
    const newSelected = new Set(selected);
    if (newSelected.has(id)) {
      newSelected.delete(id);
    } else {
      newSelected.add(id);
    }
    setSelected(newSelected);
  };

  const statusVariant = {
    success: 'default' as const,
    failed: 'destructive' as const,
    running: 'secondary' as const,
    queued: 'outline' as const,
    skipped: 'outline' as const,
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-4">
        <Input
          placeholder="Search tasks..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-sm"
          aria-label="Search tasks"
        />
        <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v as TaskStatus | 'all')}>
          <SelectTrigger className="w-[180px]" aria-label="Filter by status">
            <SelectValue placeholder="Filter by status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            <SelectItem value="success">Success</SelectItem>
            <SelectItem value="failed">Failed</SelectItem>
            <SelectItem value="running">Running</SelectItem>
            <SelectItem value="queued">Queued</SelectItem>
            <SelectItem value="skipped">Skipped</SelectItem>
          </SelectContent>
        </Select>
      </div>
      <div className="border rounded-lg overflow-x-auto">
        <Table>
          <TableHeader className="sticky top-0 bg-background">
            <TableRow>
              <TableHead className="w-12 hidden sm:table-cell">
                <Checkbox aria-label="Select all tasks" />
              </TableHead>
              <TableHead>Task Name</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="hidden md:table-cell">Agent Type</TableHead>
              <TableHead className="hidden lg:table-cell">Duration</TableHead>
              <TableHead className="hidden lg:table-cell">Started At</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredTasks.map((task) => (
              <TableRow
                key={task.id}
                onClick={() => onTaskClick(task)}
                className="cursor-pointer"
                role="button"
                tabIndex={0}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    onTaskClick(task);
                  }
                }}
              >
                <TableCell onClick={(e) => e.stopPropagation()} className="hidden sm:table-cell">
                  <Checkbox
                    checked={selected.has(task.id)}
                    onCheckedChange={() => toggleSelect(task.id)}
                    aria-label={`Select ${task.name}`}
                  />
                </TableCell>
                <TableCell className="font-medium">{task.name}</TableCell>
                <TableCell>
                  <Badge variant={statusVariant[task.status]}>{task.status}</Badge>
                </TableCell>
                <TableCell className="hidden md:table-cell">{task.agentType}</TableCell>
                <TableCell className="hidden lg:table-cell">{Math.floor(task.duration / 1000)}s</TableCell>
                <TableCell className="hidden lg:table-cell">{formatSmartDateTime(task.startedAt)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}