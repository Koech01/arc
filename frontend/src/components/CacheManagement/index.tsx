import { toast } from 'sonner';
import { useState } from 'react';
import { cacheApi } from '@/lib/api';
import { Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';


export default function CacheManagementPage() {
  const [loading, setLoading] = useState(false);

  const handleClearCache = async () => {
    if (!confirm('Are you sure you want to clear the entire cache? This action cannot be undone.')) {
      return;
    }

    setLoading(true);

    try {
      await cacheApi.clear();
      toast.success('Cache cleared successfully', { position: 'top-center' });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to clear cache', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mx-auto p-6 max-w-4xl">
      <h1 className="text-3xl font-bold mb-6">Cache Management</h1>

      <Card>
        <CardHeader>
          <CardTitle>Task Execution Cache</CardTitle>
          <CardDescription>
            Clear cached task execution results. This will force all tasks to re-execute on next workflow run.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button
            variant="destructive"
            onClick={handleClearCache}
            disabled={loading}
            aria-busy={loading}
          >
            <Trash2 className="mr-2 h-4 w-4" />
            {loading ? 'Clearing Cache...' : 'Clear Cache'}
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}