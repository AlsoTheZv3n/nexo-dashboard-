import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  acknowledgeIncident,
  createAlertRule,
  deleteAlertRule,
  fetchAlertIncidents,
  fetchAlertRules,
} from "./api";

const OPERATORS = [
  { value: 0, label: "greater than" },
  { value: 1, label: "less than" },
  { value: 2, label: "equals" },
];
const AGGS = [
  { value: 0, label: "avg" },
  { value: 1, label: "sum" },
  { value: 2, label: "min" },
  { value: 3, label: "max" },
  { value: 4, label: "count" },
];

export function AlertsPage() {
  const qc = useQueryClient();
  const rules = useQuery({ queryKey: ["alerts", "rules"], queryFn: fetchAlertRules });
  const incidents = useQuery({
    queryKey: ["alerts", "incidents"],
    queryFn: () => fetchAlertIncidents(50),
    refetchInterval: 30_000,
  });

  const createMut = useMutation({
    mutationFn: createAlertRule,
    onSuccess: () => {
      toast.success("Rule created.");
      qc.invalidateQueries({ queryKey: ["alerts", "rules"] });
    },
    onError: () => toast.error("Create failed."),
  });
  const deleteMut = useMutation({
    mutationFn: deleteAlertRule,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["alerts", "rules"] }),
  });
  const ackMut = useMutation({
    mutationFn: acknowledgeIncident,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["alerts", "incidents"] }),
  });

  const [form, setForm] = useState({
    name: "",
    metricKey: "",
    operator: 0,
    threshold: 0,
    windowMinutes: 5,
    aggregation: 0,
    webhookUrl: "",
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Alerts</h1>
        <p className="text-sm text-muted-foreground">
          Rules evaluated every minute; webhooks POSTed on first trigger; auto-resolved on recovery.
        </p>
      </div>

      <Card>
        <CardHeader><CardTitle className="text-base">New rule (Admin)</CardTitle></CardHeader>
        <CardContent>
          <form
            className="grid grid-cols-1 gap-3 md:grid-cols-4"
            onSubmit={(e) => {
              e.preventDefault();
              createMut.mutate({
                ...form,
                webhookUrl: form.webhookUrl.trim() ? form.webhookUrl.trim() : null,
              });
              setForm({ ...form, name: "", metricKey: "", threshold: 0 });
            }}
          >
            <Input placeholder="name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required aria-label="name" />
            <Input placeholder="metric.key" value={form.metricKey} onChange={(e) => setForm({ ...form, metricKey: e.target.value })} required aria-label="metric key" />
            <select
              className="h-10 rounded-md border border-input bg-background px-3 text-sm"
              value={form.operator}
              onChange={(e) => setForm({ ...form, operator: Number(e.target.value) })}
              aria-label="operator"
            >
              {OPERATORS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
            <Input type="number" placeholder="threshold" value={form.threshold} onChange={(e) => setForm({ ...form, threshold: Number(e.target.value) })} aria-label="threshold" />

            <Input type="number" placeholder="window (min)" min={1} max={1440} value={form.windowMinutes} onChange={(e) => setForm({ ...form, windowMinutes: Number(e.target.value) })} aria-label="window minutes" />
            <select
              className="h-10 rounded-md border border-input bg-background px-3 text-sm"
              value={form.aggregation}
              onChange={(e) => setForm({ ...form, aggregation: Number(e.target.value) })}
              aria-label="aggregation"
            >
              {AGGS.map((a) => <option key={a.value} value={a.value}>{a.label}</option>)}
            </select>
            <Input placeholder="webhook (optional)" value={form.webhookUrl} onChange={(e) => setForm({ ...form, webhookUrl: e.target.value })} aria-label="webhook url" />
            <Button type="submit" disabled={createMut.isPending}>Create rule</Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle className="text-base">Rules</CardTitle></CardHeader>
        <CardContent className="p-0">
          {rules.isLoading ? (
            <Skeleton className="h-24 w-full" />
          ) : (
            <table className="w-full text-sm">
              <thead className="border-b bg-muted/30">
                <tr>
                  <th className="px-4 py-2 text-left font-medium">Name</th>
                  <th className="px-4 py-2 text-left font-medium">Metric</th>
                  <th className="px-4 py-2 text-left font-medium">Condition</th>
                  <th className="px-4 py-2 text-left font-medium">Window</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {(rules.data ?? []).map((r) => (
                  <tr key={r.id} className="border-b last:border-0">
                    <td className="px-4 py-2 font-medium">{r.name}</td>
                    <td className="px-4 py-2 font-mono text-xs">{r.metricKey}</td>
                    <td className="px-4 py-2 text-xs">{r.aggregation} {r.operator} {r.threshold}</td>
                    <td className="px-4 py-2 text-xs">{r.windowMinutes} min</td>
                    <td className="px-4 py-2 text-right">
                      <Button size="sm" variant="ghost" onClick={() => deleteMut.mutate(r.id)}>Delete</Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle className="text-base">Recent incidents</CardTitle></CardHeader>
        <CardContent className="p-0">
          {incidents.isLoading ? (
            <Skeleton className="h-24 w-full" />
          ) : (
            <table className="w-full text-sm">
              <thead className="border-b bg-muted/30">
                <tr>
                  <th className="px-4 py-2 text-left font-medium">State</th>
                  <th className="px-4 py-2 text-left font-medium">Rule</th>
                  <th className="px-4 py-2 text-left font-medium">Observed</th>
                  <th className="px-4 py-2 text-left font-medium">When</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {(incidents.data ?? []).length === 0 && (
                  <tr><td className="px-4 py-6 text-center text-muted-foreground" colSpan={5}>No incidents.</td></tr>
                )}
                {(incidents.data ?? []).map((i) => (
                  <tr key={i.id} className="border-b last:border-0">
                    <td className="px-4 py-2">{i.state}</td>
                    <td className="px-4 py-2 font-mono text-xs">{i.ruleId.slice(0, 8)}</td>
                    <td className="px-4 py-2">{i.observedValue.toFixed(2)}</td>
                    <td className="px-4 py-2 font-mono text-xs">{new Date(i.triggeredAt).toLocaleString()}</td>
                    <td className="px-4 py-2 text-right">
                      {i.state === "Firing" && (
                        <Button size="sm" variant="ghost" onClick={() => ackMut.mutate(i.id)}>Ack</Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
