import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Play } from "lucide-react";
import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuth } from "@/features/auth/AuthContext";
import { fetchExecutions } from "@/features/executions/api";
import { fetchScript, parseMeta } from "./api";
import { RunScriptDialog } from "./RunScriptDialog";

export function ScriptDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const canRun = user?.role === "Admin" || user?.role === "Operator";
  const [runOpen, setRunOpen] = useState(false);

  const scriptQ = useQuery({
    queryKey: ["script", id],
    queryFn: () => fetchScript(id!),
    enabled: !!id,
  });

  const historyQ = useQuery({
    queryKey: ["executions", "for-script", id],
    queryFn: () => fetchExecutions({ page: 1, pageSize: 10, scriptId: id }),
    enabled: !!id,
  });

  if (scriptQ.isLoading) {
    return <Skeleton className="h-96 w-full" />;
  }
  if (scriptQ.error || !scriptQ.data) {
    return <p className="text-sm text-destructive">Script not found.</p>;
  }
  const script = scriptQ.data;
  const meta = parseMeta(script.metaJson);
  const recent = historyQ.data?.items ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <Link to="/scripts">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="mr-1 h-4 w-4" />
            Back
          </Button>
        </Link>
        <h1 className="text-2xl font-semibold tracking-tight">{script.name}</h1>
        <div className="ml-auto">
          {canRun && (
            <Button size="sm" onClick={() => setRunOpen(true)}>
              <Play className="mr-1 h-4 w-4" />
              Run
            </Button>
          )}
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Metadata</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-x-4 gap-y-2 text-sm md:grid-cols-[140px_1fr]">
          <div className="text-muted-foreground">Description</div>
          <div>{script.description}</div>

          <div className="text-muted-foreground">File</div>
          <div className="font-mono text-xs">{script.filePath}</div>

          <div className="text-muted-foreground">Updated</div>
          <div>{new Date(script.updatedAt).toLocaleString()}</div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Parameters</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {meta.parameters.length === 0 ? (
            <p className="px-6 py-4 text-sm text-muted-foreground">No parameters.</p>
          ) : (
            <table className="w-full text-sm">
              <thead className="border-b bg-muted/30">
                <tr>
                  <th className="px-4 py-2 text-left font-medium">Name</th>
                  <th className="px-4 py-2 text-left font-medium">Type</th>
                  <th className="px-4 py-2 text-left font-medium">Required</th>
                  <th className="px-4 py-2 text-left font-medium">Default</th>
                </tr>
              </thead>
              <tbody>
                {meta.parameters.map((p) => (
                  <tr key={p.name} className="border-b last:border-0">
                    <td className="px-4 py-2 font-mono text-xs">{p.name}</td>
                    <td className="px-4 py-2">{p.type}</td>
                    <td className="px-4 py-2">{p.required ? "yes" : "no"}</td>
                    <td className="px-4 py-2 font-mono text-xs">
                      {p.default === undefined || p.default === null ? "—" : String(p.default)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Recent runs</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {historyQ.isLoading ? (
            <Skeleton className="h-24 w-full" />
          ) : recent.length === 0 ? (
            <p className="px-6 py-4 text-sm text-muted-foreground">No executions yet for this script.</p>
          ) : (
            <table className="w-full text-sm">
              <thead className="border-b bg-muted/30">
                <tr>
                  <th className="px-4 py-2 text-left font-medium">Status</th>
                  <th className="px-4 py-2 text-left font-medium">Created</th>
                  <th className="px-4 py-2 text-left font-medium">Exit</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {recent.map((e) => (
                  <tr key={e.id} className="border-b last:border-0">
                    <td className="px-4 py-2">{e.status}</td>
                    <td className="px-4 py-2 font-mono text-xs">{new Date(e.createdAt).toLocaleString()}</td>
                    <td className="px-4 py-2">{e.exitCode ?? "—"}</td>
                    <td className="px-4 py-2 text-right">
                      <Link to={`/executions/${e.id}`} className="text-primary hover:underline">
                        Details
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>

      {runOpen && <RunScriptDialog script={script} onClose={() => setRunOpen(false)} />}
    </div>
  );
}
