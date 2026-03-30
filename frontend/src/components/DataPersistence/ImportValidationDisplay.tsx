import type { ImportResult } from '@/components/types/export-import';


interface ImportValidationDisplayProps {
  results: ImportResult[];
}

export function ImportValidationDisplay({ results }: ImportValidationDisplayProps) {
  if (!results || results.length === 0) {
    return <div className="text-sm text-muted-foreground">No import results.</div>;
  }

  const failed = results.filter(r => !r.success);

  return (
    <div className="space-y-2">
      <div className="text-sm">Imported: {results.length} (Failed: {failed.length})</div>
      <ul className="list-disc pl-5 text-sm">
        {results.map(r => (
          <li key={r.executionId || r.importedExecutionId || r.importedAt} className={r.success ? 'text-green-600' : 'text-red-600'}>
            {r.executionId || r.importedExecutionId} - {r.success ? 'Success' : `Failed: ${r.errorMessage}`}
          </li>
        ))}
      </ul>
    </div>
  );
}