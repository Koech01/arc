import { toast } from 'sonner';
import { adminApi } from '@/lib/api';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { useState, useEffect, useMemo } from 'react';
import { ScrollArea } from "@/components/ui/scroll-area";
import StatisticsCard from '@/components/ui/statistics-card';
import { AdminDashboardSkeleton } from './AdminDashboardSkeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { CheckCircle2Icon, AlertTriangleIcon, WrenchIcon, CogIcon, Users, PlayIcon } from 'lucide-react';
import type { AdminStats, User, SystemHealthComponent, AdminExecutionRow } from '@/components/types/admin';


export default function AdminDashboard() {
  const navigate = useNavigate();
  const [stats, setStats] = useState<AdminStats | null>(null);
  const [users, setUsers] = useState<User[]>([]);
  const [health, setHealth] = useState<SystemHealthComponent[]>([]);
  const [recentExecutions, setRecentExecutions] = useState<AdminExecutionRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchEmail, setSearchEmail] = useState('');

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [statsData, usersData, healthData, executionsData] = await Promise.all([
        adminApi.getStats(),
        adminApi.getUsers(),
        adminApi.getHealth(),
        adminApi.getExecutions({ limit: 5, offset: 0 }),
      ]);
      
      setStats(statsData);
      setUsers(usersData);
      setHealth(healthData);
      setRecentExecutions(executionsData.executions);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load admin data', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  const filteredUsers = useMemo(() => {
    return users.filter(user => 
      !searchEmail || user.email.toLowerCase().includes(searchEmail.toLowerCase())
    );
  }, [users, searchEmail]);

  if (loading) return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <AdminDashboardSkeleton />
    </ScrollArea>
  );

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-8 py-8 md:gap-8 md:py-8">
        {/* Dashboard Stats */}
        <div className="mx-auto grid max-w-7xl gap-4 sm:grid-cols-2 lg:grid-cols-3 w-full pl-4 pr-4">
          <StatisticsCard
            title="Total Users"
            value={stats?.totalUsers.toString() || '0'}
            changePercentage={`+${stats?.newUsersThisWeek || 0}`}
            trend="up"
            sentiment="positive"
            footerPrimary={`+${stats?.newUsersThisWeek || 0} users this week`}
            footerSecondary={`Active: ${stats?.activeUsers || 0}`}
          />
          <StatisticsCard
            title="Active LLMs"
            value={stats?.activeLLMs.toString() || '0'}
            changePercentage={`+${stats?.newLLMsThisWeek || 0}`}
            trend="up"
            sentiment="positive"
            footerPrimary={`+${stats?.newLLMsThisWeek || 0} LLMs this week`}
          />
          <StatisticsCard
            title="Total Executions"
            value={stats?.totalExecutions.toLocaleString() || '0'}
            changePercentage={`+${stats?.executionsToday || 0}`}
            trend="up"
            sentiment="positive"
            footerPrimary={`+${stats?.executionsToday || 0} executions today`}
          />
        </div>

        {/* Quick Actions */}
        <div className="flex flex-col gap-4 px-4 lg:px-6">
          <h2 className="text-lg font-semibold">Quick Actions</h2>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Button variant="outline" onClick={() => navigate('/admin/users')} className="justify-start">
              <Users className="mr-2 h-4 w-4" />
              Manage Users
            </Button>
            <Button variant="outline" onClick={() => navigate('/admin/executions')} className="justify-start">
              <PlayIcon className="mr-2 h-4 w-4" />
              View Executions
            </Button>
            <Button variant="outline" onClick={() => navigate('/admin/maintenance')} className="justify-start">
              <WrenchIcon className="mr-2 h-4 w-4" />
              Maintenance Mode
            </Button>
            <Button variant="outline" onClick={() => navigate('/admin/system')} className="justify-start">
              <CogIcon className="mr-2 h-4 w-4" />
              System Config
            </Button>
          </div>
        </div>

        {/* System Health */}
        {health.length > 0 && (
          <div className="flex flex-col gap-4 px-4 lg:px-6">
            <h2 className="text-lg font-semibold">System Health</h2>
            <div className="overflow-hidden rounded-lg border">
              <Table>
                <TableHeader className="bg-muted">
                  <TableRow>
                    <TableHead>Component</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="hidden md:table-cell">Uptime</TableHead>
                    <TableHead className="hidden lg:table-cell">Response Time</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {health.map((component) => (
                    <TableRow key={component.name}>
                      <TableCell><span className="font-medium">{component.name}</span></TableCell>
                      <TableCell>
                        <Badge variant="outline" className="flex w-fit gap-1 px-2 [&_svg]:size-3">
                          {component.status === 'Healthy' && <CheckCircle2Icon className="text-green-500 dark:text-green-400" />}
                          {component.status !== 'Healthy' && <AlertTriangleIcon className="text-red-500 dark:text-red-400" />}
                          {component.status}
                        </Badge>
                      </TableCell>
                      <TableCell className="hidden md:table-cell"><span className="text-sm">{component.uptime.toFixed(2)}%</span></TableCell>
                      <TableCell className="hidden lg:table-cell"><span className="text-sm">{component.responseTime}ms</span></TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </div>
        )}

        {/* Recent Executions */}
        {recentExecutions.length > 0 && (
          <div className="flex flex-col gap-4 px-4 lg:px-6">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold">Recent Executions</h2>
              <Button variant="link" onClick={() => navigate('/admin/executions')}>
                View All
              </Button>
            </div>
            <div className="overflow-hidden rounded-lg border">
              <Table>
                <TableHeader className="bg-muted">
                  <TableRow>
                    <TableHead>Execution ID</TableHead>
                    <TableHead className="hidden sm:table-cell">User</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="hidden md:table-cell">Tasks</TableHead>
                    <TableHead className="hidden lg:table-cell">Duration</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {recentExecutions.map((exec) => (
                    <TableRow key={exec.executionId}>
                      <TableCell><span className="text-sm">{exec.executionId.slice(0, 8)}...</span></TableCell>
                      <TableCell className="hidden sm:table-cell"><span className="text-sm">{exec.userEmail}</span></TableCell>
                      <TableCell>
                        <Badge variant="outline">{exec.status}</Badge>
                      </TableCell>
                      <TableCell className="hidden md:table-cell"><span className="text-sm">{exec.taskCount}</span></TableCell>
                      <TableCell className="hidden lg:table-cell"><span className="text-sm">{exec.executionTimeMs}ms</span></TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </div>
        )}

        {/* Users Table */}
        <div className="flex flex-col gap-4 px-4 lg:px-6">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold">Users</h2>
            <div className="w-64">
              <Label htmlFor="search-email" className="sr-only">Search by email</Label>
              <Input
                id="search-email"
                placeholder="Search by email..."
                value={searchEmail}
                onChange={(e) => setSearchEmail(e.target.value)}
                aria-label="Search users by email"
              />
            </div>
          </div>
          <div className="overflow-hidden rounded-lg border">
            <Table>
              <TableHeader className="bg-muted">
                <TableRow>
                  <TableHead>Email</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Created</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredUsers.length > 0 ? (
                  filteredUsers.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell><span className="font-medium">{user.email}</span></TableCell>
                      <TableCell>
                        <Badge variant="outline" className="flex w-fit gap-1 px-2 text-muted-foreground">
                          {user.role}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline" className="flex w-fit gap-1 px-2 text-muted-foreground [&_svg]:size-3">
                          {user.status === 'Active' && <CheckCircle2Icon className="text-green-500 dark:text-green-400" />}
                          {user.status === 'Inactive' && <AlertTriangleIcon className="text-red-500 dark:text-red-400" />}
                          {user.status}
                        </Badge>
                      </TableCell>
                      <TableCell><span className="text-sm">{new Date(user.createdAt).toLocaleDateString()}</span></TableCell>
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell colSpan={4} className="h-24 text-center">
                      No users found.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>
        </div>
      </div>
    </ScrollArea>
  );
}