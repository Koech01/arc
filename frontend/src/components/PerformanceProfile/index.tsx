import { toast } from 'sonner';
import { performanceApi } from '@/lib/api';
import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import type { PerformanceProfile } from '@/components/types/performance';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';


export function PerformanceProfilePage() {
  const { id } = useParams<{ id: string }>();
  const [profile, setProfile] = useState<PerformanceProfile | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchProfile = async () => {
      if (!id) return;
      setLoading(true);
      try {
        const data = await performanceApi.getProfile(id);
        setProfile(data);
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'Failed to load performance profile', { position: 'top-center' });
      } finally {
        setLoading(false);
      }
    };

    fetchProfile();
  }, [id]);

  if (loading) return <div className="flex justify-center p-8">Loading...</div>;
  if (!profile) return <div className="flex justify-center p-8">No profile data available</div>;

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-bold">Performance Profile</h1>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card>
          <CardHeader>
            <CardTitle>Total Duration</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{profile.totalDuration}ms</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Critical Path</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{profile.criticalPathDuration}ms</p>
            <p className="text-sm text-muted-foreground">{profile.criticalPath.length} tasks</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Parallelization</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{profile.parallelizationEfficiency}%</p>
            <p className="text-sm text-muted-foreground">Max concurrent: {profile.maxConcurrentTasks}</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Critical Path Tasks</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-2">
            {profile.criticalPath.map((taskId) => (
              <Badge key={taskId} variant="destructive">{taskId}</Badge>
            ))}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Task Metrics</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Task</TableHead>
                <TableHead>Execution Time</TableHead>
                <TableHead>Wait Time</TableHead>
                <TableHead>Critical Path</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {profile.taskMetrics.map((task) => (
                <TableRow key={task.taskId}>
                  <TableCell>{task.taskName}</TableCell>
                  <TableCell>{task.executionTime}ms</TableCell>
                  <TableCell>{task.waitTime}ms</TableCell>
                  <TableCell>
                    {task.isOnCriticalPath && <Badge variant="destructive">Critical</Badge>}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}