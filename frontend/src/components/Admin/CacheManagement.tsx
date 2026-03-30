import { toast } from 'sonner';
import { adminApi } from '@/lib/api';
import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { ScrollArea } from "@/components/ui/scroll-area";
import StatisticsCard from '@/components/ui/statistics-card';
import type { AdminCacheStats } from '@/components/types/admin';
import { CacheManagementSkeleton } from './CacheManagementSkeleton';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog";


export default function CacheManagement() {
  const [stats, setStats] = useState<AdminCacheStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [showClearDialog, setShowClearDialog] = useState(false);
  const [clearing, setClearing] = useState(false);

  useEffect(() => {
    fetchStats();
  }, []);

  const fetchStats = async () => {
    setLoading(true);
    try {
      const data = await adminApi.getCacheStats();
      setStats(data);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load cache stats', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  const handleClearCache = async () => {
    setClearing(true);
    try {
      await adminApi.clearCache();
      toast.success('Cache cleared successfully');
      setShowClearDialog(false);
      fetchStats();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to clear cache');
    } finally {
      setClearing(false);
    }
  };

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <CacheManagementSkeleton />
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-6 py-8 px-4 lg:px-6">
        <div>
          <h1 className="text-2xl font-bold">Cache Management</h1>
          <p className="text-muted-foreground">Monitor and manage task execution cache</p>
        </div>

        {/* Cache Stats */}
        {stats && (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <StatisticsCard
              title="Total Entries"
              value={stats.totalEntries.toString()}
              changePercentage=""
              trend="up"
              sentiment="positive"
              footerPrimary="All cache entries"
            />
            <StatisticsCard
              title="Active Entries"
              value={stats.activeEntries.toString()}
              changePercentage=""
              trend="up"
              sentiment="positive"
              footerPrimary="Valid cache entries"
            />
            <StatisticsCard
              title="Expired Entries"
              value={stats.expiredEntries.toString()}
              changePercentage=""
              trend="down"
              sentiment="negative"
              footerPrimary="Expired cache entries"
            />
          </div>
        )}

        {/* Cache Actions */}
        <div className="flex flex-col gap-4">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-lg font-semibold">Cache Actions</h2>
              <p className="text-sm text-muted-foreground">
                Clear the cache to free up space and remove expired entries
              </p>
            </div>
            <Button 
              variant="destructive" 
              onClick={() => setShowClearDialog(true)}
              disabled={loading || !stats || stats.totalEntries === 0}
            >
              Clear Cache
            </Button>
          </div>

          {stats && (
            <div className="border rounded-lg p-4">
              <div className="grid gap-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Oldest Entry:</span>
                  <span>
                    {stats.oldestEntryUtc ? new Date(stats.oldestEntryUtc).toLocaleString() : 'N/A'}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Newest Entry:</span>
                  <span>
                    {stats.newestEntryUtc ? new Date(stats.newestEntryUtc).toLocaleString() : 'N/A'}
                  </span>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Clear Cache Dialog */}
      <AlertDialog open={showClearDialog} onOpenChange={setShowClearDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Clear Cache</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to clear all cache entries? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={clearing}>Cancel</AlertDialogCancel>
            <AlertDialogAction 
              onClick={handleClearCache} 
              className="bg-destructive text-destructive-foreground"
              disabled={clearing}
            >
              {clearing ? 'Clearing...' : 'Clear Cache'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </ScrollArea>
  );
}