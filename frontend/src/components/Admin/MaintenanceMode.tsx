import { toast } from 'sonner';
import { adminApi } from '@/lib/api';
import { useState, useEffect } from 'react';
import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Textarea } from '@/components/ui/textarea';
import { ScrollArea } from "@/components/ui/scroll-area";
import { MaintenanceModeSkeleton } from './MaintenanceModeSkeleton';
import type { MaintenanceModeStatus } from '@/components/types/admin';
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertTriangle, Settings2, Clock, User, FileText, Activity } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';


export default function MaintenanceMode() {
  const [status, setStatus] = useState<MaintenanceModeStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [updating, setUpdating] = useState(false);
  const [reason, setReason] = useState('');

  useEffect(() => {
    fetchStatus();
    const interval = setInterval(fetchStatus, 10000);
    return () => clearInterval(interval);
  }, []);

  const fetchStatus = async () => {
    try {
      const data = await adminApi.getMaintenanceStatus();
      setStatus(data);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load maintenance status', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  const handleToggle = async (enabled: boolean) => {
    setUpdating(true);
    try {
      if (enabled) {
        await adminApi.enableMaintenance(reason || undefined);
        toast.success('Maintenance mode enabled');
      } else {
        await adminApi.disableMaintenance();
        toast.success('Maintenance mode disabled');
        setReason('');
      }
      fetchStatus();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to toggle maintenance mode');
    } finally {
      setUpdating(false);
    }
  };

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <MaintenanceModeSkeleton />
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-6 py-8 px-4 lg:px-6 max-w-5xl">
        <div>
          <h1 className="text-2xl font-semibold">Maintenance Mode</h1>
          <p className="text-sm text-muted-foreground  mt-2">Control system-wide maintenance mode and API availability</p>
        </div>

        {/* Maintenance Status Alert */}
        {status?.isEnabled && (
          <Alert variant="destructive" className="border-2">
            <AlertTriangle className="h-5 w-5" />
            <AlertTitle className="text-base font-semibold">Maintenance Mode Active</AlertTitle>
            <AlertDescription className="mt-2">
              The system is currently in maintenance mode. All non-admin API requests will receive a 503 Service Unavailable response.
            </AlertDescription>
          </Alert>
        )}

        {/* Maintenance Control Card */}
        <Card className="overflow-hidden">
          <CardHeader className="bg-muted/50 border-b">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-background rounded-md border">
                <Settings2 className="h-5 w-5 text-primary" />
              </div>
              <div>
                <CardTitle>Maintenance Control</CardTitle>
                <CardDescription>Toggle system maintenance mode on or off</CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent className="p-6">
            <div className="space-y-6">
              {/* Toggle Section */}
              <div className="flex items-start justify-between gap-4">
                <div className="space-y-1 flex-1">
                  <Label htmlFor="maintenance-toggle" className="text-base font-semibold flex items-center gap-2">
                    <Activity className="h-4 w-4" />
                    Enable Maintenance Mode
                  </Label>
                  <p className="text-sm text-muted-foreground leading-relaxed">
                    When enabled, all non-admin API requests will be blocked with a 503 status code
                  </p>
                </div>
                <Switch
                  id="maintenance-toggle"
                  checked={status?.isEnabled || false}
                  onCheckedChange={handleToggle}
                  disabled={updating}
                  aria-label="Toggle maintenance mode"
                  className="mt-1"
                />
              </div>

              {/* Reason Input - Only shown when disabled */}
              {!status?.isEnabled && (
                <div className="space-y-3 pt-4 border-t">
                  <Label htmlFor="reason" className="text-sm font-medium flex items-center gap-2">
                    <FileText className="h-4 w-4" />
                    Maintenance Reason (Optional)
                  </Label>
                  <Textarea
                    id="reason"
                    placeholder="Describe the reason for entering maintenance mode (e.g., system upgrade, database migration)..."
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                    rows={3}
                    aria-label="Maintenance reason"
                    className="resize-none"
                  />
                  <p className="text-xs text-muted-foreground">
                    This reason will be logged and visible in the admin panel
                  </p>
                </div>
              )}

              {/* Active Maintenance Details */}
              {status?.isEnabled && (
                <div className="space-y-4 pt-4 border-t">
                  <h4 className="font-semibold text-sm flex items-center gap-2">
                    <FileText className="h-4 w-4" />
                    Active Maintenance Details
                  </h4>
                  <div className="grid gap-3">
                    <div className="flex items-center justify-between p-3 rounded-lg bg-muted/50 border">
                      <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <User className="h-4 w-4" />
                        Enabled By:
                      </div>
                      <span className="text-sm font-medium">{status.enabledBy || 'Unknown'}</span>
                    </div>
                    <div className="flex items-center justify-between p-3 rounded-lg bg-muted/50 border">
                      <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <Clock className="h-4 w-4" />
                        Enabled At:
                      </div>
                      <span className="text-sm font-medium">
                        {status.enabledAtUtc ? new Date(status.enabledAtUtc).toLocaleString() : 'N/A'}
                      </span>
                    </div>
                    {status.reason && (
                      <div className="p-3 rounded-lg bg-muted/50 border">
                        <div className="flex items-center gap-2 text-sm text-muted-foreground mb-2">
                          <FileText className="h-4 w-4" />
                          Reason:
                        </div>
                        <p className="text-sm leading-relaxed">{status.reason}</p>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Current Status Card */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base flex items-center gap-2">
              <Activity className="h-4 w-4" />
              Current System Status
            </CardTitle>
            <CardDescription>Real-time maintenance mode status</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="flex items-center justify-between p-4 rounded-lg border bg-muted/30">
                <span className="text-sm font-medium">Operating Mode:</span>
                <Badge 
                  variant={status?.isEnabled ? 'destructive' : 'default'}
                  className="px-3 py-1 font-semibold"
                >
                  {status?.isEnabled ? 'Maintenance Active' : 'Normal Operation'}
                </Badge>
              </div>
              <div className="flex items-start gap-2 text-xs text-muted-foreground p-3 rounded-lg bg-muted/30">
                <Clock className="h-3.5 w-3.5 mt-0.5 flex-shrink-0" />
                <span>Status is automatically refreshed every 10 seconds to ensure synchronization across all admin sessions</span>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </ScrollArea>
  );
}