import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";


export function CacheManagementSkeleton() {
  return (
    <div className="flex flex-col gap-6 py-8 px-4 lg:px-6">
      {/* Header */}
      <div>
        <Skeleton className="h-8 w-48 mb-2" />
        <Skeleton className="h-4 w-96" />
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 3 }).map((_, i) => (
          <Card key={i}>
            <div className="p-6 space-y-3">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-10 w-20" />
              <Skeleton className="h-3 w-32" />
            </div>
          </Card>
        ))}
      </div>

      {/* Cache Actions Section */}
      <div className="flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <div className="space-y-2">
            <Skeleton className="h-6 w-32" />
            <Skeleton className="h-4 w-96" />
          </div>
          <Skeleton className="h-10 w-32" />
        </div>

        {/* Cache Details */}
        <Card>
          <div className="p-4">
            <div className="grid gap-2">
              <div className="flex justify-between">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-48" />
              </div>
              <div className="flex justify-between">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-48" />
              </div>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}