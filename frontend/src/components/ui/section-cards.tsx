import StatisticsCard from "@/components/ui/statistics-card";


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

export function SectionCards({ metrics }: { metrics: DashboardMetrics | null }) {
  if (!metrics) return null;

  // Calculate trends and sentiments
  const runsTrend = metrics.runsLast24h > 0 ? 'up' : 'down';
  const runsChangePercent = ((metrics.runsLast24h / Math.max(metrics.totalRuns - metrics.runsLast24h, 1)) * 100).toFixed(1);
  
  const successRateTrend = metrics.successRate >= 95 ? "up" : "down";
  const successRateSentiment = metrics.successRate >= 95 ? "positive" : "negative";
  
  const failuresTrend = metrics.failuresLast24h > 0 ? "up" : "down";
  const failuresSentiment = metrics.failuresLast24h > 0 ? "negative" : "positive";
  
  const latencyTrend = metrics.avgLatency < 100 ? 'down' : 'up';
  const latencySentiment = metrics.avgLatency < 100 ? 'positive' : 'negative';

  return ( 
    <div className="mx-auto grid max-w-7xl gap-4 sm:grid-cols-2 lg:grid-cols-4 w-full px-4">
      <StatisticsCard
        title="Total Runs"
        value={metrics.totalRuns.toLocaleString()}
        changePercentage={`+${runsChangePercent} %`}
        trend={runsTrend as 'up' | 'down'}
        sentiment="positive"
        footerPrimary={`+${metrics.runsLast24h} runs in the last 24h`}
        footerSecondary={`Peak: ${metrics.peakConcurrency} | Avg: ${metrics.avgDuration}ms`}
      />

      <StatisticsCard
        title="Success Rate"
        value={`${metrics.successRate.toFixed(1)} %`}
        changePercentage={`${successRateTrend === "up" ? "+" : ""}${metrics.successRate.toFixed(1)} %`}
        trend={successRateTrend as 'up' | 'down'}
        sentiment={successRateSentiment as 'positive' | 'negative'}
        footerPrimary={`${metrics.failuresLast24h} failed executions in last 24h`}
        footerSecondary={`Total successful: ${metrics.totalRuns - metrics.failures}`}
      />

      <StatisticsCard
        title="Failures"
        value={metrics.failures.toString()}
        changePercentage={`${metrics.failuresLast24h > 0 ? "+" : ""}${metrics.failuresLast24h}`}
        trend={failuresTrend as 'up' | 'down'}
        sentiment={failuresSentiment as 'positive' | 'negative'}
        footerPrimary={metrics.failuresLast24h === 0 ? 'No failures in the last 24h' : `+${metrics.failuresLast24h} failures in the last 24h`}
      />

      <StatisticsCard
        title="Avg Latency"
        value={`${metrics.avgLatency} ms`}
        changePercentage={`${metrics.avgLatency} ms`}
        trend={latencyTrend as 'up' | 'down'}
        sentiment={latencySentiment as 'positive' | 'negative'}
        footerPrimary={`P95 latency: ${metrics.p95Latency}ms`}
      />
    </div> 
  );
}