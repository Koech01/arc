import { executionApi } from '@/lib/api';
import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import type { ExecutionListItem } from '@/components/types';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';


interface CompareDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  currentExecutionId?: string;
}

export function CompareDialog({ open, onOpenChange, currentExecutionId }: CompareDialogProps) {
  const navigate = useNavigate();
  const [executions, setExecutions] = useState<ExecutionListItem[]>([]);
  const [selected, setSelected] = useState<string[]>(currentExecutionId ? [currentExecutionId] : []);

  useEffect(() => {
    if (open) {
      executionApi.getAll().then(setExecutions);
    }
  }, [open]);

  const handleCompare = () => {
    if (selected.length === 2) {
      navigate(`/executions/compare?ids=${selected.join(',')}`);
      onOpenChange(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader className="text-left">
          <DialogTitle>Select Execution to Compare</DialogTitle>
        </DialogHeader>
        <div className="space-y-2 max-h-96 overflow-y-auto">
          {executions.map(exec => (
            <label key={exec.id} className="flex items-center gap-2 p-2 hover:bg-muted rounded cursor-pointer">
              <Checkbox
                checked={selected.includes(exec.id)}
                onCheckedChange={(checked) => {
                  if (checked) {
                    setSelected(prev => [...prev, exec.id].slice(-2));
                  } else {
                    setSelected(prev => prev.filter(id => id !== exec.id));
                  }
                }}
              />
              <span className="text-sm">{exec.id.slice(0, 8)}...</span>
              <span className="text-sm text-muted-foreground">{exec.status}</span>
            </label>
          ))}
        </div>
        <Button onClick={handleCompare} disabled={selected.length !== 2}>
          Compare ({selected.length}/2)
        </Button>
      </DialogContent>
    </Dialog>
  );
}