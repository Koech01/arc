import { toast } from 'sonner';
import { useState } from 'react';
import { ExportForm } from './ExportForm.tsx';
import { ImportForm } from './ImportForm.tsx';
import { ExportResults } from './ExportResults.tsx';
import { ScrollArea } from "@/components/ui/scroll-area";
import { ImportValidationDisplay } from './ImportValidationDisplay';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { ExecutionExportItem, ImportResult } from '@/components/types/export-import';


export default function DataPersistencePage() {
  const [exports, setExports] = useState<ExecutionExportItem[]>([]);
  const [importResults, setImportResults] = useState<ImportResult[]>([]);

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
       <div className="mx-auto max-w-7xl space-y-8 p-6">
        <div className="space-y-8">
          <div className="grid grid-cols-1 gap-8 lg:grid-cols-4 justify-items-center">
            {/* Left Column - Main Content */}
            <div className="w-full space-y-4 lg:col-span-3">
              <Card className="pb-4"> 
                <CardHeader className="pl-4 md:pl-4"> 
                  <CardTitle>Export</CardTitle>
                  <p className="text-muted-foreground text-sm">Export execution data.</p>
                </CardHeader>
                  
                <CardContent className="space-y-6"> 
                  <ExportForm
                    onExportComplete={(res: ExecutionExportItem[]) =>
                      setExports(res)
                    }
                    onError={(e: Error) =>
                      toast.error(e.message, { position: 'top-center' })
                    }
                  />
                  <ExportResults exports={exports} /> 
                </CardContent>
              </Card>
            </div>

            <div className="w-full space-y-4 lg:col-span-3">
              <Card className="pb-4"> 
                <CardHeader className="pl-4 md:pl-4"> 
                  <CardTitle>Import</CardTitle>
                  <p className="text-muted-foreground text-sm">Import execution data.</p>
                </CardHeader>

                <CardContent className="space-y-6"> 
                  <ImportForm
                    onImportComplete={(res: ImportResult[]) =>
                      setImportResults(res)
                    }
                    onError={(e: Error) =>
                      toast.error(e.message, { position: 'top-center' })
                    }
                  />
                  <ImportValidationDisplay results={importResults} />
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </div>
    </ScrollArea>
  );
}