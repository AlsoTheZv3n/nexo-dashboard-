import { screen } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { Route, Routes } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { server } from "@/mocks/server";
import { renderWithProviders } from "@/test/testUtils";
import { ScriptDetailPage } from "./ScriptDetailPage";

function script(overrides: Record<string, unknown> = {}) {
  return {
    id: "s-1",
    name: "Get-SystemHealth",
    description: "Checks system health",
    filePath: "Get-SystemHealth.ps1",
    metaJson: JSON.stringify({
      parameters: [{ name: "MinFreeGB", type: "int", default: 10, required: false }],
    }),
    updatedAt: new Date().toISOString(),
    ...overrides,
  };
}

function renderAt(id: string) {
  return renderWithProviders(
    <Routes>
      <Route path="/scripts/:id" element={<ScriptDetailPage />} />
    </Routes>,
    { initialEntries: [`/scripts/${id}`] },
  );
}

describe("ScriptDetailPage", () => {
  it("renders script metadata + parameters table", async () => {
    server.use(
      http.get("/api/v1/scripts/:id", ({ params }) =>
        HttpResponse.json(script({ id: params.id as string })),
      ),
      http.get("/api/v1/executions", () =>
        HttpResponse.json({ items: [], page: 1, pageSize: 10, total: 0 }),
      ),
    );
    renderAt("s-1");
    expect(await screen.findByText("Get-SystemHealth")).toBeInTheDocument();
    expect(await screen.findByText("MinFreeGB")).toBeInTheDocument();
    expect(screen.getByText("No executions yet for this script.")).toBeInTheDocument();
  });

  it("lists recent runs returned by /executions filtered by scriptId", async () => {
    let receivedScriptId: string | null = null;
    server.use(
      http.get("/api/v1/scripts/:id", ({ params }) =>
        HttpResponse.json(script({ id: params.id as string })),
      ),
      http.get("/api/v1/executions", ({ request }) => {
        receivedScriptId = new URL(request.url).searchParams.get("scriptId");
        return HttpResponse.json({
          items: [
            {
              id: "e-1",
              scriptId: "s-1",
              status: "Completed",
              stdout: "ok",
              stderr: null,
              exitCode: 0,
              createdAt: new Date().toISOString(),
              startedAt: new Date().toISOString(),
              completedAt: new Date().toISOString(),
            },
          ],
          page: 1,
          pageSize: 10,
          total: 1,
        });
      }),
    );

    renderAt("s-1");
    expect(await screen.findByText("Completed")).toBeInTheDocument();
    expect(receivedScriptId).toBe("s-1");
  });
});
