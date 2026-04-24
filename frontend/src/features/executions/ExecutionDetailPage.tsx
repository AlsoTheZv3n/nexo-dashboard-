import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, CircleSlash, Clock } from "lucide-react";
import { Link, useParams } from "react-router-dom";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuth } from "@/features/auth/AuthContext";
import { cn } from "@/lib/utils";
import { cancelExecution, fetchExecution, type Execution, type ExecutionStatus } from "./api";

const statusClass: Record<ExecutionStatus, string> = {
  Pending: "bg-muted text-muted-foreground",
  Running: "bg-blue-100 text-blue-900 dark:bg-blue-900/40 dark:text-blue-200",
  Completed: "bg-green-100 text-green-900 dark:bg-green-900/40 dark:text-green-200",
  Failed: "bg-red-100 text-red-900 dark:bg-red-900/40 dark:text-red-200",
  Cancelled: "bg-yellow-100 text-yellow-900 dark:bg-yellow-900/40 dark:text-yellow-200",
};

function isTerminal(status: ExecutionStatus) {
  return status === "Completed" || status === "Failed" || status === "Cancelled";
}

function duration(e: Execution): string {
  if (!e.startedAt) return "—";
  const end = e.completedAt ? new Date(e.completedAt).getTime() : Date.now();
  const start = new Date(e.startedAt).getTime();
  return `${((end - start) / 1000).toFixed(1)} s`;
}

export function ExecutionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const canCancel = user?.role === "Admin" || user?.role === "Operator";

  const query = useQuery({
    queryKey: ["execution", id],
    queryFn: () => fetchExecution(id!),
    enabled: !!id,
    // Poll every 2s while the execution is still in flight; stop once terminal.
    refetchInterval: (q) => {
      const status = q.state.data?.status;
      return status && isTerminal(status) ? false : 2000;
    },
    refetchIntervalInBackground: true,
  });

  const cancelMut = useMutation({
    mutationFn: () => cancelExecution(id!),
    onSuccess: () => {
      toast.success("Cancel requested.");
      queryClient.invalidateQueries({ queryKey: ["execution", id] });
    },
    onError: () => toast.error("Failed to cancel."),
  });

  if (query.isLoading) {
    return <Skeleton className="h-96 w-full" />;
  }
  if (query.error || !query.data) {
    return <p className="text-sm text-destructive">Execution not found.</p>;
  }
  const e = query.data;

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <Link to="/executions">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="mr-1 h-4 w-4" />
            Back
          </Button>
        </Link>
        <h1 className="text-2xl font-semibold tracking-tight">Execution details</h1>
        <span className={cn("ml-2 rounded px-2 py-0.5 text-xs", statusClass[e.status])} data-testid="status-chip">
          {e.status}
        </span>
        <div className="ml-auto flex items-center gap-2 text-xs text-muted-foreground">
          <Clock className="h-3.5 w-3.5" />
          {duration(e)}
        </div>
        {canCancel && !isTerminal(e.status) && (
          <Button
            size="sm"
            variant="destructive"
            onClick={() => cancelMut.mutate()}
            disabled={cancelMut.isPending}
          >
            <CircleSlash className="mr-1 h-4 w-4" />
            Cancel
          </Button>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Metadata</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
          <div className="text-muted-foreground">Execution ID</div>
          <div className="font-mono text-xs">{e.id}</div>

          <div className="text-muted-foreground">Script ID</div>
          <div className="font-mono text-xs">{e.scriptId}</div>

          <div className="text-muted-foreground">Created</div>
          <div>{new Date(e.createdAt).toLocaleString()}</div>

          <div className="text-muted-foreground">Started</div>
          <div>{e.startedAt ? new Date(e.startedAt).toLocaleString() : "—"}</div>

          <div className="text-muted-foreground">Completed</div>
          <div>{e.completedAt ? new Date(e.completedAt).toLocaleString() : "—"}</div>

          <div className="text-muted-foreground">Exit code</div>
          <div>{e.exitCode ?? "—"}</div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">stdout</CardTitle>
        </CardHeader>
        <CardContent>
          <pre
            data-testid="stdout"
            className="max-h-96 overflow-auto rounded-md bg-muted/40 p-3 font-mono text-xs"
          >
            {e.stdout ?? (isTerminal(e.status) ? "(empty)" : "Waiting for output…")}
          </pre>
        </CardContent>
      </Card>

      {e.stderr && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base text-destructive">stderr</CardTitle>
          </CardHeader>
          <CardContent>
            <pre
              data-testid="stderr"
              className="max-h-96 overflow-auto rounded-md bg-destructive/5 p-3 font-mono text-xs text-destructive"
            >
              {e.stderr}
            </pre>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
