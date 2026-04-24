import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { KpiCard } from "./KpiCard";

describe("KpiCard", () => {
  it("renders the value when not loading", () => {
    render(<KpiCard label="Scripts" value={42} />);
    expect(screen.getByTestId("kpi-Scripts")).toHaveTextContent("42");
  });

  it("renders a skeleton while loading", () => {
    render(<KpiCard label="Scripts" value={null} loading />);
    expect(screen.getByTestId("skeleton")).toBeInTheDocument();
  });

  it("falls back to em-dash when value is nullish", () => {
    render(<KpiCard label="Scripts" value={null} />);
    expect(screen.getByTestId("kpi-Scripts")).toHaveTextContent("—");
  });

  it("renders the hint when provided", () => {
    render(<KpiCard label="Scripts" value={3} hint="42% failures" />);
    expect(screen.getByText("42% failures")).toBeInTheDocument();
  });
});
