import { screen } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { Route, Routes } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { server } from "@/mocks/server";
import { renderWithProviders } from "@/test/testUtils";
import { ExecutionDetailPage } from "./ExecutionDetailPage";

function completedExec() {
  return {
    id: "e-1",
    scriptId: "s-1",
    status: "Completed",
    stdout: "hello world",
    stderr: null,
    exitCode: 0,
    createdAt: new Date().toISOString(),
    startedAt: new Date().toISOString(),
    completedAt: new Date().toISOString(),
  };
}

function runningExec() {
  return {
    id: "e-2",
    scriptId: "s-1",
    status: "Running",
    stdout: null,
    stderr: null,
    exitCode: null,
    createdAt: new Date().toISOString(),
    startedAt: new Date().toISOString(),
    completedAt: null,
  };
}

function renderAt(id: string) {
  return renderWithProviders(
    <Routes>
      <Route path="/executions/:id" element={<ExecutionDetailPage />} />
    </Routes>,
    { initialEntries: [`/executions/${id}`] },
  );
}

describe("ExecutionDetailPage", () => {
  it("renders the completed status chip and stdout", async () => {
    server.use(
      http.get("/api/v1/executions/:id", ({ params }) =>
        HttpResponse.json({ ...completedExec(), id: params.id }),
      ),
    );

    renderAt("e-1");
    expect(await screen.findByTestId("status-chip")).toHaveTextContent("Completed");
    expect(await screen.findByTestId("stdout")).toHaveTextContent("hello world");
  });

  it("does not show a Cancel button for terminal status", async () => {
    server.use(
      http.get("/api/v1/executions/:id", ({ params }) =>
        HttpResponse.json({ ...completedExec(), id: params.id }),
      ),
    );
    renderAt("e-1");
    await screen.findByTestId("status-chip");
    expect(screen.queryByRole("button", { name: /cancel/i })).not.toBeInTheDocument();
  });

  it("shows Cancel for a Running execution (admin token from mocks)", async () => {
    localStorage.setItem(
      "nexo.tokens",
      JSON.stringify({ accessToken: "mock", refreshToken: "mock", accessExpiresAt: new Date().toISOString() }),
    );
    localStorage.setItem("nexo.user", JSON.stringify({ id: "u-1", username: "admin", role: "Admin" }));

    server.use(
      http.get("/api/v1/executions/:id", ({ params }) =>
        HttpResponse.json({ ...runningExec(), id: params.id }),
      ),
    );
    renderAt("e-2");
    await screen.findByTestId("status-chip");
    expect(screen.getByRole("button", { name: /cancel/i })).toBeInTheDocument();
  });
});
