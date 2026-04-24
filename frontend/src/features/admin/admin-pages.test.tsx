import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { server } from "@/mocks/server";
import { renderWithProviders } from "@/test/testUtils";
import { AlertsPage } from "./AlertsPage";
import { ApiKeysPage } from "./ApiKeysPage";
import { AuditLogPage } from "./AuditLogPage";
import { SchedulesPage } from "./SchedulesPage";
import { UsersPage } from "./UsersPage";

describe("UsersPage", () => {
  it("renders users fetched from /v1/users", async () => {
    server.use(
      http.get("/api/v1/users", () =>
        HttpResponse.json([
          { id: "u-1", username: "alice", role: "Admin", isActive: true, createdAt: new Date().toISOString(), lastLoginAt: null },
        ]),
      ),
    );
    renderWithProviders(<UsersPage />);
    expect(await screen.findByText("alice")).toBeInTheDocument();
  });
});

describe("AuditLogPage", () => {
  it("renders entries from /v1/audit", async () => {
    server.use(
      http.get("/api/v1/audit", () =>
        HttpResponse.json({
          items: [
            {
              id: "a-1",
              userId: "u-1",
              action: "auth.login",
              targetType: "User",
              targetId: "u-1",
              detailsJson: null,
              ipAddress: "127.0.0.1",
              timestamp: new Date().toISOString(),
            },
          ],
          page: 1, pageSize: 100, total: 1,
        }),
      ),
    );
    renderWithProviders(<AuditLogPage />);
    expect(await screen.findByText("auth.login")).toBeInTheDocument();
  });
});

describe("AlertsPage", () => {
  it("renders rule rows and incident table", async () => {
    server.use(
      http.get("/api/v1/alerts/rules", () =>
        HttpResponse.json([
          {
            id: "r-1",
            name: "cpu-warn",
            metricKey: "host.cpu.percent",
            operator: "GreaterThan",
            threshold: 80,
            windowMinutes: 5,
            aggregation: "Avg",
            webhookUrl: null,
            isActive: true,
            lastEvaluatedAt: null,
          },
        ]),
      ),
      http.get("/api/v1/alerts/incidents", () => HttpResponse.json([])),
    );
    renderWithProviders(<AlertsPage />);
    expect(await screen.findByText("cpu-warn")).toBeInTheDocument();
    expect(screen.getByText("Rules")).toBeInTheDocument();
  });
});

describe("SchedulesPage", () => {
  it("lists schedules with their cron expression", async () => {
    server.use(
      http.get("/api/v1/schedules", () =>
        HttpResponse.json([
          {
            id: "s-1",
            scriptId: "scr-1",
            name: "hourly-health",
            cronExpression: "0 * * * *",
            parametersJson: "{}",
            isActive: true,
            lastRunAt: null,
            nextRunAt: new Date().toISOString(),
          },
        ]),
      ),
      http.get("/api/v1/scripts", () =>
        HttpResponse.json([
          { id: "scr-1", name: "Get-SystemHealth", description: "", filePath: "", metaJson: '{"parameters":[]}', updatedAt: new Date().toISOString() },
        ]),
      ),
    );
    renderWithProviders(<SchedulesPage />);
    expect(await screen.findByText("hourly-health")).toBeInTheDocument();
    expect(screen.getByText("0 * * * *")).toBeInTheDocument();
  });
});

describe("ApiKeysPage", () => {
  it("shows the plaintext once after create", async () => {
    server.use(
      http.get("/api/v1/api-keys", () => HttpResponse.json([])),
      http.post("/api/v1/api-keys", () =>
        HttpResponse.json({
          key: {
            id: "k-1",
            name: "ci-runner",
            prefix: "nxk_12345678",
            role: "Viewer",
            isActive: true,
            createdAt: new Date().toISOString(),
            expiresAt: null,
            lastUsedAt: null,
          },
          plaintext: "nxk_12345678_very_secret",
        }),
      ),
    );
    const user = userEvent.setup();
    renderWithProviders(<ApiKeysPage />);
    await user.type(screen.getByLabelText(/key name/i), "ci-runner");
    await user.click(screen.getByRole("button", { name: /^create$/i }));
    expect(await screen.findByTestId("new-api-key")).toHaveTextContent("nxk_12345678_very_secret");
  });
});
