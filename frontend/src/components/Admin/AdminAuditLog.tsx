import { toast } from 'sonner';
import { adminApi } from '@/lib/api';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { useState, useEffect, useCallback } from 'react';
import { ScrollArea } from "@/components/ui/scroll-area";
import { AdminAuditLogSkeleton } from './AdminAuditLogSkeleton';
import type { AdminAuditEntry } from '@/components/types/admin';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";


const AUDIT_ACTIONS = [
  'ViewedUserList',
  'ViewedUserDetail',
  'ViewedStats',
  'ActivatedUser',
  'DeactivatedUser',
  'ChangedUserRole',
  'ResetUserPassword',
  'DeletedUser',
  'ViewedAllExecutions',
  'ViewedLLMConfigs',
  'ViewedWebhooks',
  'DisabledWebhook',
  'ClearedCache',
  'ClearedUserCache',
  'ViewedCacheStats',
  'EnabledMaintenanceMode',
  'DisabledMaintenanceMode',
  'ViewedSystemConfig',
  'ViewedLoginHistory',
  'ViewedAdminAuditLog',
];

export default function AdminAuditLog() {
  const [entries, setEntries] = useState<AdminAuditEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [actionFilter, setActionFilter] = useState('all');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');

  const fetchAuditLog = useCallback(async () => {
    setLoading(true);
    try {
      const data = await adminApi.getAuditLog({
        action: actionFilter && actionFilter !== 'all' ? actionFilter : undefined,
        from: fromDate || undefined,
        to: toDate || undefined,
        limit: 100,
        offset: 0,
      });
      setEntries(data);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load audit log', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  }, [actionFilter, fromDate, toDate]);

  useEffect(() => {
    fetchAuditLog();
  }, [fetchAuditLog]);

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <AdminAuditLogSkeleton />
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-6 py-8 px-4 lg:px-6">
        <div>
          <h1 className="text-2xl font-bold">Admin Audit Log</h1>
          <p className="text-muted-foreground">Track all administrative actions performed in the system</p>
        </div>

        {/* Filters */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <Label htmlFor="action-filter">Action</Label>
            <Select value={actionFilter} onValueChange={setActionFilter}>
              <SelectTrigger id="action-filter">
                <SelectValue placeholder="All actions" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All actions</SelectItem>
                {AUDIT_ACTIONS.map((action) => (
                  <SelectItem key={action} value={action}>
                    {action}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label htmlFor="from-date">From Date</Label>
            <Input
              id="from-date"
              type="datetime-local"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
            />
          </div>
          <div>
            <Label htmlFor="to-date">To Date</Label>
            <Input
              id="to-date"
              type="datetime-local"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
            />
          </div>
          <div className="flex items-end">
            <Button onClick={() => { setActionFilter('all'); setFromDate(''); setToDate(''); }}>
              Clear Filters
            </Button>
          </div>
        </div>

        {/* Audit Log Table */}
        <div className="overflow-hidden rounded-lg border">
          <Table>
            <TableHeader className="bg-muted">
              <TableRow>
                <TableHead>Timestamp</TableHead>
                <TableHead>Admin User</TableHead>
                <TableHead>Action</TableHead>
                <TableHead>Target User</TableHead>
                <TableHead>Details</TableHead>
                <TableHead>IP Address</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {entries.length > 0 ? (
                entries.map((entry) => (
                  <TableRow key={entry.id}>
                    <TableCell>
                      <span className="text-sm">{new Date(entry.timestampUtc).toLocaleString()}</span>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm">{entry.adminUserId.slice(0, 8)}...</span>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline">{entry.adminAuditAction}</Badge>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm">
                        {entry.targetUserId ? `${entry.targetUserId.slice(0, 8)}...` : '-'}
                      </span>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm">{entry.detail || '-'}</span>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm">{entry.ipAddress || '-'}</span>
                    </TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center">
                    No audit log entries found.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      </div>
    </ScrollArea>
  );
}