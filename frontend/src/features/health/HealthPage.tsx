import { useQuery } from "@tanstack/react-query";
import { CheckCircle2, RefreshCw, XCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { api } from "@/lib/api";

interface HealthRow {
  label: string;
  status: "ok" | "down" | "loading";
  detail?: string;
}

async function fetchLive() {
  const r = await api.get<{ status: string; at: string }>("/v1/health/live");
  return r.data;
}
async function fetchReady() {
  const r = await api.get<{ status: string; db?: string }>("/v1/health/ready");
  return r.data;
}

function row(label: string, query: { isLoading: boolean; isError: boolean; data?: { status: string; db?: string } }): HealthRow {
  if (query.isLoading) return { label, status: "loading" };
  if (query.isError) return { label, status: "down", detail: "Endpoint unreachable" };
  return {
    label,
    status: query.data?.status === "alive" || query.data?.status === "ready" ? "ok" : "down",
    detail: query.data?.db ? `db: ${query.data.db}` : undefined,
  };
}

export function HealthPage() {
  const live = useQuery({ queryKey: ["health", "live"], queryFn: fetchLive, refetchInterval: 15_000 });
  const ready = useQuery({ queryKey: ["health", "ready"], queryFn: fetchReady, refetchInterval: 15_000 });

  const rows = [row("Liveness", live), row("Readiness", ready)];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Health</h1>
          <p className="text-sm text-muted-foreground">Auto-refreshed every 15 s.</p>
        </div>
        <Button
          size="sm"
          variant="outline"
          onClick={() => {
            live.refetch();
            ready.refetch();
          }}
        >
          <RefreshCw className="mr-1 h-4 w-4" />
          Refresh now
        </Button>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        {rows.map((r) => (
          <Card key={r.label}>
            <CardHeader>
              <CardDescription>{r.label}</CardDescription>
              <CardTitle className="flex items-center gap-2 text-2xl">
                {r.status === "ok" && <CheckCircle2 data-testid={`status-ok-${r.label}`} className="h-6 w-6 text-green-600" />}
                {r.status === "down" && <XCircle data-testid={`status-down-${r.label}`} className="h-6 w-6 text-destructive" />}
                {r.status === "loading" && <RefreshCw className="h-6 w-6 animate-spin text-muted-foreground" />}
                <span>{r.status === "loading" ? "checking…" : r.status === "ok" ? "healthy" : "down"}</span>
              </CardTitle>
            </CardHeader>
            {r.detail && <CardContent className="text-xs text-muted-foreground">{r.detail}</CardContent>}
          </Card>
        ))}
      </div>
    </div>
  );
}
