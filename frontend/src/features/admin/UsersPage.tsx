import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { createUser, fetchUsers, updateUser, type User } from "./api";

export function UsersPage() {
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({ queryKey: ["admin", "users"], queryFn: fetchUsers });

  const createMut = useMutation({
    mutationFn: createUser,
    onSuccess: () => {
      toast.success("User created.");
      qc.invalidateQueries({ queryKey: ["admin", "users"] });
    },
    onError: () => toast.error("Create failed."),
  });
  const toggleMut = useMutation({
    mutationFn: (u: User) => updateUser(u.id, { isActive: !u.isActive }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin", "users"] }),
  });

  const [form, setForm] = useState({ username: "", password: "", role: "Viewer" as User["role"] });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Users</h1>
        <p className="text-sm text-muted-foreground">Admin-only: create users, toggle active, change role.</p>
      </div>

      <Card>
        <CardHeader><CardTitle className="text-base">New user</CardTitle></CardHeader>
        <CardContent>
          <form
            className="grid grid-cols-1 gap-3 md:grid-cols-[1fr_1fr_auto_auto]"
            onSubmit={(e) => {
              e.preventDefault();
              createMut.mutate(form);
              setForm({ username: "", password: "", role: "Viewer" });
            }}
          >
            <Input
              placeholder="username"
              value={form.username}
              onChange={(e) => setForm({ ...form, username: e.target.value })}
              required
              aria-label="Username"
            />
            <Input
              placeholder="password"
              type="password"
              value={form.password}
              onChange={(e) => setForm({ ...form, password: e.target.value })}
              required
              aria-label="Password"
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
            <Button type="submit" disabled={createMut.isPending}>Create</Button>
          </form>
        </CardContent>
      </Card>

      {isLoading ? (
        <Skeleton className="h-64 w-full" />
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/30">
              <tr>
                <th className="px-4 py-2 text-left font-medium">Username</th>
                <th className="px-4 py-2 text-left font-medium">Role</th>
                <th className="px-4 py-2 text-left font-medium">Active</th>
                <th className="px-4 py-2 text-left font-medium">Last login</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {(data ?? []).map((u) => (
                <tr key={u.id} className="border-b last:border-0">
                  <td className="px-4 py-2 font-medium">{u.username}</td>
                  <td className="px-4 py-2">{u.role}</td>
                  <td className="px-4 py-2">{u.isActive ? "yes" : "no"}</td>
                  <td className="px-4 py-2 text-xs text-muted-foreground">
                    {u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString() : "—"}
                  </td>
                  <td className="px-4 py-2 text-right">
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => toggleMut.mutate(u)}
                      aria-label={`${u.isActive ? "Deactivate" : "Activate"} ${u.username}`}
                    >
                      {u.isActive ? "Deactivate" : "Activate"}
                    </Button>
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
