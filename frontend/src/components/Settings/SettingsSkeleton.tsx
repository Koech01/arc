import { Skeleton } from "@/components/ui/skeleton";
import { Separator } from "@/components/ui/separator";
import { CardContent, CardHeader } from "@/components/ui/card";


export function SettingsSkeleton() {
  return (
    <div className="w-screen sm:flex flex-col">
      {/* Appearance Section */}
      <CardHeader className="pl-4">
        <Skeleton className="h-7 w-32" />
      </CardHeader>
      <CardContent className="space-y-6 mb-4">
        <div className="flex items-center justify-between">
          <Skeleton className="h-6 w-40 sm:h-4 sm:w-64" />
          <Skeleton className="h-6 w-16 sm:h-10 sm:w-48" />
        </div>
      </CardContent>

      <Separator />

      {/* Notification Preferences Section */}
      <CardHeader className="pl-4">
        <Skeleton className="h-7 w-56" />
        <Skeleton className="h-4 w-80" />
      </CardHeader>
      <CardContent className="space-y-6 mb-4">
        {Array.from({ length: 3 }).map((_, index) => (
          <div key={index} className="flex items-center justify-between">
            <div className="space-y-2 flex-1">
              <Skeleton className="h-5 w-40" />
              <Skeleton className="h-4 w-[80%] sm:h-4 sm:w-full sm:max-w-md" />
            </div>
            <Skeleton className="h-6 w-11 rounded-full" />
          </div>
        ))}
      </CardContent>

      <Separator />

      {/* Localization Section */}
      <CardHeader className="pl-4">
        <Skeleton className="h-7 w-32" />
        <Skeleton className="h-4 w-72" />
      </CardHeader>
      <CardContent className="space-y-6 mb-4">
        <div className="flex items-center justify-between">
          <div className="space-y-2 flex-1">
            <Skeleton className="h-5 w-24" />
            <Skeleton className="h-4 w-64" />
          </div>
          <Skeleton className="h-8 w-16 sm:h-10 sm:w-48" />
        </div>
      </CardContent>
    </div>
  );
}