export { WorkflowListPage } from './WorkflowListPage';
import { ScrollArea } from "@/components/ui/scroll-area";
import { CreateWorkflowForm } from './CreateWorkflowForm';
export { WorkflowDetailPage } from './WorkflowDetailPage';
export { ExecutionResultDialog } from './ExecutionResultDialog';


export default function CreateWorkflow() {
  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-6 pt-6 pl-2 pr-2 pb-6">
        <div className="pt-0 pb-0 pl-6 pr-0">
          <h1 className="text-2xl font-semibold">Create Workflow</h1>
          <p className="text-sm text-muted-foreground">Define a new workflow with tasks and triggers</p>
        </div> 
        <CreateWorkflowForm />
      </div>
    </ScrollArea>
  );
}