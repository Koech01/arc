import type { Execution, Task } from '@/components/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';


interface OverviewTabProps {
  execution: Execution;
  tasks: Task[];
}

export function OverviewTab({ tasks }: OverviewTabProps) {
  const successCount = tasks.filter(t => t.status === 'success').length;
  const failedCount = tasks.filter(t => t.status === 'failed').length;
  const runningCount = tasks.filter(t => t.status === 'running').length;

  return (
    <div className="grid gap-4 grid-cols-2 lg:grid-cols-4">
      <Card>
        <CardHeader className="text-center pt-2 pb-2">
          <CardTitle className="text-sm font-medium text-muted-foreground">Total Tasks</CardTitle>
        </CardHeader>
        <CardContent className="text-center pb-2">
          <div className="text-2xl font-bold">{tasks.length}</div>
        </CardContent>
      </Card>
      <Card>
        <CardHeader className="text-center pt-2 pb-2">
          <CardTitle className="text-sm font-medium text-muted-foreground">Successful</CardTitle>
        </CardHeader>
        <CardContent className="text-center pb-2">
          <div className="text-2xl font-bold text-green-600">{successCount}</div>
        </CardContent>
      </Card>
      <Card>
        <CardHeader className="text-center pt-2 pb-2">
          <CardTitle className="text-sm font-medium text-muted-foreground">Failed</CardTitle>
        </CardHeader>
        <CardContent className="text-center pb-2">
          <div className="text-2xl font-bold text-red-600">{failedCount}</div>
        </CardContent>
      </Card>
      <Card>
        <CardHeader className="text-center pt-2 pb-2">
          <CardTitle className="text-sm font-medium text-muted-foreground">Running</CardTitle>
        </CardHeader>
        <CardContent className="text-center pb-2">
          <div className="text-2xl font-bold text-blue-600">{runningCount}</div>
        </CardContent>
      </Card>
    </div>
  );
}