import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Eye, Download, FileText } from 'lucide-react';
import { ScrollArea } from '@/components/ui/scroll-area';
import type { ExecutionOutput } from '@/components/types';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';


interface OutputsSectionProps {
  outputs: ExecutionOutput[];
}

export function OutputsSection({ outputs }: OutputsSectionProps) {
  const downloadOutput = (output: ExecutionOutput) => {
    const blob = new Blob([output.value], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${output.key}.txt`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-3">
      {outputs.map((output, idx) => (
        <Card key={idx} className="p-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <FileText className="h-4 w-4 text-blue-600" />
              </div>
              <div>
                <p className="text-sm font-medium">{output.key}</p>
                <p className="text-muted-foreground text-xs">{output.type}</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Dialog>
                <DialogTrigger asChild>
                  <Button variant="ghost" size="icon" aria-label={`View ${output.key}`}>
                    <Eye className="h-4 w-4" />
                  </Button>
                </DialogTrigger>
                <DialogContent className="p-3 max-w-3xl">
                  <DialogHeader>
                    <DialogTitle>{output.key}</DialogTitle>
                  </DialogHeader>
                  <ScrollArea className="h-[500px]">
                    <pre className="text-xs p-4 bg-muted rounded whitespace-pre-wrap">
                      {output.value}
                    </pre>
                  </ScrollArea>
                </DialogContent>
              </Dialog>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => downloadOutput(output)}
                aria-label={`Download ${output.key}`}
              >
                <Download className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </Card>
      ))}
    </div>
  );
}