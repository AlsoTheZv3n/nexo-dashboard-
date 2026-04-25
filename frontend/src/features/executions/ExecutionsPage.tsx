import { useQuery } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useState } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { fetchExecutions, type ExecutionStatus } from "./api";

const PAGE_SIZE = 25;

const statusColor: Record<ExecutionStatus, string> = {
  Pending: "bg-muted text-muted-foreground",
  Running: "bg-blue-100 text-blue-900 dark:bg-blue-900/40 dark:text-blue-200",
  Completed: "bg-green-100 text-green-900 dark:bg-green-900/40 dark:text-green-200",
  Failed: "bg-red-100 text-red-900 dark:bg-red-900/40 dark:text-red-200",
  Cancelled: "bg-yellow-100 text-yellow-900 dark:bg-yellow-900/40 dark:text-yellow-200",
};

export function ExecutionsPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading, error, isFetching } = useQuery({
    queryKey: ["executions", page],
    queryFn: () => fetchExecutions({ page, pageSize: PAGE_SIZE }),
    placeholderData: (prev) => prev,
  });

  const total = data?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold tracking-tight">Executions</h1>

      {error && <p className="text-sm text-destructive">Failed to load executions.</p>}

      {isLoading ? (
        <Skeleton className="h-40 w-full" />
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/30">
              <tr>
                <th className="px-4 py-2 text-left font-medium">Status</th>
                <th className="px-4 py-2 text-left font-medium">Created</th>
                <th className="px-4 py-2 text-left font-medium">Duration</th>
                <th className="px-4 py-2 text-left font-medium">Exit</th>
                <th className="px-4 py-2 text-left font-medium"></th>
              </tr>
            </thead>
            <tbody>
              {(data?.items ?? []).length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-muted-foreground">
                    No executions yet.
                  </td>
                </tr>
              )}
              {(data?.items ?? []).map((e) => {
                const duration =
                  e.startedAt && e.completedAt
                    ? `${Math.round((new Date(e.completedAt).getTime() - new Date(e.startedAt).getTime()) / 1000)} s`
                    : "—";
                return (
                  <tr key={e.id} className="border-b last:border-0">
                    <td className="px-4 py-2">
                      <span className={`rounded px-2 py-0.5 text-xs ${statusColor[e.status]}`}>{e.status}</span>
                    </td>
                    <td className="px-4 py-2 font-mono text-xs">{new Date(e.createdAt).toLocaleString()}</td>
                    <td className="px-4 py-2">{duration}</td>
                    <td className="px-4 py-2">{e.exitCode ?? "—"}</td>
                    <td className="px-4 py-2 text-right">
                      <Link to={`/executions/${e.id}`} className="text-primary hover:underline">
                        Details
                      </Link>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <div className="flex items-center justify-between">
        <div className="text-xs text-muted-foreground" data-testid="page-summary">
          Page {page} of {totalPages} · {total} total{isFetching ? " · loading…" : ""}
        </div>
        <div className="flex items-center gap-2">
          <Button
            size="sm"
            variant="outline"
            disabled={page <= 1 || isFetching}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            aria-label="Previous page"
          >
            <ChevronLeft className="h-4 w-4" />
            Prev
          </Button>
          <Button
            size="sm"
            variant="outline"
            disabled={page >= totalPages || isFetching}
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            aria-label="Next page"
          >
            Next
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
