import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardFooter, CardHeader } from "@/components/ui/card";


export function TemplateListSkeleton() {
  return (
    <div className="container mx-auto px-4 py-8">
      <div className="space-y-6">
        {/* Header Skeleton */}
        <div className="flex justify-between items-center gap-4">
          <Skeleton className="h-8 w-56" />
          <Skeleton className="h-10 w-40" />
        </div>

        {/* Search Bar Skeleton */}
        <div className="flex-1 md:w-3/4">
          <Skeleton className="h-10 w-full" />
        </div>

        {/* Template Cards Grid Skeleton */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Card key={index} className="flex flex-col">
              <CardHeader className="p-4 gap-2">
                <Skeleton className="h-6 w-40" />
                <Skeleton className="h-4 w-full" />
              </CardHeader>
              <CardFooter className="flex justify-end items-center gap-2 py-3 px-3 mt-auto border-0 border-t border-border">
                <div className="flex gap-2">
                  <Skeleton className="h-9 w-16" />
                  <Skeleton className="h-9 w-20" />
                </div>
              </CardFooter>
            </Card>
          ))}
        </div>
      </div>
    </div>
  );
}