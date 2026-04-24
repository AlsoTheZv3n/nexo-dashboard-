import { createBrowserRouter, Navigate } from "react-router-dom";
import { AppLayout } from "@/app/layouts/AppLayout";
import { AlertsPage } from "@/features/admin/AlertsPage";
import { ApiKeysPage } from "@/features/admin/ApiKeysPage";
import { AuditLogPage } from "@/features/admin/AuditLogPage";
import { SchedulesPage } from "@/features/admin/SchedulesPage";
import { UsersPage } from "@/features/admin/UsersPage";
import { LoginPage } from "@/features/auth/LoginPage";
import { RequireAuth } from "@/features/auth/RequireAuth";
import { DashboardPage } from "@/features/dashboard/DashboardPage";
import { ExecutionDetailPage } from "@/features/executions/ExecutionDetailPage";
import { ExecutionsPage } from "@/features/executions/ExecutionsPage";
import { ScriptsPage } from "@/features/scripts/ScriptsPage";

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  {
    path: "/",
    element: (
      <RequireAuth>
        <AppLayout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <DashboardPage /> },
      { path: "scripts", element: <ScriptsPage /> },
      { path: "scripts/:id", element: <ScriptsPage /> },
      { path: "executions", element: <ExecutionsPage /> },
      { path: "executions/:id", element: <ExecutionDetailPage /> },
      { path: "schedules", element: <SchedulesPage /> },
      { path: "alerts", element: <AlertsPage /> },
      { path: "audit", element: <AuditLogPage /> },
      { path: "users", element: <UsersPage /> },
      { path: "api-keys", element: <ApiKeysPage /> },
      { path: "*", element: <Navigate to="/" replace /> },
    ],
  },
]);
