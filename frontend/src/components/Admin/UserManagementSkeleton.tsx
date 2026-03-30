import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";


export function UserManagementSkeleton() {
  return (
    <div className="flex flex-col gap-6 py-8 px-4 lg:px-6">
      {/* Header */}
      <div>
        <Skeleton className="h-8 w-48 mb-2" />
        <Skeleton className="h-4 w-96" />
      </div>

      {/* Filters */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="space-y-2">
            <Skeleton className="h-4 w-24" />
            <Skeleton className="h-10 w-full" />
          </div>
        ))}
      </div>

      {/* Refresh Button */}
      <div className="flex justify-end">
        <Skeleton className="h-10 w-24" />
      </div>

      {/* Table */}
      <Card>
        <div className="p-6">
          {/* Table Header */}
          <div className="flex gap-4 pb-4 border-b">
            <Skeleton className="h-4 flex-[2]" />
            <Skeleton className="h-4 flex-[2]" />
            <Skeleton className="h-4 flex-1" />
            <Skeleton className="h-4 flex-1" />
            <Skeleton className="h-4 flex-[1.5]" />
            <Skeleton className="h-4 flex-[2]" />
          </div>

          {/* Table Rows */}
          {Array.from({ length: 10 }).map((_, i) => (
            <div key={i} className="flex gap-4 py-4 border-b last:border-b-0">
              <Skeleton className="h-4 flex-[2]" />
              <Skeleton className="h-4 flex-[2]" />
              <Skeleton className="h-6 w-16 flex-1" />
              <Skeleton className="h-6 w-16 flex-1" />
              <Skeleton className="h-4 flex-[1.5]" />
              <div className="flex gap-2 flex-[2]">
                <Skeleton className="h-8 w-8" />
                <Skeleton className="h-8 w-8" />
                <Skeleton className="h-8 w-8" />
                <Skeleton className="h-8 w-8" />
                <Skeleton className="h-8 w-8" />
              </div>
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}