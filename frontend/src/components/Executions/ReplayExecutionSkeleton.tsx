import { Skeleton } from '@/components/ui/skeleton';
import { Card, CardContent, CardHeader } from '@/components/ui/card';


export function ReplayExecutionSkeleton() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4 md:gap-0">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10 rounded-md" />
          <div className="space-y-2">
            <Skeleton className="h-5 w-48" />
            <Skeleton className="h-4 w-64" />
          </div>
        </div>
        <Skeleton className="h-5 w-32" />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {[...Array(4)].map((_, i) => (
          <Card key={i} className="p-3 flex flex-col gap-3">
            <CardHeader className="p-0">
              <Skeleton className="h-4 w-24" />
            </CardHeader>
            <CardContent className="p-0">
              <Skeleton className="h-5 w-32" />
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader>
          <Skeleton className="h-5 w-40" />
        </CardHeader>
        <CardContent>
          <div className="space-y-3 mb-4">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="flex items-center gap-4">
                <Skeleton className="h-5 w-10" />
                <Skeleton className="h-5 flex-1" />
                <Skeleton className="h-5 w-24 hidden md:block" />
                <Skeleton className="h-5 w-24 hidden lg:block" />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}