import { toast } from 'sonner';
import { useState, useEffect } from 'react';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { regressionGatesApi, goldenExecutionsApi } from '@/lib/api';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle2, XCircle, Trash2, Power, TestTube, Plus, Star, Loader2 } from 'lucide-react';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import type { RegressionGate, RegressionTestResult, DivergenceRuleType, DivergenceRule, GoldenExecution } from '@/components/types/regression-gates';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '@/components/ui/alert-dialog';


export default function RegressionGatesPage() {
  const [gates, setGates] = useState<RegressionGate[]>([]);
  const [goldenExecutions, setGoldenExecutions] = useState<GoldenExecution[]>([]);
  const [loading, setLoading] = useState(true);
  const [createOpen, setCreateOpen] = useState(false);
  const [testGateId, setTestGateId] = useState<string | null>(null);
  const [testResult, setTestResult] = useState<RegressionTestResult | null>(null);
  const [deleteGateId, setDeleteGateId] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      const [gatesRes, goldenRes] = await Promise.allSettled([
        regressionGatesApi.list(),
        goldenExecutionsApi.list(),
      ]);
      
      if (gatesRes.status === 'fulfilled') setGates(gatesRes.value.gates);
      if (goldenRes.status === 'fulfilled') setGoldenExecutions(goldenRes.value.goldenExecutions);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  const handleToggle = async (gateId: string, isActive: boolean) => {
    try {
      await regressionGatesApi.toggle(gateId, { isActive: !isActive });
      setGates(gates.map(g => g.id === gateId ? { ...g, isActive: !isActive } : g));
      toast.success(`Gate ${!isActive ? 'activated' : 'deactivated'}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to toggle gate');
    }
  };

  const handleDelete = async () => {
    if (!deleteGateId) return;
    try {
      await regressionGatesApi.delete(deleteGateId);
      setGates(gates.filter(g => g.id !== deleteGateId));
      toast.success('Gate deleted');
      setDeleteGateId(null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete gate');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <Loader2 className="w-8 h-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Regression Gates</h1>
          <p className="text-muted-foreground mt-1">
            Compare executions against golden baselines to detect regressions
          </p>
        </div>
        <CreateGateDialog 
          open={createOpen} 
          onOpenChange={setCreateOpen}
          goldenExecutions={goldenExecutions}
          onSuccess={() => { loadData(); setCreateOpen(false); }}
        />
      </div>

      <div className="space-y-4">
        {gates.length === 0 ? (
          <Card>
            <CardContent className="text-center py-12">
              <Star className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
              <h3 className="text-lg font-semibold mb-2">No Regression Gates</h3>
              <p className="text-sm text-muted-foreground mb-4">
                Create your first gate to protect against unwanted changes
              </p>
              <Button onClick={() => setCreateOpen(true)}>
                <Plus className="w-4 h-4 mr-2" />
                Create Gate
              </Button>
            </CardContent>
          </Card>
        ) : (
          gates.map((gate) => (
            <Card key={gate.id} className={!gate.isActive ? 'opacity-60' : ''}>
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <CardTitle className="flex items-center gap-2">
                      {gate.name}
                      <Badge variant={gate.isActive ? 'default' : 'secondary'}>
                        {gate.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                    </CardTitle>
                    <CardDescription>{gate.description || 'No description'}</CardDescription>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => setTestGateId(gate.id)}
                      disabled={!gate.isActive}
                      aria-label="Test gate"
                    >
                      <TestTube className="w-4 h-4 mr-1" />
                      Test
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => handleToggle(gate.id, gate.isActive)}
                      aria-label={gate.isActive ? 'Deactivate gate' : 'Activate gate'}
                    >
                      <Power className="w-4 h-4" />
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => setDeleteGateId(gate.id)}
                      aria-label="Delete gate"
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  <div className="text-sm">
                    <span className="font-medium">Golden Baseline:</span>{' '}
                    <code className="text-xs bg-muted px-2 py-0.5 rounded ml-1">{gate.goldenExecutionId}</code>
                  </div>
                  <div>
                    <span className="font-medium text-sm">Rules:</span>
                    <ul className="mt-2 space-y-1">
                      {gate.rules.map((rule, idx) => (
                        <li key={idx} className="text-sm flex items-center gap-2">
                          <Badge variant="outline">{rule.type}</Badge>
                          <span className="text-muted-foreground">
                            Threshold: {(rule.threshold * 100).toFixed(0)}%
                          </span>
                        </li>
                      ))}
                    </ul>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))
        )}
      </div>

      {testGateId && (
        <TestGateDialog
          gateId={testGateId}
          open={!!testGateId}
          onOpenChange={(open) => { if (!open) { setTestGateId(null); setTestResult(null); } }}
          onResult={setTestResult}
        />
      )}

      {testResult && (
        <TestResultDialog
          result={testResult}
          open={!!testResult}
          onOpenChange={(open) => { if (!open) setTestResult(null); }}
        />
      )}

      <AlertDialog open={!!deleteGateId} onOpenChange={(open) => { if (!open) setDeleteGateId(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Regression Gate</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete this gate. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete}>Delete</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

interface CreateGateDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  goldenExecutions: GoldenExecution[];
  onSuccess: () => void;
}

function CreateGateDialog({ open, onOpenChange, goldenExecutions, onSuccess }: CreateGateDialogProps) {
  const [name, setName] = useState('');
  const [gateDescription, setGateDescription] = useState('');
  const [goldenExecutionId, setGoldenExecutionId] = useState('');
  const [rules, setRules] = useState<DivergenceRule[]>([
    { type: 'SimilarityPercentage', threshold: 0.95 },
  ]);
  const [submitting, setSubmitting] = useState(false);

  const addRule = () => {
    setRules([...rules, { type: 'SimilarityPercentage', threshold: 0.90 }]);
  };

  const removeRule = (index: number) => {
    setRules(rules.filter((_, i) => i !== index));
  };

  const updateRule = (index: number, updates: Partial<DivergenceRule>) => {
    setRules(rules.map((rule, i) => i === index ? { ...rule, ...updates } : rule));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      await regressionGatesApi.create({
        name: name.trim(),
        description: gateDescription.trim() || undefined,
        goldenExecutionId,
        rules,
      });
      toast.success('Regression gate created');
      setName('');
      setGateDescription('');
      setGoldenExecutionId('');
      setRules([{ type: 'SimilarityPercentage', threshold: 0.95 }]);
      onSuccess();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to create gate');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        <Button>
          <Plus className="w-4 h-4 mr-2" />
          Create Gate
        </Button>
      </DialogTrigger>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Create Regression Gate</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <Label htmlFor="name">Gate Name*</Label>
            <Input
              id="name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Production quality gate"
              required
              maxLength={200}
              aria-required="true"
            />
          </div>

          <div>
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              value={gateDescription}
              onChange={(e) => setGateDescription(e.target.value)}
              placeholder="Ensures new model maintains output quality"
              rows={3}
              maxLength={1000}
            />
          </div>

          <div>
            <Label htmlFor="goldenExecutionId">Golden Execution ID*</Label>
            <Select value={goldenExecutionId} onValueChange={setGoldenExecutionId} required>
              <SelectTrigger id="goldenExecutionId" aria-required="true">
                <SelectValue placeholder="Select golden baseline..." />
              </SelectTrigger>
              <SelectContent>
                {goldenExecutions.length === 0 ? (
                  <div className="p-2 text-sm text-muted-foreground text-center">
                    No golden executions. Mark an execution as golden first.
                  </div>
                ) : (
                  goldenExecutions.map((exec) => (
                    <SelectItem key={exec.executionId} value={exec.executionId}>
                      {exec.executionId}
                      {exec.label && ` - ${exec.label}`}
                    </SelectItem>
                  ))
                )}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <Label>Divergence Rules* (at least 1)</Label>
              <Button type="button" size="sm" variant="outline" onClick={addRule}>
                <Plus className="w-4 h-4 mr-1" />
                Add Rule
              </Button>
            </div>

            {rules.map((rule, index) => (
              <div key={index} className="flex gap-2 items-end p-3 border rounded-lg">
                <div className="flex-1">
                  <Label className="text-xs">Rule Type</Label>
                  <Select
                    value={rule.type}
                    onValueChange={(value) => updateRule(index, { type: value as DivergenceRuleType })}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="SimilarityPercentage">Similarity Percentage</SelectItem>
                      <SelectItem value="MaxTaskDivergence">Max Task Divergence</SelectItem>
                      <SelectItem value="CriticalPathPreservation">Critical Path Preservation</SelectItem>
                      <SelectItem value="NoStatusDegradation">No Status Degradation</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="flex-1">
                  <Label className="text-xs">Threshold (0.0 - 1.0)</Label>
                  <Input
                    type="number"
                    min="0"
                    max="1"
                    step="0.01"
                    value={rule.threshold}
                    onChange={(e) => updateRule(index, { threshold: parseFloat(e.target.value) })}
                  />
                </div>

                <Button
                  type="button"
                  size="sm"
                  variant="ghost"
                  onClick={() => removeRule(index)}
                  disabled={rules.length === 1}
                  aria-label="Remove rule"
                >
                  <Trash2 className="w-4 h-4" />
                </Button>
              </div>
            ))}
          </div>

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={submitting || !goldenExecutionId}>
              {submitting ? 'Creating...' : 'Create Gate'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

interface TestGateDialogProps {
  gateId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onResult: (result: RegressionTestResult) => void;
}

function TestGateDialog({ gateId, open, onOpenChange, onResult }: TestGateDialogProps) {
  const [candidateExecutionId, setCandidateExecutionId] = useState('');
  const [testing, setTesting] = useState(false);

  const handleTest = async (e: React.FormEvent) => {
    e.preventDefault();
    setTesting(true);
    try {
      const result = await regressionGatesApi.test(gateId, { candidateExecutionId });
      onResult(result);
      toast.success('Test completed');
      onOpenChange(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to test gate');
    } finally {
      setTesting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Test Regression Gate</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleTest} className="space-y-4">
          <div>
            <Label htmlFor="candidateExecutionId">Candidate Execution ID*</Label>
            <Input
              id="candidateExecutionId"
              value={candidateExecutionId}
              onChange={(e) => setCandidateExecutionId(e.target.value)}
              placeholder="Enter execution ID to test..."
              required
              aria-required="true"
            />
            <p className="text-xs text-muted-foreground mt-1">
              The execution to compare against the golden baseline
            </p>
          </div>

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={testing || !candidateExecutionId.trim()}>
              {testing ? 'Testing...' : 'Run Test'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

interface TestResultDialogProps {
  result: RegressionTestResult;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

function TestResultDialog({ result, open, onOpenChange }: TestResultDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Test Results</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <Card className={result.passed ? 'border-green-500' : 'border-red-500'}>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                {result.passed ? (
                  <>
                    <CheckCircle2 className="w-5 h-5 text-green-500" />
                    Test Passed
                  </>
                ) : (
                  <>
                    <XCircle className="w-5 h-5 text-red-500" />
                    Test Failed
                  </>
                )}
              </CardTitle>
              <CardDescription>
                Gate: {result.gateName} | Tested: {new Date(result.testedAt).toLocaleString()}
              </CardDescription>
            </CardHeader>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Divergence Summary</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-muted-foreground">Overall Similarity:</span>
                  <div className="text-2xl font-bold">
                    {(result.divergenceSummary.similarityPercentage * 100).toFixed(1)}%
                  </div>
                </div>
                <div>
                  <span className="text-muted-foreground">Task Comparison:</span>
                  <div className="text-base font-semibold mt-1">
                    <span className="text-green-600">{result.divergenceSummary.identicalTaskCount} identical</span>
                    {' / '}
                    <span className="text-red-600">{result.divergenceSummary.differentTaskCount} different</span>
                  </div>
                </div>
              </div>

              {result.divergenceSummary.firstDivergenceIndex !== null &&
               result.divergenceSummary.firstDivergenceIndex >= 0 && (
                <div className="pt-2 border-t">
                  <span className="text-sm text-muted-foreground">First Divergence at Index:</span>
                  <span className="ml-2 font-mono text-sm">{result.divergenceSummary.firstDivergenceIndex}</span>
                </div>
              )}

              {result.divergenceSummary.criticalPathTaskIds.length > 0 && (
                <div className="pt-2 border-t">
                  <span className="text-sm text-muted-foreground">Critical Path Tasks:</span>
                  <div className="mt-2 flex flex-wrap gap-1">
                    {result.divergenceSummary.criticalPathTaskIds.map((taskId) => (
                      <Badge key={taskId} variant="outline" className="font-mono text-xs">
                        {taskId}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Rule Evaluation Results</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {result.ruleResults.map((ruleResult, idx) => (
                  <div
                    key={idx}
                    className={`p-3 rounded-lg border ${
                      ruleResult.passed ? 'bg-green-50 border-green-200' : 'bg-red-50 border-red-200'
                    }`}
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          {ruleResult.passed ? (
                            <CheckCircle2 className="w-4 h-4 text-green-600" />
                          ) : (
                            <XCircle className="w-4 h-4 text-red-600" />
                          )}
                          <span className="font-semibold text-sm">{ruleResult.ruleType}</span>
                        </div>
                        <p className="text-sm text-muted-foreground">{ruleResult.message}</p>
                      </div>
                      <div className="text-right ml-4">
                        <div className="text-xs text-muted-foreground">Threshold</div>
                        <div className="font-mono text-sm">{(ruleResult.threshold * 100).toFixed(0)}%</div>
                        <div className="text-xs text-muted-foreground mt-1">Actual</div>
                        <div className="font-mono text-sm font-bold">
                          {(ruleResult.actualValue * 100).toFixed(1)}%
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>

          <div className="flex justify-end">
            <Button onClick={() => onOpenChange(false)}>Close</Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}