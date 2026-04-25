import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { server } from "@/mocks/server";
import { renderWithProviders } from "@/test/testUtils";
import { NotificationBell } from "./NotificationBell";

describe("NotificationBell", () => {
  it("renders the bell with no badge when nothing is unread", async () => {
    renderWithProviders(<NotificationBell />);
    await waitFor(() => {
      expect(screen.queryByTestId("notification-badge")).not.toBeInTheDocument();
    });
    expect(screen.getByTestId("notification-bell")).toBeInTheDocument();
  });

  it("shows the unread badge with the count", async () => {
    server.use(
      http.get("/api/v1/notifications", () =>
        HttpResponse.json({
          items: [
            {
              id: "n-1",
              kind: "alert",
              title: "Alert firing: cpu-warn",
              body: "host.cpu.percent > 90 (observed 92.4)",
              severity: "critical",
              triggeredAt: new Date().toISOString(),
              linkPath: "/alerts",
            },
          ],
          unreadCount: 1,
        }),
      ),
    );
    renderWithProviders(<NotificationBell />);
    expect(await screen.findByTestId("notification-badge")).toHaveTextContent("1");
  });

  it("opens a dropdown listing the notifications when clicked", async () => {
    server.use(
      http.get("/api/v1/notifications", () =>
        HttpResponse.json({
          items: [
            {
              id: "n-1",
              kind: "alert",
              title: "Alert firing: cpu-warn",
              body: "host.cpu.percent > 90 (observed 92.4)",
              severity: "critical",
              triggeredAt: new Date().toISOString(),
              linkPath: "/alerts",
            },
          ],
          unreadCount: 1,
        }),
      ),
    );
    const user = userEvent.setup();
    renderWithProviders(<NotificationBell />);
    await screen.findByTestId("notification-badge");
    await user.click(screen.getByTestId("notification-bell"));
    const dropdown = await screen.findByTestId("notification-dropdown");
    expect(dropdown).toBeInTheDocument();
    expect(screen.getByText("Alert firing: cpu-warn")).toBeInTheDocument();
  });

  it("caps the badge at 9+", async () => {
    server.use(
      http.get("/api/v1/notifications", () =>
        HttpResponse.json({
          items: Array.from({ length: 12 }, (_, i) => ({
            id: `n-${i}`,
            kind: "alert",
            title: `Alert ${i}`,
            body: "x",
            severity: "critical",
            triggeredAt: new Date().toISOString(),
            linkPath: "/alerts",
          })),
          unreadCount: 12,
        }),
      ),
    );
    renderWithProviders(<NotificationBell />);
    expect(await screen.findByTestId("notification-badge")).toHaveTextContent("9+");
  });
});
