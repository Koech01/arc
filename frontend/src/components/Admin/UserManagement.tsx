import { toast } from 'sonner';
import { adminApi } from '@/lib/api';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { useState, useEffect, useCallback } from 'react';
import { ScrollArea } from "@/components/ui/scroll-area";
import type { AdminUserDetail } from '@/components/types/admin';
import type { LoginHistoryEntry } from '@/components/types/admin';
import { UserManagementSkeleton } from './UserManagementSkeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useReactTable, getCoreRowModel, getPaginationRowModel, flexRender, type ColumnDef } from '@tanstack/react-table';
import { CheckCircle2Icon, AlertTriangleIcon, Lock, Trash2, Shield, RefreshCw, History, ChevronLeftIcon, ChevronRightIcon, ChevronsLeftIcon, ChevronsRightIcon } from 'lucide-react';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog";


export default function UserManagement() {
  const [users, setUsers] = useState<AdminUserDetail[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchEmail, setSearchEmail] = useState('');
  const [searchUsername, setSearchUsername] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [pagination, setPagination] = useState({ pageIndex: 0, pageSize: 10 });
  const includeDeleted = false;
  
  // Action dialogs
  const [deleteUser, setDeleteUser] = useState<AdminUserDetail | null>(null);
  const [changeRole, setChangeRole] = useState<AdminUserDetail | null>(null);
  const [newRole, setNewRole] = useState<'Admin' | 'User'>('User');
  const [resetPassword, setResetPassword] = useState<AdminUserDetail | null>(null);
  const [newPassword, setNewPassword] = useState('');
  
  // Login history sheet
  const [loginHistory, setLoginHistory] = useState<{ user: AdminUserDetail; entries: LoginHistoryEntry[] } | null>(null);

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    try {
      const data = await adminApi.searchUsers({
        email: searchEmail || undefined,
        username: searchUsername || undefined,
        role: roleFilter && roleFilter !== 'all' ? roleFilter : undefined,
        isActive: statusFilter && statusFilter !== 'all' ? statusFilter === 'Active' : undefined,
        includeDeleted,
        limit: 100,
        offset: 0,
      });
      setUsers(data.users);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load users', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  }, [searchEmail, searchUsername, roleFilter, statusFilter, includeDeleted]);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  const handleToggleStatus = async (user: AdminUserDetail) => {
    try {
      await adminApi.toggleUserStatus(user.id, user.status !== 'Active');
      toast.success(`User ${user.status === 'Active' ? 'deactivated' : 'activated'} successfully`);
      fetchUsers();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to toggle user status');
    }
  };

  const handleChangeRole = async () => {
    if (!changeRole) return;
    try {
      await adminApi.changeUserRole(changeRole.id, newRole);
      toast.success('User role changed successfully');
      setChangeRole(null);
      fetchUsers();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to change role');
    }
  };

  const handleResetPassword = async () => {
    if (!resetPassword || newPassword.length < 8) {
      toast.error('Password must be at least 8 characters');
      return;
    }
    try {
      await adminApi.resetUserPassword(resetPassword.id, newPassword);
      toast.success('Password reset successfully');
      setResetPassword(null);
      setNewPassword('');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to reset password');
    }
  };

  const handleDeleteUser = async () => {
    if (!deleteUser) return;
    try {
      await adminApi.deleteUser(deleteUser.id);
      toast.success('User deleted successfully');
      setDeleteUser(null);
      fetchUsers();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete user');
    }
  };

  const handleViewLoginHistory = async (user: AdminUserDetail) => {
    try {
      const entries = await adminApi.getLoginHistory(user.id, 50);
      setLoginHistory({ user, entries });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load login history');
    }
  };

  const columns: ColumnDef<AdminUserDetail>[] = [
    {
      accessorKey: 'username',
      header: () => <span className="hidden md:table-cell">Username</span>,
      cell: ({ row }) => <span className="hidden md:table-cell font-medium">{row.original.username}</span>,
    },
    {
      accessorKey: 'email',
      header: 'Email',
      cell: ({ row }) => <span className="text-sm">{row.original.email}</span>,
    },
    {
      accessorKey: 'role',
      header: 'Role',
      cell: ({ row }) => <Badge variant="outline">{row.original.role}</Badge>,
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => (
        <Badge variant="outline" className="flex w-fit gap-1 px-2 [&_svg]:size-3">
          {row.original.status === 'Active' && <CheckCircle2Icon className="text-green-500 dark:text-green-400" />}
          {row.original.status === 'Inactive' && <AlertTriangleIcon className="text-red-500 dark:text-red-400" />}
          {row.original.status}
        </Badge>
      ),
    },
    {
      accessorKey: 'createdAt',
      header: () => <span className="hidden md:table-cell">Created</span>,
      cell: ({ row }) => <span className="hidden md:table-cell text-sm">{new Date(row.original.createdAt).toLocaleDateString()}</span>,
    },
    {
      id: 'actions',
      header: () => <span className="hidden md:table-cell">Actions</span>,
      cell: ({ row }) => (
        <div className="hidden md:flex gap-1">
          <Button
            size="sm"
            variant="ghost"
            onClick={() => handleToggleStatus(row.original)}
            aria-label={row.original.status === 'Active' ? 'Deactivate user' : 'Activate user'}
          >
            <Lock className="h-4 w-4" />
          </Button>
          <Button
            size="sm"
            variant="ghost"
            onClick={() => {
              setChangeRole(row.original);
              setNewRole(row.original.role);
            }}
            aria-label="Change role"
          >
            <Shield className="h-4 w-4" />
          </Button>
          <Button
            size="sm"
            variant="ghost"
            onClick={() => setResetPassword(row.original)}
            aria-label="Reset password"
          >
            <RefreshCw className="h-4 w-4" />
          </Button>
          <Button
            size="sm"
            variant="ghost"
            onClick={() => handleViewLoginHistory(row.original)}
            aria-label="View login history"
          >
            <History className="h-4 w-4" />
          </Button>
          <Button
            size="sm"
            variant="ghost"
            onClick={() => setDeleteUser(row.original)}
            aria-label="Delete user"
          >
            <Trash2 className="h-4 w-4 text-destructive" />
          </Button>
        </div>
      ),
    },
  ];

  const table = useReactTable({
    data: users,
    columns,
    state: { pagination },
    onPaginationChange: setPagination,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
  });

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <UserManagementSkeleton />
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-6 py-8 px-4 lg:px-6">
        <div>
            <h1 className="text-1xl font-semibold">User Management</h1>
            <p className="text-sm text-muted-foreground">Manage user accounts, roles, and permissions</p>
        </div>

        {/* Filters */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <Label htmlFor="search-email">Email</Label>
            <Input
              id="search-email"
              placeholder="Search by email..."
              value={searchEmail}
              onChange={(e) => setSearchEmail(e.target.value)}
            />
          </div>
          <div>
            <Label htmlFor="search-username">Username</Label>
            <Input
              id="search-username"
              placeholder="Search by username..."
              value={searchUsername}
              onChange={(e) => setSearchUsername(e.target.value)}
            />
          </div>
          <div>
            <Label htmlFor="role-filter">Role</Label>
            <Select value={roleFilter} onValueChange={setRoleFilter}>
              <SelectTrigger id="role-filter">
                <SelectValue placeholder="All roles" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All roles</SelectItem>
                <SelectItem value="Admin">Admin</SelectItem>
                <SelectItem value="User">User</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label htmlFor="status-filter">Status</Label>
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger id="status-filter">
                <SelectValue placeholder="All statuses" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All statuses</SelectItem>
                <SelectItem value="Active">Active</SelectItem>
                <SelectItem value="Inactive">Inactive</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Users Table */}
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
                    No users found.
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

      {/* Delete User Dialog */}
      <AlertDialog open={!!deleteUser} onOpenChange={() => setDeleteUser(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete User</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete {deleteUser?.username}? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDeleteUser} className="bg-destructive text-destructive-foreground">
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Change Role Dialog */}
      <AlertDialog open={!!changeRole} onOpenChange={() => setChangeRole(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Change User Role</AlertDialogTitle>
            <AlertDialogDescription>
              Change role for {changeRole?.username}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <div className="py-4">
            <Label htmlFor="new-role">New Role</Label>
            <Select value={newRole} onValueChange={(val) => setNewRole(val as 'Admin' | 'User')}>
              <SelectTrigger id="new-role">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Admin">Admin</SelectItem>
                <SelectItem value="User">User</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleChangeRole}>
              Change Role
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Reset Password Dialog */}
      <AlertDialog open={!!resetPassword} onOpenChange={() => { setResetPassword(null); setNewPassword(''); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Reset Password</AlertDialogTitle>
            <AlertDialogDescription>
              Set a new password for {resetPassword?.username}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <div className="py-4">
            <Label htmlFor="new-password">New Password (min 8 characters)</Label>
            <Input
              id="new-password"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="Enter new password"
            />
          </div>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleResetPassword}>
              Reset Password
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Login History Sheet */}
      <Sheet open={!!loginHistory} onOpenChange={() => setLoginHistory(null)}>
        <SheetContent className="w-full sm:max-w-2xl overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Login History</SheetTitle>
            <SheetDescription>
              Login history for {loginHistory?.user.username}
            </SheetDescription>
          </SheetHeader>
          <div className="mt-6">
            {loginHistory && loginHistory.entries.length > 0 ? (
              <div className="space-y-4">
                {loginHistory.entries.map((entry) => (
                  <div key={entry.id} className="border rounded-lg p-3">
                    <div className="flex items-center justify-between">
                      <Badge variant={entry.success ? 'default' : 'destructive'}>
                        {entry.success ? 'Success' : 'Failed'}
                      </Badge>
                      <span className="text-sm text-muted-foreground">
                        {new Date(entry.timestampUtc).toLocaleString()}
                      </span>
                    </div>
                    {!entry.success && entry.failureReason && (
                      <p className="text-sm text-destructive mt-2">{entry.failureReason}</p>
                    )}
                    {entry.ipAddress && (
                      <p className="text-sm text-muted-foreground mt-2">IP: {entry.ipAddress}</p>
                    )}
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-center text-muted-foreground">No login history available</p>
            )}
          </div>
        </SheetContent>
      </Sheet>
    </ScrollArea>
  );
}