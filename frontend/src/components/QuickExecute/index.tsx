import { toast } from 'sonner';
import { useState } from 'react';
import { Label } from '@/components/ui/label';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';


const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

interface ExecuteResponse {
  executionId: string;
}

export default function QuickExecutePage() {
  const navigate = useNavigate();
  const [plan, setPlan] = useState('');
  const [loading, setLoading] = useState(false);

  const handleExecute = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      const response = await fetch(`${API_BASE_URL}/execute`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ plan }),
      });

      if (!response.ok) {
        const error = await response.json().catch(() => ({ message: 'Execution failed' }));
        throw new Error(error.message);
      }

      const result: ExecuteResponse = await response.json();
      navigate(`/executions/${result.executionId}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to execute', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">Quick Execute</h1>
        <p className="text-sm text-muted-foreground">Execute tasks directly without creating a workflow</p>
      </div>

      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>Execution Plan</CardTitle>
          <CardDescription>Enter your execution plan in JSON format</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleExecute} className="space-y-4">
            <div>
              <Label htmlFor="plan">Plan (JSON)</Label>
              <textarea
                id="plan"
                value={plan}
                onChange={(e) => setPlan(e.target.value)}
                className="w-full min-h-[300px] mt-2 p-3 border rounded-md text-sm"
                placeholder='{\n  "tasks": [\n    {\n      "id": "task-1",\n      "name": "Example Task",\n      "agentType": "http",\n      "config": {},\n      "dependencies": []\n    }\n  ]\n}'
                required
                aria-label="Execution plan"
                aria-required="true"
              />
            </div>
            <Button type="submit" disabled={loading} aria-busy={loading}>
              {loading ? 'Executing...' : 'Execute'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}