import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";


export function WebhooksListSkeleton() {
  return (
    <div className="container mx-auto px-4 py-8">
      <div className="space-y-6 md:space-y-10">
        {/* Header Skeleton */}
        <div className="flex justify-between items-center gap-4">
          <Skeleton className="h-5 w-40" />
          <Skeleton className="h-5 w-44" />
        </div>

        {/* Search Bar Skeleton */}
        <div className="mt-2 md: flex-1 md:w-3/4">
          <Skeleton className="h-5 w-full" />
        </div>

        {/* Webhook Cards Grid Skeleton */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {Array.from({ length: 2 }).map((_, index) => (
            <Card key={index} className="p-6 space-y-4">
              <div className="flex justify-between items-start">
                <div className="flex-1 space-y-2">
                  <Skeleton className="h-5 w-3/4" />
                  <Skeleton className="h-4 w-full" />
                </div>
                <Skeleton className="h-6 w-16" />
              </div>
              <div className="space-y-2">
                <Skeleton className="h-4 w-20" />
                <div className="flex gap-2">
                  <Skeleton className="h-6 w-24" />
                  <Skeleton className="h-6 w-28" />
                </div>
              </div>
              <div className="flex gap-2 pt-2">
                <Skeleton className="h-9 flex-1" />
                <Skeleton className="h-9 flex-1" />
              </div>
            </Card>
          ))}
        </div>
      </div>
    </div>
  );
}