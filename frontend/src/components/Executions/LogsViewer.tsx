import { useState } from 'react';
import { Download } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { formatSmartDateTime } from '@/lib/dateFormat';
import type { ExecutionLog, LogLevel } from '@/components/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';


interface LogsViewerProps {
  logs: ExecutionLog[];
}

export function LogsViewer({ logs }: LogsViewerProps) {
  const [search, setSearch] = useState('');
  const [levelFilter, setLevelFilter] = useState<LogLevel | 'all'>('all');

  const filteredLogs = logs.filter((log) => {
    const matchesSearch = log.message.toLowerCase().includes(search.toLowerCase());
    const matchesLevel = levelFilter === 'all' || log.level === levelFilter;
    return matchesSearch && matchesLevel;
  });

  const levelColor = {
    info: 'text-blue-500',
    warning: 'text-yellow-500',
    error: 'text-red-500',
    debug: 'text-gray-500',
  };

  const downloadLogs = () => {
    const content = filteredLogs
      .map((log) => `[${log.timestamp}] [${log.level.toUpperCase()}] ${log.message}`)
      .join('\n');
    const blob = new Blob([content], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'execution-logs.txt';
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <Card className="pb-4">
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>Logs</CardTitle>
          <Button variant="outline" size="sm" onClick={downloadLogs} aria-label="Download logs">
            <Download className="h-4 w-4 mr-2" />
            Download
          </Button>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex items-center gap-4">
          <Input
            placeholder="Search logs..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="max-w-sm"
            aria-label="Search logs"
          />
          <Select value={levelFilter} onValueChange={(v) => setLevelFilter(v as LogLevel | 'all')}>
            <SelectTrigger className="w-[180px]" aria-label="Filter by log level">
              <SelectValue placeholder="Filter by level" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All levels</SelectItem>
              <SelectItem value="info">Info</SelectItem>
              <SelectItem value="warning">Warning</SelectItem>
              <SelectItem value="error">Error</SelectItem>
              <SelectItem value="debug">Debug</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="text-xs space-y-1 w-full rounded border bg-muted p-4">
          {filteredLogs.map((log) => (
            <div key={log.id} className="flex gap-2">
              <span className="text-muted-foreground">{formatSmartDateTime(log.timestamp)}</span>
              <span className={levelColor[log.level]}>[{log.level.toUpperCase()}]</span>
              <span>{log.message}</span>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}