import { toast } from 'sonner';
import { executionApi } from "@/lib/api";
import { useState, useEffect } from "react"; 
import { DashboardSkeleton } from "./DashboardSkeleton";
import { ScrollArea } from "@/components/ui/scroll-area";
import type { ExecutionListItem } from "@/components/types";
import { SectionCards } from "@/components/ui/section-cards";
import { ExecutionTable } from "@/components/ui/excecution-table";
import { SystemHealthTable } from "@/components/ui/system-health-table";


interface DashboardMetrics {
  totalRuns: number;
  successRate: number;
  failures: number;
  avgLatency: number;
  runsLast24h: number;
  failuresLast24h: number;
  peakConcurrency: number;
  avgDuration: number;
  p95Latency: number;
}

interface SystemHealth {
  component: string;
  status: "Healthy" | "Partial" | "Down";
  uptime: string;
  lastCheck: string;
  responseTime: string;
}

export default function Dashboard() {
  const [executions, setExecutions] = useState<ExecutionListItem[]>([]);
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [systemHealth, setSystemHealth] = useState<SystemHealth[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchDashboardData = async () => {
    setLoading(true);
    try {
      const data = await executionApi.getAll();
      setExecutions(data);
      
      const now = Date.now();
      const last24h = now - 24 * 60 * 60 * 1000;
      const recentExecs = data.filter(e => new Date(e.startedAt).getTime() > last24h);
      const completed = data.filter(e => e.status === "completed" || e.status === "failed");
      const failed = data.filter(e => e.status === "failed");
      const recentFailed = recentExecs.filter(e => e.status === "failed");
      
      const avgDur = completed.length > 0 
        ? completed.reduce((sum, e) => sum + (parseFloat(e.duration) || 0), 0) / completed.length 
        : 0;
      
      const sortedDurations = completed.map(e => parseFloat(e.duration) || 0).sort((a, b) => a - b);
      const p95Index = Math.floor(sortedDurations.length * 0.95);
      const p95 = sortedDurations[p95Index] || 0;

      setMetrics({
        totalRuns: data.length,
        successRate: completed.length > 0 ? ((completed.length - failed.length) / completed.length) * 100 : 0,
        failures: failed.length,
        avgLatency: Math.round(avgDur),
        runsLast24h: recentExecs.length,
        failuresLast24h: recentFailed.length,
        peakConcurrency: Math.max(...data.map(e => e.totalTasks || 0), 0),
        avgDuration: Math.round(avgDur),
        p95Latency: Math.round(p95),
      });

      setSystemHealth([
        { component: "API Gateway", status: "Healthy", uptime: "99.9%", lastCheck: new Date().toISOString(), responseTime: "45ms" },
        { component: "Database", status: "Healthy", uptime: "99.8%", lastCheck: new Date().toISOString(), responseTime: "12ms" },
        { component: "Task Queue", status: "Healthy", uptime: "100%", lastCheck: new Date().toISOString(), responseTime: "8ms" },
      ]);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Failed to load dashboard data", { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboardData();
  }, []);

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <DashboardSkeleton />
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex flex-col gap-8 py-8 md:gap-8 md:py-8">
        <SectionCards metrics={metrics} />
        <ExecutionTable data={executions.slice(0, 10)} />
        <SystemHealthTable data={systemHealth} />
      </div>
    </ScrollArea>
  );
}