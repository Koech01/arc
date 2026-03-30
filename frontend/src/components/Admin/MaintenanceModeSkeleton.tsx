import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent, CardHeader } from "@/components/ui/card";


export function MaintenanceModeSkeleton() {
  return (
    <div className="flex flex-col gap-6 py-8 px-4 lg:px-6 max-w-5xl">
      {/* Header */}
      <div>
        <Skeleton className="h-9 w-56 mb-2" />
        <Skeleton className="h-5 w-96" />
      </div>

      {/* Maintenance Control Card */}
      <Card className="overflow-hidden">
        <CardHeader className="bg-muted/50 border-b pb-4">
          <div className="flex items-center gap-3">
            <Skeleton className="h-10 w-10 rounded-md" />
            <div className="space-y-2">
              <Skeleton className="h-5 w-48" />
              <Skeleton className="h-4 w-64" />
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-6 space-y-6">
          {/* Toggle Section */}
          <div className="flex items-start justify-between gap-4">
            <div className="space-y-2 flex-1">
              <Skeleton className="h-5 w-56" />
              <Skeleton className="h-4 w-full max-w-md" />
            </div>
            <Skeleton className="h-6 w-11 rounded-full" />
          </div>

          {/* Reason Input Skeleton */}
          <div className="space-y-3 pt-4 border-t">
            <Skeleton className="h-4 w-48" />
            <Skeleton className="h-24 w-full rounded-md" />
            <Skeleton className="h-3 w-72" />
          </div>
        </CardContent>
      </Card>

      {/* Status Card */}
      <Card>
        <CardHeader className="pb-3">
          <Skeleton className="h-5 w-48 mb-1" />
          <Skeleton className="h-4 w-56" />
        </CardHeader>
        <CardContent className="space-y-4">
          <Skeleton className="h-16 w-full rounded-lg" />
          <Skeleton className="h-12 w-full rounded-lg" />
        </CardContent>
      </Card>
    </div>
  );
}