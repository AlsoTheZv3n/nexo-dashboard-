import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

const kpis = [
  { key: "scripts", label: "Available scripts", value: "—" },
  { key: "executions", label: "Executions (24h)", value: "—" },
  { key: "failures", label: "Failures (24h)", value: "—" },
  { key: "avgDuration", label: "Avg duration", value: "—" },
];

export function DashboardPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Overview</h1>
        <p className="text-sm text-muted-foreground">Live operational KPIs. Charts arrive in Phase 6.</p>
      </div>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {kpis.map((k) => (
          <Card key={k.key}>
            <CardHeader>
              <CardDescription>{k.label}</CardDescription>
              <CardTitle className="text-3xl">{k.value}</CardTitle>
            </CardHeader>
            <CardContent className="text-xs text-muted-foreground">Waiting for Phase 6 metrics wiring.</CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
