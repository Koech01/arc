import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent } from "@/components/ui/card";


export function ExecutionPageSkeleton() {
  return (
    <div className="w-screen sm:flex flex-col gap-6 p-6">
      {/* Header Skeleton */}
      <div className="flex flex-col gap-4">
        <Skeleton className="h-5 w-64" />
        <div className="flex gap-4">
          <Skeleton className="h-5 w-32" />
          <Skeleton className="h-5 w-32" />
          <Skeleton className="h-5 w-32" />
        </div>
      </div>

      {/* Tabs Skeleton */}
      <div className="hidden sm:flex gap-2">
        {Array.from({ length: 7 }).map((_, index) => (
          <Skeleton key={index} className="h-5 w-24" />
        ))}
      </div>

      {/* Content Cards Skeleton */}
      <div className="mt-4 md:mt-0 flex flex-col gap-5">
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          {Array.from({ length: 4 }).map((_, index) => (
            <Card
              key={index}
              className="w-[100%] sm:w-full sm:max-w-xs py-4"
            >
              <CardContent>
                <Skeleton className="aspect-video w-full" />
              </CardContent>
            </Card>
          ))}
        </div> 

        <Card className="flex flex-col w-full gap-3 py-3 px-3">
          {Array.from({ length: 5 }).map((_, index) => (
            <div className="flex gap-5" key={index}>
              <Skeleton className="h-4 flex-[2]" />
              <Skeleton className="h-4 flex-[1]" />
              <Skeleton className="h-4 flex-[1.5]" />
              <Skeleton className="h-4 flex-[1]" />
              <Skeleton className="h-4 flex-[1.2]" />
            </div>
          ))}
        </Card>
      </div>
    </div>
  );
}