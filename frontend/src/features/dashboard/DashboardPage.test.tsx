import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { renderWithProviders } from "@/test/testUtils";
import { DashboardPage } from "./DashboardPage";

describe("DashboardPage", () => {
  it("renders the four KPI cards once summary arrives", async () => {
    renderWithProviders(<DashboardPage />);

    expect(await screen.findByText("7")).toBeInTheDocument(); // scriptCount
    expect(await screen.findByText("42")).toBeInTheDocument(); // executionsLast24h
    expect(await screen.findByText("3")).toBeInTheDocument(); // failuresLast24h
    expect(await screen.findByText("1.8s")).toBeInTheDocument(); // avg duration
  });

  it("shows a failure rate hint when there were failures", async () => {
    renderWithProviders(<DashboardPage />);
    // 3 / 42 = 7.14% → "7.1% failure rate"
    expect(await screen.findByText(/7\.1% failure rate/)).toBeInTheDocument();
  });

  it("switches the range when a preset button is clicked", async () => {
    const user = userEvent.setup();
    renderWithProviders(<DashboardPage />);
    await screen.findByText("7");
    await user.click(screen.getByRole("button", { name: "30d" }));
    // Subtitle reflects the new preset
    expect(await screen.findByText(/last 30 days/i)).toBeInTheDocument();
  });

  it("toggles auto-refresh", async () => {
    const user = userEvent.setup();
    renderWithProviders(<DashboardPage />);
    const toggle = screen.getByRole("button", { name: /auto-refresh off/i });
    await user.click(toggle);
    expect(screen.getByRole("button", { name: /auto-refresh on/i })).toBeInTheDocument();
  });
});
