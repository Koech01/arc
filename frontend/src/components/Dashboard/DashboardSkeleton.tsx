import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent } from "@/components/ui/card";


export function DashboardSkeleton() {
  return (
    <div className="flex flex-col gap-8 py-8 px-4 md:gap-8 md:py-8">
      {/* Section Cards Skeleton */}
      <div className="flex flex-col sm:flex-row gap-4 justify-center items-center">
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

      {/* Execution Table Skeleton */}
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

      {/* System Health Table Skeleton */}
      <Card className="flex flex-col w-full gap-3 py-3 px-3">
        {Array.from({ length: 5 }).map((_, index) => (
          <div className="flex gap-5" key={index}>
            <Skeleton className="h-4 flex-[1.5]" />
            <Skeleton className="h-4 flex-[1.2]" />
            <Skeleton className="h-4 flex-[1]" />
            <Skeleton className="h-4 flex-[2]" />
            <Skeleton className="h-4 flex-[1]" />
          </div>
        ))}
      </Card>
    </div>
  );
}