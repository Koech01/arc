import { toast } from 'sonner';
import { useEffect, useState } from 'react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { templateApi, llmApi } from '@/lib/api';
import { extractVariables } from '@/lib/templateUtils';
import type { LLMConfig } from '@/components/types/llm';
import { ScrollArea } from '@/components/ui/scroll-area';
import { useNavigate, useParams } from 'react-router-dom';
import type { TemplateDetail } from '@/components/types/template';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Card, CardContent, CardFooter, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';


export function InstantiateTemplatePage() {
  const navigate = useNavigate();
  const { name } = useParams<{ name: string }>();
  const [template, setTemplate] = useState<TemplateDetail | null>(null);
  const [variables, setVariables] = useState<Record<string, string>>({});
  const [detectedVars, setDetectedVars] = useState<string[]>([]);
  const [llms, setLlms] = useState<LLMConfig[]>([]);
  const [llmConfigId, setLlmConfigId] = useState('');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!name) return;
    
    const fetchData = async () => {
      setLoading(true);
      try {
        const [data, llmList] = await Promise.all([templateApi.getByName(name), llmApi.getAll()]);
        setTemplate(data);
        setLlms(llmList);

        const vars = extractVariables(data.tasks);
        setDetectedVars(vars);

        const initialVars: Record<string, string> = {};
        vars.forEach(v => initialVars[v] = '');
        setVariables(initialVars);
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'Failed to load template', { position: 'top-center' });
        navigate('/templates');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [name, navigate]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name) return;

    setSubmitting(true);
    try {
      const result = await templateApi.instantiate(name, {
        variables: detectedVars.length > 0 ? variables : undefined,
        llmConfigId: !template?.llmConfigId && llmConfigId ? llmConfigId : undefined,
      });
      toast.success(`Workflow "${result.workflowName}" created successfully`, { position: 'top-center' });
      navigate(`/workflows/${result.workflowId}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to instantiate template', { position: 'top-center' });
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <div className="container mx-auto px-4 py-8">
          <div className="text-center">Loading template...</div>
        </div>
      </ScrollArea>
    );
  }

  if (!template) return null;

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="container mx-auto px-4 py-8">
        <h1 className="text-2xl font-bold mb-6">Instantiate Template</h1>

        <Card className="max-w-2xl">
          <div className="pt-5 pl-4 pr-4 pb-4">
            <CardTitle className="mb-1">{template.name}</CardTitle>
            <CardDescription>{template.description}</CardDescription>
          </div>
          <CardContent>
            <form onSubmit={handleSubmit} id="instantiate-form" className="space-y-4">
              {!template.llmConfigId && (
                <div>
                  <Label htmlFor="llmConfigId">
                    LLM Provider <span className="text-red-500">*</span>
                  </Label>
                  <Select value={llmConfigId} onValueChange={setLlmConfigId} required>
                    <SelectTrigger id="llmConfigId" aria-required="true">
                      <SelectValue placeholder="Select LLM provider" />
                    </SelectTrigger>
                    <SelectContent>
                      {llms.map((llm) => (
                        <SelectItem key={llm.id} value={llm.id}>{llm.name}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  {llms.length === 0 && (
                    <p className="text-xs text-muted-foreground mt-1">No LLMs configured. <a href="/llms" className="underline">Add one</a></p>
                  )}
                </div>
              )}

              {detectedVars.length === 0 && template.llmConfigId ? (
                <p className="text-sm text-muted-foreground">
                  This template has no variables. Click instantiate to create a new workflow.
                </p>
              ) : (
                detectedVars.map(varName => (
                  <div key={varName}>
                    <Label htmlFor={varName}>
                      {varName} <span className="text-red-500">*</span>
                    </Label>
                    <Input
                      id={varName}
                      value={variables[varName]}
                      onChange={(e) => setVariables({ ...variables, [varName]: e.target.value })}
                      required
                      aria-required="true"
                      placeholder={`Enter value for ${varName}`}
                    />
                  </div>
                ))
              )}
            </form>
          </CardContent>
          <CardFooter className="flex justify-end gap-2 mt-4">
            <Button variant="outline" onClick={() => navigate('/templates')}>
              Cancel
            </Button>
            <Button
              type="submit"
              form="instantiate-form"
              disabled={submitting || (!template.llmConfigId && !llmConfigId)}
              aria-busy={submitting}
            >
              {submitting ? 'Creating...' : 'Instantiate'}
            </Button>
          </CardFooter>
        </Card>
      </div>
    </ScrollArea>
  );
}