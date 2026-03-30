import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";


export function ExecutionListSkeleton() {
  return (
    <div className="w-screen sm:flex flex-col gap-6 p-6">
      {/* Header Skeleton */}
      <div className="flex flex-col gap-2">
        <Skeleton className="h-4 w-40" />
        <Skeleton className="hidden sm:h-4 w-56" />
      </div>

      {/* Analytics Cards Skeleton */}
      <div className="mt-4 md:mt-0 grid gap-1 md:grid-cols-4">
        {Array.from({ length: 4 }).map((_, index) => (
          <div key={index} className="rounded-lg border py-6 space-y-2 flex flex-col items-center">
            <Skeleton className="h-4 w-16" />
            <Skeleton className="h-4 w-20" />
          </div>
        ))}
      </div>

      {/* Filters Skeleton */}
      <div className="flex gap-4 items-end mt-4 mb-4">
        <div className="flex-1 space-y-2">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-10 w-full" />
        </div>
        <div className="w-48 space-y-2">
          <Skeleton className="h-4 w-16" />
          <Skeleton className="h-10 w-full" />
        </div>
      </div>

      {/* Table Skeleton */}
      <Card className="flex flex-col w-full gap-3 py-3 px-3">
        {/* Table Header */}
        <div className="flex gap-5 pb-2 border-b">
          <Skeleton className="h-4 flex-[1]" />
          <Skeleton className="h-4 flex-[1]" />
          <Skeleton className="h-4 flex-[0.5]" />
          <Skeleton className="h-4 flex-[1]" />
          <Skeleton className="h-4 flex-[1.5]" />
        </div>
        {/* Table Rows */}
        {Array.from({ length: 3 }).map((_, index) => (
          <div className="flex gap-5" key={index}>
            <Skeleton className="h-4 flex-[1]" />
            <Skeleton className="h-4 flex-[1]" />
            <Skeleton className="h-4 flex-[0.5]" />
            <Skeleton className="h-4 flex-[1]" />
            <Skeleton className="h-4 flex-[1.5]" />
          </div>
        ))}
      </Card>

      {/* Pagination Skeleton */}
      <div className="hidden sm:flex items-center justify-between px-4">
        <Skeleton className="h-4 w-32" />
        <div className="flex items-center gap-4">
          <Skeleton className="h-8 w-32" />
          <Skeleton className="h-8 w-24" />
          <div className="flex gap-2">
            <Skeleton className="h-8 w-8" />
            <Skeleton className="h-8 w-8" />
            <Skeleton className="h-8 w-8" />
            <Skeleton className="h-8 w-8" />
          </div>
        </div>
      </div>
    </div>
  );
}