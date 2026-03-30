import { FileQuestion } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { ScrollArea } from "@/components/ui/scroll-area";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty';


export function NotFoundPage() {
  const navigate = useNavigate();

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <Empty>
        <EmptyHeader>
          <EmptyMedia variant="icon">
            <FileQuestion aria-hidden="true" />
          </EmptyMedia>
          <EmptyTitle id="not-found-title">Page not found</EmptyTitle>
          <EmptyDescription>
            The page you're looking for doesn't exist or has been moved.
            Check the URL and try again.
          </EmptyDescription>
        </EmptyHeader>
        <div className="flex flex-col items-center gap-3 sm:flex-row">
          <Button
            variant="outline"
            onClick={() => navigate(-1)}
            aria-label="Go back to previous page"
          >
            Go back
          </Button>
          <Button
            onClick={() => navigate('/dashboard')}
            aria-label="Go to dashboard"
          >
            Dashboard
          </Button>
        </div>
      </Empty>
    </ScrollArea>
  );
}