import { toast } from 'sonner';
import { adminApi } from '@/lib/api';
import { useState, useEffect } from 'react';
import { Badge } from '@/components/ui/badge';
import { ScrollArea } from "@/components/ui/scroll-area";
import type { SystemConfig } from '@/components/types/admin';
import { Database, Cpu, Shield, Gauge, Activity, Code } from 'lucide-react';
import { SystemConfigurationSkeleton } from './SystemConfigurationSkeleton';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';


export default function SystemConfiguration() {
  const [config, setConfig] = useState<SystemConfig | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchConfig();
  }, []);

  const fetchConfig = async () => {
    setLoading(true);
    try {
      const data = await adminApi.getSystemConfig();
      setConfig(data);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load system configuration', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <SystemConfigurationSkeleton />
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-6 py-8 px-4 lg:px-6 max-w-7xl">
        <div>
          <h1 className="text-2xl font-semibold">System Configuration</h1>
          <p className="text-sm text-muted-foreground mt-2">View and monitor current system configuration settings</p>
        </div>

        {config && (
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {/* Database Configuration */}
            <Card className="overflow-hidden hover:shadow-md transition-shadow">
              <CardHeader className="pb-3 bg-gradient-to-br from-blue-50/50 to-transparent dark:from-blue-950/20">
                <div className="flex items-center gap-3">
                  <div className="p-2 bg-background rounded-md border shadow-sm">
                    <Database className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                  </div>
                  <div>
                    <CardTitle className="text-base">Database</CardTitle>
                    <CardDescription className="text-sm">Storage provider</CardDescription>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="pt-4 pb-4">
                <div className="space-y-3">
                  <div className="flex justify-between items-center p-2.5 rounded-md bg-muted/50">
                    <span className="text-sm text-muted-foreground font-medium">Provider</span>
                    <Badge variant="secondary">{config.databaseProvider}</Badge>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* LLM Configuration */}
            <Card className="overflow-hidden hover:shadow-md transition-shadow">
              <CardHeader className="pb-3 bg-gradient-to-br from-purple-50/50 to-transparent dark:from-purple-950/20">
                <div className="flex items-center gap-3">
                  <div className="p-2 bg-background rounded-md border shadow-sm">
                    <Cpu className="h-5 w-5 text-purple-600 dark:text-purple-400" />
                  </div>
                  <div>
                    <CardTitle className="text-base">LLM Defaults</CardTitle>
                    <CardDescription className="text-sm">AI model settings</CardDescription>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="pt-4 pb-4">
                <div className="space-y-3">
                  <div className="flex justify-between items-center p-2.5 rounded-md bg-muted/50">
                    <span className="text-sm text-muted-foreground font-medium">Provider</span>
                    <span className="text-sm font-medium">{config.lLMDefaultProvider}</span>
                  </div>
                  <div className="flex justify-between items-center p-2.5 rounded-md bg-muted/50">
                    <span className="text-sm text-muted-foreground font-medium">Model</span>
                    <span className="text-sm font-medium">{config.lLMDefaultModel}</span>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Authentication */}
            <Card className="overflow-hidden hover:shadow-md transition-shadow">
              <CardHeader className="pb-3 bg-gradient-to-br from-green-50/50 to-transparent dark:from-green-950/20">
                <div className="flex items-center gap-3">
                  <div className="p-2 bg-background rounded-md border shadow-sm">
                    <Shield className="h-5 w-5 text-green-600 dark:text-green-400" />
                  </div>
                  <div>
                    <CardTitle className="text-base">Authentication</CardTitle>
                    <CardDescription className="text-sm">Security settings</CardDescription>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="pt-4 pb-4">
                <div className="space-y-3">
                  <div className="flex justify-between items-center p-2.5 rounded-md bg-muted/50">
                    <span className="text-sm text-muted-foreground font-medium">JWT Expiration</span>
                    <span className="text-sm font-medium">{config.jwtExpirationMinutes} min</span>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Rate Limiting */}
            <Card className="overflow-hidden hover:shadow-md transition-shadow">
              <CardHeader className="pb-3 bg-gradient-to-br from-orange-50/50 to-transparent dark:from-orange-950/20">
                <div className="flex items-center gap-3">
                  <div className="p-2 bg-background rounded-md border shadow-sm">
                    <Gauge className="h-5 w-5 text-orange-600 dark:text-orange-400" />
                  </div>
                  <div>
                    <CardTitle className="text-base">Rate Limiting</CardTitle>
                    <CardDescription className="text-sm">Request throttling</CardDescription>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="pt-4 pb-4">
                <div className="space-y-3">
                  <div className="flex justify-between items-center p-2.5 rounded-md bg-muted/50">
                    <span className="text-sm text-muted-foreground font-medium">Permit Limit</span>
                    <span className="text-sm font-medium">{config.rateLimitPermitLimit}</span>
                  </div>
                  <div className="flex justify-between items-center p-2.5 rounded-md bg-muted/50">
                    <span className="text-sm text-muted-foreground font-medium">Time Window</span>
                    <span className="text-sm font-medium">{config.rateLimitWindowSeconds}s</span>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* System Status */}
            <Card className="overflow-hidden hover:shadow-md transition-shadow">
              <CardHeader className="pb-3 bg-gradient-to-br from-red-50/50 to-transparent dark:from-red-950/20">
                <div className="flex items-center gap-3">
                  <div className="p-2 bg-background rounded-md border shadow-sm">
                    <Activity className="h-5 w-5 text-red-600 dark:text-red-400" />
                  </div>
                  <div>
                    <CardTitle className="text-base">System Status</CardTitle>
                    <CardDescription className="text-sm">Current state</CardDescription>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="pt-4 pb-4">
                <div className="space-y-3">
                  <div className="flex justify-between items-center p-2.5 rounded-md bg-muted/50">
                    <span className="text-sm text-muted-foreground font-medium">Environment</span>
                    <Badge 
                      variant={config.environment === 'Production' ? 'default' : 'secondary'}
                      className="font-semibold"
                    >
                      {config.environment}
                    </Badge>
                  </div>
                  <div className="flex justify-between items-center p-2.5 rounded-md bg-muted/50">
                    <span className="text-sm text-muted-foreground font-medium">Maintenance</span>
                    <Badge 
                      variant={config.maintenanceModeEnabled ? 'destructive' : 'outline'}
                      className="font-semibold"
                    >
                      {config.maintenanceModeEnabled ? 'Enabled' : 'Disabled'}
                    </Badge>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* API Version */}
            <Card className="overflow-hidden hover:shadow-md transition-shadow">
              <CardHeader className="pb-3 bg-gradient-to-br from-cyan-50/50 to-transparent dark:from-cyan-950/20">
                <div className="flex items-center gap-3">
                  <div className="p-2 bg-background rounded-md border shadow-sm">
                    <Code className="h-5 w-5 text-cyan-600 dark:text-cyan-400" />
                  </div>
                  <div>
                    <CardTitle className="text-base">API Information</CardTitle>
                    <CardDescription className="text-sm">Version details</CardDescription>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="pt-4 pb-4">
                <div className="space-y-3">
                  <div className="flex justify-between items-center p-2.5 rounded-md bg-muted/50">
                    <span className="text-sm text-muted-foreground font-medium">Version</span>
                    <span className="text-sm font-semibold text-primary">{config.apiVersion}</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        )}
      </div>
    </ScrollArea>
  );
}