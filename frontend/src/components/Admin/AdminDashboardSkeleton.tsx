import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";


export function AdminDashboardSkeleton() {
  return (
    <div className="flex flex-col gap-8 py-8 md:gap-8 md:py-8">
      {/* Statistics Cards Skeleton */}
      <div className="mx-auto grid max-w-7xl gap-4 sm:grid-cols-2 lg:grid-cols-3 w-full pl-4 pr-4">
        {Array.from({ length: 3 }).map((_, index) => (
          <Card key={index} className="p-6 space-y-3">
            <Skeleton className="h-4 w-24" />
            <Skeleton className="h-8 w-16" />
            <div className="space-y-1">
              <Skeleton className="h-3 w-32" />
              <Skeleton className="h-3 w-24" />
            </div>
          </Card>
        ))}
      </div>

      {/* Quick Actions Skeleton */}
      <div className="flex flex-col gap-4 px-4 lg:px-6">
        <Skeleton className="h-6 w-32" />
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-10 w-full" />
          ))}
        </div>
      </div>

      {/* System Health Table Skeleton */}
      <div className="flex flex-col gap-4 px-4 lg:px-6">
        <Skeleton className="h-6 w-32" />
        <Card>
          <div className="p-6">
            {/* Table Header */}
            <div className="flex gap-4 pb-4 border-b">
              <Skeleton className="h-4 flex-[2]" />
              <Skeleton className="h-4 flex-[1.5]" />
              <Skeleton className="h-4 flex-1" />
              <Skeleton className="h-4 flex-[1.5]" />
            </div>
            {/* Table Rows */}
            {Array.from({ length: 3 }).map((_, index) => (
              <div className="flex gap-4 py-4 border-b last:border-b-0" key={index}>
                <Skeleton className="h-4 flex-[2]" />
                <Skeleton className="h-6 w-20 flex-[1.5]" />
                <Skeleton className="h-4 flex-1" />
                <Skeleton className="h-4 flex-[1.5]" />
              </div>
            ))}
          </div>
        </Card>
      </div>

      {/* Recent Executions Table Skeleton */}
      <div className="flex flex-col gap-4 px-4 lg:px-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-6 w-40" />
          <Skeleton className="h-4 w-16" />
        </div>
        <Card>
          <div className="p-6">
            {/* Table Header */}
            <div className="flex gap-4 pb-4 border-b">
              <Skeleton className="h-4 flex-[2]" />
              <Skeleton className="h-4 flex-[1.5]" />
              <Skeleton className="h-4 flex-1" />
              <Skeleton className="h-4 flex-1" />
              <Skeleton className="h-4 flex-[1.5]" />
            </div>
            {/* Table Rows */}
            {Array.from({ length: 5 }).map((_, index) => (
              <div className="flex gap-4 py-4 border-b last:border-b-0" key={index}>
                <Skeleton className="h-4 flex-[2]" />
                <Skeleton className="h-4 flex-[1.5]" />
                <Skeleton className="h-6 w-20 flex-1" />
                <Skeleton className="h-4 flex-1" />
                <Skeleton className="h-4 flex-[1.5]" />
              </div>
            ))}
          </div>
        </Card>
      </div>

      {/* Users Table Section Skeleton */}
      <div className="flex flex-col gap-4 px-4 lg:px-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-6 w-20" />
          <Skeleton className="h-10 w-64" />
        </div>
        <Card>
          <div className="p-6">
            {/* Table Header */}
            <div className="flex gap-4 pb-4 border-b">
              <Skeleton className="h-4 flex-[2]" />
              <Skeleton className="h-4 flex-1" />
              <Skeleton className="h-4 flex-1" />
              <Skeleton className="h-4 flex-[1.5]" />
            </div>
            {/* Table Rows */}
            {Array.from({ length: 6 }).map((_, index) => (
              <div className="flex gap-4 py-4 border-b last:border-b-0" key={index}>
                <Skeleton className="h-4 flex-[2]" />
                <Skeleton className="h-6 w-16 flex-1" />
                <Skeleton className="h-6 w-16 flex-1" />
                <Skeleton className="h-4 flex-[1.5]" />
              </div>
            ))}
          </div>
        </Card>
      </div>
    </div>
  );
}