import { Skeleton } from "@/components/ui/skeleton";


export function NotificationsSkeleton() {
  return (
    <div className="w-screen sm:flex flex-col">
      {/* Header Skeleton */}
      <div className="flex items-center justify-between p-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-10 w-40" />
      </div>

      {/* Notification Items Skeleton */}
      <div className="flex flex-col gap-4 p-4 pt-0">
        {Array.from({ length: 4 }).map((_, index) => (
          <div key={index} className="flex flex-col gap-2 rounded-lg border p-3">
            <div className="flex w-full items-center justify-between">
              <div className="flex items-center gap-4">
                <Skeleton className="h-5 w-48" />
                <Skeleton className="hidden sm:h-6 w-16" />
              </div>
              <Skeleton className="h-4 w-32" />
            </div>
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-3/4" />
          </div>
        ))}
      </div>
    </div>
  );
}