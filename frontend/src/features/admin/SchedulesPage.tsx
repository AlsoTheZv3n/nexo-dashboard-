import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { fetchScripts, type Script } from "@/features/scripts/api";
import { createSchedule, deleteSchedule, fetchSchedules, toggleSchedule } from "./api";

export function SchedulesPage() {
  const qc = useQueryClient();
  const schedules = useQuery({ queryKey: ["schedules"], queryFn: fetchSchedules });
  const scripts = useQuery({ queryKey: ["scripts"], queryFn: fetchScripts });

  const createMut = useMutation({
    mutationFn: createSchedule,
    onSuccess: () => {
      toast.success("Schedule created.");
      qc.invalidateQueries({ queryKey: ["schedules"] });
    },
    onError: () => toast.error("Create failed (check cron syntax)."),
  });
  const toggleMut = useMutation({
    mutationFn: (s: { id: string; active: boolean }) => toggleSchedule(s.id, s.active),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["schedules"] }),
  });
  const deleteMut = useMutation({
    mutationFn: deleteSchedule,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["schedules"] }),
  });

  const [form, setForm] = useState({ scriptId: "", name: "", cronExpression: "0 * * * *" });

  const scriptById = (id: string) => scripts.data?.find((s: Script) => s.id === id)?.name ?? id.slice(0, 8);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Schedules</h1>
        <p className="text-sm text-muted-foreground">
          Cron expressions (5 fields: <code className="font-mono">m h dom mon dow</code>). Evaluated every 30 seconds.
        </p>
      </div>

      <Card>
        <CardHeader><CardTitle className="text-base">New schedule</CardTitle></CardHeader>
        <CardContent>
          <form
            className="grid grid-cols-1 gap-3 md:grid-cols-[1fr_1fr_1fr_auto]"
            onSubmit={(e) => {
              e.preventDefault();
              if (!form.scriptId) return;
              createMut.mutate({ ...form, parameters: null });
              setForm({ ...form, name: "" });
            }}
          >
            <select
              className="h-10 rounded-md border border-input bg-background px-3 text-sm"
              value={form.scriptId}
              onChange={(e) => setForm({ ...form, scriptId: e.target.value })}
              required
              aria-label="Script"
            >
              <option value="">— script —</option>
              {(scripts.data ?? []).map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
            <Input
              placeholder="name"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              required
              aria-label="Schedule name"
            />
            <Input
              placeholder="cron (0 * * * *)"
              value={form.cronExpression}
              onChange={(e) => setForm({ ...form, cronExpression: e.target.value })}
              required
              aria-label="Cron expression"
            />
            <Button type="submit" disabled={createMut.isPending}>Create</Button>
          </form>
        </CardContent>
      </Card>

      {schedules.isLoading ? (
        <Skeleton className="h-40 w-full" />
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/30">
              <tr>
                <th className="px-4 py-2 text-left font-medium">Name</th>
                <th className="px-4 py-2 text-left font-medium">Script</th>
                <th className="px-4 py-2 text-left font-medium">Cron</th>
                <th className="px-4 py-2 text-left font-medium">Active</th>
                <th className="px-4 py-2 text-left font-medium">Next run</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {(schedules.data ?? []).length === 0 && (
                <tr><td className="px-4 py-6 text-center text-muted-foreground" colSpan={6}>No schedules.</td></tr>
              )}
              {(schedules.data ?? []).map((s) => (
                <tr key={s.id} className="border-b last:border-0">
                  <td className="px-4 py-2 font-medium">{s.name}</td>
                  <td className="px-4 py-2">{scriptById(s.scriptId)}</td>
                  <td className="px-4 py-2 font-mono text-xs">{s.cronExpression}</td>
                  <td className="px-4 py-2">{s.isActive ? "yes" : "paused"}</td>
                  <td className="px-4 py-2 font-mono text-xs">
                    {s.nextRunAt ? new Date(s.nextRunAt).toLocaleString() : "—"}
                  </td>
                  <td className="px-4 py-2 text-right">
                    <Button size="sm" variant="ghost" onClick={() => toggleMut.mutate({ id: s.id, active: !s.isActive })}>
                      {s.isActive ? "Pause" : "Resume"}
                    </Button>
                    <Button size="sm" variant="ghost" onClick={() => deleteMut.mutate(s.id)}>Delete</Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
