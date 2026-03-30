import { toast } from 'sonner';
import { useState } from 'react';
import { batchApi } from '@/lib/api';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import type { BatchExecutionResult } from '@/components/types/batch';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';


export default function BatchExecutionPage() {
  const [plans, setPlans] = useState('');
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<BatchExecutionResult[]>([]);
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setResults([]);
    setLoading(true);

    try {
      const parsedPlans = JSON.parse(plans);
      if (!Array.isArray(parsedPlans)) {
        throw new Error('Input must be a JSON array of execution plans');
      }

      const response = await batchApi.execute({ plans: parsedPlans });
      setResults(response.results);
      toast.success('Batch execution completed', { position: 'top-center' });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to execute batch', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mx-auto p-6 max-w-4xl">
      <h1 className="text-3xl font-bold mb-6">Batch Execution</h1>

      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Execute Multiple Plans</CardTitle>
          <CardDescription>
            Submit an array of execution plans to run in batch. Each plan will be executed independently.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit}>
            <Textarea
              id="plans"
              value={plans}
              onChange={(e) => setPlans(e.target.value)}
              placeholder='[{"tasks": [{"id": "task1", "name": "Task 1", "agentType": "http", "config": {}, "dependencies": []}]}, {"tasks": [...]}]'
              required
              aria-label="Batch execution plans"
              aria-required="true"
              className="min-h-[300px]"
            />
            <Button type="submit" disabled={loading} aria-busy={loading} className="mt-4">
              {loading ? 'Executing...' : 'Execute Batch'}
            </Button>
          </form>
        </CardContent>
      </Card>

      {results.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Batch Results</CardTitle>
            <CardDescription>
              {results.filter(r => r.success).length} of {results.length} executions succeeded
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {results.map((result, index) => (
                <div
                  key={index}
                  className="flex items-center justify-between p-3 border rounded"
                >
                  <div>
                    <span className="font-medium">Execution {index + 1}</span>
                    {result.success ? (
                      <span className="ml-2 text-green-600">✓ Success</span>
                    ) : (
                      <span className="ml-2 text-red-600">✗ Failed: {result.error}</span>
                    )}
                  </div>
                  {result.executionId && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => navigate(`/executions/${result.executionId}`)}
                    >
                      View Details
                    </Button>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}