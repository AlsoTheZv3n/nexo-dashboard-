import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Copy } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { createApiKey, fetchApiKeys, revokeApiKey, type User } from "./api";

export function ApiKeysPage() {
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({ queryKey: ["admin", "apikeys"], queryFn: fetchApiKeys });

  const [form, setForm] = useState<{ name: string; role: User["role"]; expiresInDays: number | null }>({
    name: "",
    role: "Viewer",
    expiresInDays: null,
  });
  const [justCreated, setJustCreated] = useState<string | null>(null);

  const createMut = useMutation({
    mutationFn: createApiKey,
    onSuccess: (res) => {
      setJustCreated(res.plaintext);
      qc.invalidateQueries({ queryKey: ["admin", "apikeys"] });
    },
    onError: () => toast.error("Create failed."),
  });
  const revokeMut = useMutation({
    mutationFn: revokeApiKey,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin", "apikeys"] }),
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">API keys</h1>
        <p className="text-sm text-muted-foreground">
          Service-to-service credentials. The plaintext is shown once — copy it now, we only store a hash.
        </p>
      </div>

      {justCreated && (
        <Card className="border-primary">
          <CardHeader><CardTitle className="text-base">New key</CardTitle></CardHeader>
          <CardContent className="space-y-2">
            <p className="text-sm text-muted-foreground">Copy this value; it won't be shown again.</p>
            <div className="flex items-center gap-2">
              <code data-testid="new-api-key" className="flex-1 break-all rounded-md bg-muted/40 p-2 font-mono text-xs">
                {justCreated}
              </code>
              <Button
                size="sm"
                variant="outline"
                onClick={() => {
                  navigator.clipboard.writeText(justCreated);
                  toast.success("Copied.");
                }}
              >
                <Copy className="mr-1 h-4 w-4" />
                Copy
              </Button>
              <Button size="sm" variant="ghost" onClick={() => setJustCreated(null)}>Dismiss</Button>
            </div>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader><CardTitle className="text-base">Create</CardTitle></CardHeader>
        <CardContent>
          <form
            className="grid grid-cols-1 gap-3 md:grid-cols-[1fr_auto_auto_auto]"
            onSubmit={(e) => {
              e.preventDefault();
              createMut.mutate(form);
              setForm({ name: "", role: "Viewer", expiresInDays: null });
            }}
          >
            <Input
              placeholder="name (e.g. ci-runner)"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              required
              aria-label="Key name"
            />
            <select
              className="h-10 rounded-md border border-input bg-background px-3 text-sm"
              value={form.role}
              onChange={(e) => setForm({ ...form, role: e.target.value as User["role"] })}
              aria-label="Role"
            >
              <option value="Viewer">Viewer</option>
              <option value="Operator">Operator</option>
              <option value="Admin">Admin</option>
            </select>
            <Input
              type="number"
              placeholder="expires (days, optional)"
              min={1}
              max={3650}
              value={form.expiresInDays ?? ""}
              onChange={(e) => setForm({ ...form, expiresInDays: e.target.value ? Number(e.target.value) : null })}
              aria-label="Expires in days"
            />
            <Button type="submit" disabled={createMut.isPending}>Create</Button>
          </form>
        </CardContent>
      </Card>

      {isLoading ? (
        <Skeleton className="h-40 w-full" />
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/30">
              <tr>
                <th className="px-4 py-2 text-left font-medium">Name</th>
                <th className="px-4 py-2 text-left font-medium">Prefix</th>
                <th className="px-4 py-2 text-left font-medium">Role</th>
                <th className="px-4 py-2 text-left font-medium">Active</th>
                <th className="px-4 py-2 text-left font-medium">Expires</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {(data ?? []).map((k) => (
                <tr key={k.id} className="border-b last:border-0">
                  <td className="px-4 py-2 font-medium">{k.name}</td>
                  <td className="px-4 py-2 font-mono text-xs">{k.prefix}…</td>
                  <td className="px-4 py-2">{k.role}</td>
                  <td className="px-4 py-2">{k.isActive ? "yes" : "revoked"}</td>
                  <td className="px-4 py-2 text-xs">
                    {k.expiresAt ? new Date(k.expiresAt).toLocaleDateString() : "never"}
                  </td>
                  <td className="px-4 py-2 text-right">
                    {k.isActive && (
                      <Button size="sm" variant="ghost" onClick={() => revokeMut.mutate(k.id)}>Revoke</Button>
                    )}
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
