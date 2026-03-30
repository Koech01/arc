import { formatDateTime } from '@/lib/date';
import { Badge } from '@/components/ui/badge';
import type { Task } from '@/components/types';
import { useIsMobile } from '@/hooks/use-mobile';
import { Card, CardContent } from '@/components/ui/card';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Drawer, DrawerContent, DrawerHeader, DrawerTitle } from '@/components/ui/drawer';
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion';


interface TaskDetailsSheetProps {
  task: Task | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}


export function TaskDetailsSheet({ task, open, onOpenChange }: TaskDetailsSheetProps) {
  const isMobile = useIsMobile();
  
  if (!task) return null;

  const statusVariant = {
    success: 'default' as const,
    failed: 'destructive' as const,
    running: 'secondary' as const,
    queued: 'outline' as const,
    skipped: 'outline' as const,
  };

  const cleanOutput = (text: string) => {
    const textarea = document.createElement('textarea');
    textarea.innerHTML = text;
    return textarea.value.replace(/\*\*/g, '').replace(/"/g, '');
  };

  const content = (
    <Tabs defaultValue="details" className="mt-6">
      <TabsList className="grid w-full grid-cols-4">
        <TabsTrigger value="details" className="text-xs md:text-sm px-2 md:px-3">Details</TabsTrigger>
        <TabsTrigger value="logs" className="text-xs md:text-sm px-2 md:px-3">Logs</TabsTrigger>
        <TabsTrigger value="output" className="text-xs md:text-sm px-2 md:px-3">Output</TabsTrigger>
        <TabsTrigger value="dependencies" className="text-xs md:text-sm px-2 md:px-3"><span className="md:hidden">Deps</span><span className="hidden md:inline">Dependencies</span></TabsTrigger>
      </TabsList>
      <TabsContent value="details" className="space-y-4 mt-5">
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <p className="text-muted-foreground">Task ID</p>
            <p>{task.id}</p>
          </div>
          <div>
            <p className="text-muted-foreground">Agent Type</p>
            <p>{task.agentType}</p>
          </div>
          <div>
            <p className="text-muted-foreground">Started At</p>
            <p>{formatDateTime(task.startedAt)}</p>
          </div>
          <div>
            <p className="text-muted-foreground">Duration</p>
            <p>{Math.floor(task.duration / 1000)}s</p>
          </div>
        </div>
      </TabsContent>
      <TabsContent value="logs" className="mt-5">
        <ScrollArea className="h-[500px]">
          {task.error ? (
            <pre className="text-xs p-4 bg-muted rounded">
              {task.error}
            </pre>
          ) : (
            <div className="flex flex-col gap-1 p-4">
              <p className="text-sm font-medium">No Logs</p>
              <p className="text-sm text-muted-foreground">No logs available</p>
            </div>
          )}
        </ScrollArea>
      </TabsContent>
      <TabsContent value="output" className="mt-5">
        <ScrollArea className="h-[500px]">
          {task.output ? (
            <Card className="pt-3 pl-0 pr-0 pb-3"> 
              <CardContent>
                <div className="whitespace-pre-wrap text-sm leading-relaxed">
                  {task.output && cleanOutput(task.output)}
                </div>
              </CardContent>
            </Card>
          ) : (
            <div className="flex flex-col gap-1 p-4">
              <p className="text-sm font-medium">No Output</p>
              <p className="text-sm text-muted-foreground">No output available</p>
            </div>
          )}
        </ScrollArea>
      </TabsContent>
      <TabsContent value="dependencies" className="mt-5">
        <Accordion type="single" collapsible>
          {task.dependencies.length > 0 ? (
            task.dependencies.map((dep, idx) => (
              <AccordionItem key={idx} value={`dep-${idx}`}>
                <AccordionTrigger>{dep}</AccordionTrigger>
                <AccordionContent>
                  <p className="text-sm text-muted-foreground">Dependency task ID: {dep}</p>
                </AccordionContent>
              </AccordionItem>
            ))
          ) : (
            <div className="flex flex-col gap-1 p-4">
              <p className="text-sm font-medium">No Dependencies</p>
              <p className="text-sm text-muted-foreground">This task has no dependencies</p>
            </div>
          )}
        </Accordion>
      </TabsContent>
    </Tabs>
  );

  if (isMobile) {
    return (
      <Drawer open={open} onOpenChange={onOpenChange}>
        <DrawerContent className="max-h-[85vh]">
          <DrawerHeader>
            <DrawerTitle className="flex items-center gap-2">
              {task.name}
              <Badge variant={statusVariant[task.status]}>{task.status}</Badge>
            </DrawerTitle>
          </DrawerHeader>
          <div className="px-4 pb-4 overflow-y-auto">
            {content}
          </div>
        </DrawerContent>
      </Drawer>
    );
  }

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-[600px] sm:max-w-[600px]">
        <SheetHeader>
          <SheetTitle className="flex items-center gap-2">
            {task.name}
            <Badge variant={statusVariant[task.status]}>{task.status}</Badge>
          </SheetTitle>
        </SheetHeader>
        {content}
      </SheetContent>
    </Sheet>
  );
}