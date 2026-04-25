import { useMutation } from "@tanstack/react-query";
import { AxiosError } from "axios";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { useAuth } from "@/features/auth/AuthContext";
import { api } from "@/lib/api";

export function ProfilePage() {
  const { user } = useAuth();

  const [form, setForm] = useState({ currentPassword: "", newPassword: "", confirm: "" });
  const [error, setError] = useState<string | null>(null);

  const mut = useMutation({
    mutationFn: (input: { currentPassword: string; newPassword: string }) =>
      api.post("/v1/profile/change-password", input),
    onSuccess: () => {
      toast.success("Password changed.");
      setForm({ currentPassword: "", newPassword: "", confirm: "" });
      setError(null);
    },
    onError: (err: unknown) => {
      const e = err as AxiosError<{ detail?: string }>;
      setError(e.response?.data?.detail ?? "Change failed.");
    },
  });

  function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (form.newPassword !== form.confirm) {
      setError("New password confirmation does not match.");
      return;
    }
    mut.mutate({ currentPassword: form.currentPassword, newPassword: form.newPassword });
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Profile</h1>
        <p className="text-sm text-muted-foreground">Your account and credentials.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Account</CardTitle>
          <CardDescription>Read-only — ask an admin to change role or activation.</CardDescription>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-x-4 gap-y-2 text-sm md:grid-cols-[140px_1fr]">
          <div className="text-muted-foreground">Username</div>
          <div className="font-medium" data-testid="profile-username">{user?.username}</div>
          <div className="text-muted-foreground">Role</div>
          <div data-testid="profile-role">{user?.role}</div>
          <div className="text-muted-foreground">User id</div>
          <div className="font-mono text-xs">{user?.id}</div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Change password</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={onSubmit} className="grid max-w-md grid-cols-1 gap-3" noValidate>
            <div className="space-y-1.5">
              <label htmlFor="cur" className="text-sm font-medium">Current password</label>
              <Input
                id="cur"
                type="password"
                autoComplete="current-password"
                value={form.currentPassword}
                onChange={(e) => setForm({ ...form, currentPassword: e.target.value })}
                required
              />
            </div>
            <div className="space-y-1.5">
              <label htmlFor="new" className="text-sm font-medium">New password</label>
              <Input
                id="new"
                type="password"
                autoComplete="new-password"
                value={form.newPassword}
                onChange={(e) => setForm({ ...form, newPassword: e.target.value })}
                required
                minLength={8}
              />
            </div>
            <div className="space-y-1.5">
              <label htmlFor="conf" className="text-sm font-medium">Confirm new password</label>
              <Input
                id="conf"
                type="password"
                autoComplete="new-password"
                value={form.confirm}
                onChange={(e) => setForm({ ...form, confirm: e.target.value })}
                required
              />
            </div>
            {error && (
              <p role="alert" className="text-sm text-destructive">{error}</p>
            )}
            <Button type="submit" disabled={mut.isPending}>
              {mut.isPending ? "Saving…" : "Change password"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
