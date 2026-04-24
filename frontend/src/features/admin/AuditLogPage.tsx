import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { fetchAudit } from "./api";

export function AuditLogPage() {
  const [action, setAction] = useState("");
  const { data, isLoading } = useQuery({
    queryKey: ["admin", "audit", action],
    queryFn: () => fetchAudit({ pageSize: 100, action: action || undefined }),
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-2">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Audit log</h1>
          <p className="text-sm text-muted-foreground">Admin-only. Who did what, when, and from where.</p>
        </div>
        <Input
          placeholder="Filter by action (e.g. auth.login)"
          value={action}
          onChange={(e) => setAction(e.target.value)}
          className="max-w-xs"
          aria-label="Filter by action"
        />
      </div>

      {isLoading ? (
        <Skeleton className="h-64 w-full" />
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/30">
              <tr>
                <th className="px-4 py-2 text-left font-medium">Time</th>
                <th className="px-4 py-2 text-left font-medium">Action</th>
                <th className="px-4 py-2 text-left font-medium">User</th>
                <th className="px-4 py-2 text-left font-medium">Target</th>
                <th className="px-4 py-2 text-left font-medium">IP</th>
              </tr>
            </thead>
            <tbody>
              {(data?.items ?? []).length === 0 && (
                <tr><td className="px-4 py-6 text-center text-muted-foreground" colSpan={5}>No entries.</td></tr>
              )}
              {(data?.items ?? []).map((e) => (
                <tr key={e.id} className="border-b last:border-0">
                  <td className="px-4 py-2 font-mono text-xs">{new Date(e.timestamp).toLocaleString()}</td>
                  <td className="px-4 py-2">{e.action}</td>
                  <td className="px-4 py-2 font-mono text-xs">{e.userId?.slice(0, 8) ?? "—"}</td>
                  <td className="px-4 py-2 font-mono text-xs">
                    {e.targetType ? `${e.targetType}:${e.targetId?.slice(0, 8) ?? ""}` : "—"}
                  </td>
                  <td className="px-4 py-2 font-mono text-xs">{e.ipAddress ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
