import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";


export function WebhooksAdminSkeleton() {
  return (
    <div className="flex flex-col gap-6 py-8 px-4 lg:px-6">
      {/* Header */}
      <div>
        <Skeleton className="h-8 w-56 mb-2" />
        <Skeleton className="h-4 w-96" />
      </div>

      {/* Table */}
      <Card>
        <div className="p-6">
          {/* Table Header */}
          <div className="flex gap-4 pb-4 border-b">
            <Skeleton className="h-4 flex-[2]" />
            <Skeleton className="h-4 flex-[1.5]" />
            <Skeleton className="h-4 flex-1" />
            <Skeleton className="h-4 flex-[1.5]" />
            <Skeleton className="h-4 flex-1" />
            <Skeleton className="h-4 flex-1" />
          </div>

          {/* Table Rows */}
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="flex gap-4 py-4 border-b last:border-b-0">
              <Skeleton className="h-4 flex-[2]" />
              <div className="flex gap-1 flex-[1.5]">
                <Skeleton className="h-6 w-16" />
                <Skeleton className="h-6 w-16" />
              </div>
              <Skeleton className="h-6 w-16 flex-1" />
              <Skeleton className="h-4 flex-[1.5]" />
              <Skeleton className="h-4 flex-1" />
              <Skeleton className="h-8 w-24 flex-1" />
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}