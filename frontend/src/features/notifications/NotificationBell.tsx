import { useQuery } from "@tanstack/react-query";
import { Bell } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { fetchNotifications } from "./api";

const POLL_MS = 30_000;

export function NotificationBell() {
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const wrapRef = useRef<HTMLDivElement>(null);

  const { data } = useQuery({
    queryKey: ["notifications"],
    queryFn: fetchNotifications,
    refetchInterval: POLL_MS,
    // Don't crash the bell when /notifications 401s during signed-out moments.
    retry: false,
  });

  const unread = data?.unreadCount ?? 0;
  const items = data?.items ?? [];

  useEffect(() => {
    if (!open) return;
    function onClick(e: MouseEvent) {
      if (!wrapRef.current?.contains(e.target as Node)) setOpen(false);
    }
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }
    document.addEventListener("mousedown", onClick);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onClick);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  return (
    <div className="relative" ref={wrapRef}>
      <Button
        variant="ghost"
        size="icon"
        onClick={() => setOpen((v) => !v)}
        aria-label={`Notifications${unread > 0 ? ` (${unread} unread)` : ""}`}
        aria-expanded={open}
        aria-haspopup="menu"
        data-testid="notification-bell"
      >
        <Bell className="h-4 w-4" />
        {unread > 0 && (
          <span
            data-testid="notification-badge"
            className="absolute right-1 top-1 inline-flex h-4 min-w-[1rem] items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-semibold leading-none text-destructive-foreground"
          >
            {unread > 9 ? "9+" : unread}
          </span>
        )}
      </Button>

      {open && (
        <div
          role="menu"
          data-testid="notification-dropdown"
          className="absolute right-0 top-full z-40 mt-2 w-80 rounded-lg border bg-card shadow-lg"
        >
          <div className="border-b px-3 py-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
            Notifications {unread > 0 && <span className="ml-1 text-destructive">({unread})</span>}
          </div>
          {items.length === 0 ? (
            <p className="px-3 py-6 text-center text-sm text-muted-foreground">All clear.</p>
          ) : (
            <ul className="max-h-80 overflow-auto">
              {items.map((n) => (
                <li key={n.id} className="border-b last:border-0">
                  <button
                    type="button"
                    onClick={() => {
                      setOpen(false);
                      if (n.linkPath) navigate(n.linkPath);
                    }}
                    className="block w-full px-3 py-2 text-left hover:bg-accent"
                  >
                    <div className="flex items-start justify-between gap-2">
                      <span className="text-sm font-medium">{n.title}</span>
                      <span
                        className={cn(
                          "rounded px-1.5 py-0.5 text-[10px] font-semibold uppercase",
                          n.severity === "critical"
                            ? "bg-destructive/10 text-destructive"
                            : "bg-muted text-muted-foreground",
                        )}
                      >
                        {n.severity}
                      </span>
                    </div>
                    <p className="mt-0.5 text-xs text-muted-foreground">{n.body}</p>
                    <p className="mt-0.5 font-mono text-[10px] text-muted-foreground">
                      {new Date(n.triggeredAt).toLocaleString()}
                    </p>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
