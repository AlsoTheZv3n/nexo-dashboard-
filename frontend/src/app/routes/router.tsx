import { createBrowserRouter, Navigate } from "react-router-dom";
import { AppLayout } from "@/app/layouts/AppLayout";
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
      { path: "audit", element: <div>Audit log — Phase 1+ stub.</div> },
      { path: "*", element: <Navigate to="/" replace /> },
    ],
  },
]);
