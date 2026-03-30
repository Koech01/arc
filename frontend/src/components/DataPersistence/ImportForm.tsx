import { toast } from 'sonner';
import { useState, useRef } from 'react';
import { exportImportApi } from '@/lib/api';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import type { ImportResult } from '@/components/types/export-import';


interface ImportFormProps {
  onImportComplete?: (results: ImportResult[]) => void;
  onError?: (error: Error) => void;
}

export function ImportForm({ onImportComplete, onError }: ImportFormProps) {
  const [isLoading, setIsLoading] = useState(false);
  const [jsonContent, setJsonContent] = useState('');
  const fileRef = useRef<HTMLInputElement | null>(null);
  const [fileName, setFileName] = useState<string | null>(null);

  const validateJson = (content: string): boolean => {
    try {
      JSON.parse(content);
      return true;
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Invalid JSON';
      toast.error(message, { position: 'top-center' });
      return false;
    }
  };

  const handleFile = async (file?: File) => {
    if (!file) return;
    const text = await file.text();
    setJsonContent(text);
    validateJson(text);
  };

  const handleImport = async () => {
    if (!jsonContent.trim()) {
      const message = 'Please provide JSON content';
      toast.error(message, { position: 'top-center' });
      return;
    }

    if (!validateJson(jsonContent)) return;

    setIsLoading(true);
    try {
      const response = await exportImportApi.import({ jsonContent });
      onImportComplete?.(response.results);
      const importedExecutionIds = response.results
        .map((result) => result.executionId)
        .filter((id): id is string => Boolean(id));

      sessionStorage.setItem('recentImportedExecutionIds', JSON.stringify(importedExecutionIds));
      window.dispatchEvent(new CustomEvent('executions-imported', {
        detail: { importedExecutionIds },
      }));
      setJsonContent('');
      setFileName(null);
      toast.success('Import completed successfully', {
        position: 'top-center',
      });
    } catch (error) {
      const err =
        error instanceof Error ? error : new Error('Import failed');
      onError?.(err);
      toast.error(err.message, { position: 'top-center' });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="fileInput">Upload JSON File</Label>

        <div className="flex items-center gap-3">
          <Input
            id="fileInput"
            type="file"
            accept=".json"
            ref={fileRef}
            className="hidden"
            onChange={(e) => {
              const file = e.target.files?.[0];
              setFileName(file?.name ?? null);
              void handleFile(file);
            }}
            disabled={isLoading}
          />

          <Button
            type="button"
            variant="outline"
            onClick={() => fileRef.current?.click()}
            disabled={isLoading}
          >
            Choose File
          </Button>

          <p className="text-sm text-muted-foreground">
            {fileName ?? 'No file selected'}
          </p>
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="jsonPaste">Or Paste JSON</Label>

        <Textarea
          id="jsonPaste"
          value={jsonContent}
          onChange={(e) => setJsonContent(e.target.value)}
          disabled={isLoading}
          className="min-h-[100px]"
        />
      </div>

      <div className="flex gap-2">
        <Button
          type="button"
          onClick={handleImport}
          disabled={isLoading || !jsonContent.trim()}
          aria-busy={isLoading}
        >
          {isLoading ? 'Importing...' : 'Import Execution'}
        </Button>
      </div>
    </div>
  );
}