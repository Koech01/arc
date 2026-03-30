import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";


export function WorkflowListSkeleton() {
  return (
    <div className="w-screen sm:w-auto sm:mx-0 flex flex-col gap-6 p-6">
      {/* Header Skeleton */}
      <div className="flex items-center justify-between">
        <div className="flex flex-col gap-2">
          <Skeleton className="h-8 w-40" />
          <Skeleton className="h-4 w-56" />
        </div>
        <Skeleton className="hidden sm:h-10 w-40" />
      </div>

      {/* Table Skeleton */}
      <Card className="flex flex-col w-full gap-3 py-3 px-3">
        {/* Table Header */}
        <div className="flex gap-5 pb-2 border-b">
          <Skeleton className="h-4 flex-[2]" />
          <Skeleton className="h-4 flex-[2]" />
          <Skeleton className="h-4 flex-[1]" />
          {/* Hidden on mobile */}
          <Skeleton className="hidden sm:block h-4 sm:flex-[1.5]" />
          <Skeleton className="hidden sm:block h-4 sm:flex-[1]" />
        </div>
        {/* Table Rows */}
        {Array.from({ length: 10 }).map((_, index) => (
          <div className="flex gap-5" key={index}>
            <Skeleton className="h-4 flex-[2]" />
            <Skeleton className="h-4 flex-[2]" />
            <Skeleton className="h-4 flex-[1]" />
            {/* Hidden on mobile */}
            <Skeleton className="hidden sm:block h-4 sm:flex-[1.5]" />
            <Skeleton className="hidden sm:block h-4 sm:flex-[1]" />
          </div>
        ))}
      </Card>

      {/* Pagination Skeleton */}
      <div className="flex items-center justify-between px-0 sm:px-4">
        <Skeleton className="hidden sm:h-4 w-32" />
        <div className="flex items-center gap-4">
          <Skeleton className="h-8 w-32" />
          <Skeleton className="h-8 w-24" />
          <div className="flex gap-2">
            <Skeleton className="h-8 w-8" />
            <Skeleton className="h-8 w-8" />
            <Skeleton className="hidden sm:block h-8 w-8" />
            <Skeleton className="hidden sm:block h-8 w-8" />
          </div>
        </div>
      </div>
    </div>
  );
}