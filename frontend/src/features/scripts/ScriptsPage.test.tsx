import { screen } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { server } from "@/mocks/server";
import { renderWithProviders } from "@/test/testUtils";
import { ScriptsPage } from "./ScriptsPage";

describe("ScriptsPage", () => {
  it("shows a skeleton while loading", () => {
    server.use(
      http.get("/api/v1/scripts", async () => {
        await new Promise((r) => setTimeout(r, 50));
        return HttpResponse.json([]);
      }),
    );
    renderWithProviders(<ScriptsPage />);
    expect(screen.getAllByTestId("skeleton").length).toBeGreaterThan(0);
  });

  it("renders scripts returned by the API", async () => {
    renderWithProviders(<ScriptsPage />);
    expect(await screen.findByText("Get-SystemHealth")).toBeInTheDocument();
    expect(screen.getByText("Get-DiskUsage")).toBeInTheDocument();
  });

  it("filters rows by the search input", async () => {
    const { user } = await renderWithProvidersAsync(<ScriptsPage />);
    await screen.findByText("Get-SystemHealth");
    await user.type(screen.getByPlaceholderText("Filter…"), "Disk");
    expect(screen.queryByText("Get-SystemHealth")).not.toBeInTheDocument();
    expect(screen.getByText("Get-DiskUsage")).toBeInTheDocument();
  });
});

async function renderWithProvidersAsync(ui: React.ReactElement) {
  const userEvent = await import("@testing-library/user-event");
  return { user: userEvent.default.setup(), ...renderWithProviders(ui) };
}
