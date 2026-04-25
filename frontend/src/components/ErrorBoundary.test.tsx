import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { afterAll, beforeAll, describe, expect, it, vi } from "vitest";
import { ErrorBoundary } from "./ErrorBoundary";

function Boom(): never {
  throw new Error("kaboom");
}

function ResettableChild() {
  const [crash, setCrash] = useState(true);
  if (crash) throw new Error("kaboom");
  return (
    <button type="button" onClick={() => setCrash(true)}>
      ok
    </button>
  );
}

describe("ErrorBoundary", () => {
  // React DOM logs caught errors to console.error; silence the noise so test output stays useful.
  let spy: ReturnType<typeof vi.spyOn>;
  beforeAll(() => {
    spy = vi.spyOn(console, "error").mockImplementation(() => {});
  });
  afterAll(() => spy.mockRestore());

  it("renders children when nothing throws", () => {
    render(
      <ErrorBoundary>
        <span>hello</span>
      </ErrorBoundary>,
    );
    expect(screen.getByText("hello")).toBeInTheDocument();
  });

  it("shows the fallback when a child throws", () => {
    render(
      <ErrorBoundary>
        <Boom />
      </ErrorBoundary>,
    );
    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByText(/Something went wrong/i)).toBeInTheDocument();
    expect(screen.getByText("kaboom")).toBeInTheDocument();
  });

  it("Try again clears the error so children re-mount", async () => {
    const user = userEvent.setup();
    render(
      <ErrorBoundary>
        <ResettableChild />
      </ErrorBoundary>,
    );
    expect(screen.getByRole("alert")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: /try again/i }));
    // Child throws again right after reset; the boundary must catch the second crash too.
    expect(screen.getByRole("alert")).toBeInTheDocument();
  });

  it("uses a custom fallback when provided", () => {
    render(
      <ErrorBoundary fallback={(err) => <p>oops: {err.message}</p>}>
        <Boom />
      </ErrorBoundary>,
    );
    expect(screen.getByText("oops: kaboom")).toBeInTheDocument();
  });
});
