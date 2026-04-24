import { useQuery } from "@tanstack/react-query";
import { RefreshCw } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import { fetchStatusBreakdown, fetchSummary, fetchTimeseries, fetchTopScripts } from "./api";
import { ExecutionsLineChart } from "./charts/ExecutionsLineChart";
import { StatusPieChart } from "./charts/StatusPieChart";
import { TopScriptsBarChart } from "./charts/TopScriptsBarChart";
import { KpiCard } from "./KpiCard";
import { useDateRange, type RangeKey } from "./useDateRange";

const AUTO_REFRESH_MS = 30_000;

export function DashboardPage() {
  const range = useDateRange("7d");
  const [autoRefresh, setAutoRefresh] = useState(false);
  const [tick, setTick] = useState(0);

  useEffect(() => {
    if (!autoRefresh) return;
    const id = window.setInterval(() => setTick((t) => t + 1), AUTO_REFRESH_MS);
    return () => window.clearInterval(id);
  }, [autoRefresh]);

  const summary = useQuery({
    queryKey: ["summary", tick],
    queryFn: fetchSummary,
  });

  const timeseries = useQuery({
    queryKey: ["ts-executions-completed", range.key, tick],
    queryFn: () =>
      fetchTimeseries({
        key: "executions.completed",
        from: range.from,
        to: range.to,
        bucket: range.bucket,
        aggregation: "count",
      }),
  });

  const statusBreakdown = useQuery({
    queryKey: ["status-breakdown", range.key, tick],
    queryFn: () => fetchStatusBreakdown({ from: range.from, to: range.to }),
  });

  const topScripts = useQuery({
    queryKey: ["top-scripts", range.key, tick],
    queryFn: () => fetchTopScripts({ from: range.from, to: range.to, limit: 5 }),
  });

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Overview</h1>
          <p className="text-sm text-muted-foreground">Operational metrics for {range.label.toLowerCase()}.</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <div className="flex rounded-md border bg-card p-0.5" role="group" aria-label="Time range">
            {range.presets.map((k: RangeKey) => (
              <button
                key={k}
                type="button"
                onClick={() => range.setKey(k)}
                className={cn(
                  "px-3 py-1 text-xs rounded-sm transition-colors",
                  range.key === k
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-accent hover:text-accent-foreground",
                )}
              >
                {k}
              </button>
            ))}
          </div>
          <Button
            size="sm"
            variant={autoRefresh ? "default" : "outline"}
            onClick={() => setAutoRefresh((v) => !v)}
            aria-pressed={autoRefresh}
          >
            <RefreshCw className={cn("mr-1 h-4 w-4", autoRefresh && "animate-spin")} />
            Auto-refresh {autoRefresh ? "on" : "off"}
          </Button>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard
          label="Available scripts"
          value={summary.data?.scriptCount ?? null}
          loading={summary.isLoading}
        />
        <KpiCard
          label="Executions (24h)"
          value={summary.data?.executionsLast24h ?? null}
          loading={summary.isLoading}
        />
        <KpiCard
          label="Failures (24h)"
          value={summary.data?.failuresLast24h ?? null}
          hint={
            summary.data && summary.data.executionsLast24h > 0
              ? `${((summary.data.failuresLast24h / summary.data.executionsLast24h) * 100).toFixed(1)}% failure rate`
              : undefined
          }
          loading={summary.isLoading}
        />
        <KpiCard
          label="Avg duration"
          value={summary.data ? `${summary.data.averageDurationSeconds.toFixed(1)}s` : null}
          loading={summary.isLoading}
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Executions over time</CardTitle>
          </CardHeader>
          <CardContent>
            {timeseries.isLoading ? (
              <Skeleton className="h-72 w-full" />
            ) : timeseries.error ? (
              <p className="text-sm text-destructive">Failed to load executions timeseries.</p>
            ) : (
              <ExecutionsLineChart points={timeseries.data?.points ?? []} bucket={range.bucket} />
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Status breakdown</CardTitle>
          </CardHeader>
          <CardContent>
            {statusBreakdown.isLoading ? (
              <Skeleton className="h-64 w-full" />
            ) : (
              <StatusPieChart rows={statusBreakdown.data ?? []} />
            )}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Top scripts by execution count</CardTitle>
        </CardHeader>
        <CardContent>
          {topScripts.isLoading ? (
            <Skeleton className="h-64 w-full" />
          ) : (
            <TopScriptsBarChart rows={topScripts.data ?? []} />
          )}
        </CardContent>
      </Card>
    </div>
  );
}
