import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import { server } from "@/mocks/server";
import { renderWithProviders } from "@/test/testUtils";
import type { Script } from "./api";
import { RunScriptDialog } from "./RunScriptDialog";

const scriptWithParams: Script = {
  id: "s-42",
  name: "Echo",
  description: "Echo a message",
  filePath: "Echo.ps1",
  metaJson: JSON.stringify({
    parameters: [
      { name: "Msg", type: "string", required: true },
      { name: "Times", type: "int", default: 1, required: false },
      { name: "Loud", type: "bool", required: false },
    ],
  }),
  updatedAt: new Date().toISOString(),
};

const scriptNoParams: Script = {
  ...scriptWithParams,
  id: "s-43",
  name: "NoParams",
  metaJson: JSON.stringify({ parameters: [] }),
};

describe("RunScriptDialog", () => {
  it("renders a field for each parameter", () => {
    renderWithProviders(<RunScriptDialog script={scriptWithParams} onClose={() => {}} />);
    expect(screen.getByLabelText(/Msg/)).toBeInTheDocument();
    expect(screen.getByLabelText(/Times/)).toBeInTheDocument();
    expect(screen.getByLabelText(/Loud/)).toBeInTheDocument();
  });

  it("shows a friendly message when the script has no parameters", () => {
    renderWithProviders(<RunScriptDialog script={scriptNoParams} onClose={() => {}} />);
    expect(screen.getByText(/runs as-is/i)).toBeInTheDocument();
  });

  it("validates required fields before submitting", async () => {
    const user = userEvent.setup();
    renderWithProviders(<RunScriptDialog script={scriptWithParams} onClose={() => {}} />);
    await user.click(screen.getByRole("button", { name: /run/i }));
    expect(await screen.findByText("Required")).toBeInTheDocument();
  });


  it("posts parsed parameters to /executions and closes on success", async () => {
    const posted: unknown[] = [];
    server.use(
      http.post("/api/v1/executions", async ({ request }) => {
        posted.push(await request.json());
        return HttpResponse.json({
          id: "e-99",
          scriptId: scriptWithParams.id,
          status: "Pending",
          stdout: null,
          stderr: null,
          exitCode: null,
          createdAt: new Date().toISOString(),
          startedAt: null,
          completedAt: null,
        });
      }),
    );
    const onClose = vi.fn();
    const user = userEvent.setup();

    renderWithProviders(<RunScriptDialog script={scriptWithParams} onClose={onClose} />);
    await user.type(screen.getByLabelText(/Msg/), "hi");
    const timesInput = screen.getByLabelText(/Times/);
    await user.clear(timesInput);
    await user.type(timesInput, "3");
    await user.click(screen.getByRole("button", { name: /^run$/i }));

    await waitFor(() => expect(onClose).toHaveBeenCalled());
    expect(posted).toHaveLength(1);
    expect((posted[0] as { parameters: Record<string, unknown> }).parameters).toEqual({
      Msg: "hi",
      Times: 3,
    });
  });
});
