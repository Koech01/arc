import { ExecutionPage } from './ExecutionPage';
export { ExecutionListPage } from './ExecutionListPage';
import { ScrollArea } from "@/components/ui/scroll-area";


export default function Executions() {
  return(
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <ExecutionPage />
    </ScrollArea>
  );
}