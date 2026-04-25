import { screen } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { server } from "@/mocks/server";
import { renderWithProviders } from "@/test/testUtils";
import { HealthPage } from "./HealthPage";

describe("HealthPage", () => {
  it("renders both probes as healthy when the API replies", async () => {
    renderWithProviders(<HealthPage />);
    expect(await screen.findByTestId("status-ok-Liveness")).toBeInTheDocument();
    expect(await screen.findByTestId("status-ok-Readiness")).toBeInTheDocument();
  });

  it("renders the readiness probe as down when /health/ready 503s", async () => {
    server.use(
      http.get("/api/v1/health/ready", () =>
        HttpResponse.json({ status: "not-ready", db: "unreachable" }, { status: 503 }),
      ),
    );
    renderWithProviders(<HealthPage />);
    expect(await screen.findByTestId("status-down-Readiness")).toBeInTheDocument();
  });
});
