import { Activity, Bell, Clock, Heart, KeyRound, LayoutDashboard, LogOut, Moon, ScrollText, Sun, Terminal, User as UserIcon, Users } from "lucide-react";
import { NavLink, Outlet } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/features/auth/AuthContext";
import { useTheme } from "@/hooks/useTheme";
import { cn } from "@/lib/utils";

interface NavItem {
  to: string;
  label: string;
  icon: typeof LayoutDashboard;
  end?: boolean;
  adminOnly?: boolean;
}

const navItems: NavItem[] = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/scripts", label: "Scripts", icon: Terminal },
  { to: "/executions", label: "Executions", icon: Activity },
  { to: "/schedules", label: "Schedules", icon: Clock },
  { to: "/alerts", label: "Alerts", icon: Bell },
  { to: "/audit", label: "Audit", icon: ScrollText },
  { to: "/users", label: "Users", icon: Users, adminOnly: true },
  { to: "/api-keys", label: "API keys", icon: KeyRound, adminOnly: true },
  { to: "/health", label: "Health", icon: Heart },
  { to: "/profile", label: "Profile", icon: UserIcon },
];

export function AppLayout() {
  const { user, logout } = useAuth();
  const { theme, toggle } = useTheme();
  const dark = theme === "dark";

  const visibleItems = navItems.filter((n) => !n.adminOnly || user?.role === "Admin");

  return (
    <div className="flex min-h-screen bg-background">
      <aside className="hidden w-60 flex-col border-r bg-card md:flex">
        <div className="flex h-14 items-center border-b px-4 font-semibold">Nexo Dashboard</div>
        <nav className="flex-1 space-y-1 p-3">
          {visibleItems.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                cn(
                  "flex items-center gap-2 rounded-md px-3 py-2 text-sm",
                  isActive ? "bg-accent text-accent-foreground" : "text-muted-foreground hover:bg-accent hover:text-accent-foreground",
                )
              }
            >
              <Icon className="h-4 w-4" />
              {label}
            </NavLink>
          ))}
        </nav>
        <div className="border-t p-3 text-xs text-muted-foreground">
          <div>Signed in as</div>
          <div className="font-medium text-foreground">{user?.username}</div>
          <div className="mt-0.5">{user?.role}</div>
        </div>
      </aside>

      <div className="flex flex-1 flex-col">
        <header className="flex h-14 items-center justify-between border-b bg-card px-4">
          <div className="text-sm text-muted-foreground md:hidden">Nexo Dashboard</div>
          <div className="ml-auto flex items-center gap-2">
            <Button variant="ghost" size="icon" onClick={toggle} aria-label="Toggle theme">
              {dark ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
            </Button>
            <Button variant="ghost" size="sm" onClick={logout}>
              <LogOut className="mr-1 h-4 w-4" />
              Logout
            </Button>
          </div>
        </header>
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
