import { toast } from 'sonner';
import { adminApi } from '@/lib/api';
import { useState, useEffect } from 'react';
import { Badge } from '@/components/ui/badge';
import { ScrollArea } from "@/components/ui/scroll-area";
import type { AdminLLMConfig } from '@/components/types/admin';
import { LLMConfigsAdminSkeleton } from './LLMConfigsAdminSkeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';


export default function LLMConfigsAdmin() {
  const [configs, setConfigs] = useState<AdminLLMConfig[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchConfigs();
  }, []);

  const fetchConfigs = async () => {
    setLoading(true);
    try {
      const data = await adminApi.getLLMConfigs(100, 0);
      setConfigs(data);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load LLM configurations', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <LLMConfigsAdminSkeleton />
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-6 py-8 px-4 lg:px-6">
        <div>
          <h1 className="text-2xl font-bold">LLM Configurations</h1>
          <p className="text-muted-foreground">View all LLM configurations across all users</p>
        </div>

        {/* LLM Configs Table */}
        <div className="overflow-hidden rounded-lg border">
          <Table>
            <TableHeader className="bg-muted">
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Model</TableHead>
                <TableHead>Base URL</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Owner</TableHead>
                <TableHead>Created</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {configs.length > 0 ? (
                configs.map((config) => (
                  <TableRow key={config.id}>
                    <TableCell><span className="font-medium">{config.name}</span></TableCell>
                    <TableCell><Badge variant="outline">{config.model}</Badge></TableCell>
                    <TableCell><span className="text-sm truncate max-w-xs">{config.baseUrl}</span></TableCell>
                    <TableCell>
                      <Badge variant={config.isActive ? 'default' : 'secondary'}>
                        {config.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                    </TableCell>
                    <TableCell><span className="text-sm">{config.ownerEmail}</span></TableCell>
                    <TableCell><span className="text-sm">{new Date(config.createdAt).toLocaleDateString()}</span></TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center">
                    No LLM configurations found.
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